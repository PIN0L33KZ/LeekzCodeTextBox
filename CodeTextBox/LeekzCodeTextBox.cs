using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodeTextBox
{
    public partial class LeekzCodeTextBox : UserControl
    {
        private const int EM_LINESCROLL = 0x00B6;

        // P/Invoke for scrolling the RichTextBox by logical lines.
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public enum LineNumberDockSide
        {
            Left,
            Right
        }

        // Snapshot of lines at the moment MarkAsSaved() was last called.
        private string[] _savedLines = [];

        // True once MarkAsSaved() was called at least once.
        private bool _hasSavedSnapshot;

        public LeekzCodeTextBox()
        {
            InitializeComponent();

            // Enable double buffering on the UserControl itself.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            UpdateStyles();

            // Enable double buffering on inner controls (panel + RichTextBox).
            EnableDoubleBuffering(PNL_LineNumber);
            EnableDoubleBuffering(RTB_Text);

            // Paint handler for the line number panel.
            PNL_LineNumber.Paint += PNL_LineNumber_Paint;

            // Events for the code area.
            InitEvents();

            // Mouse wheel handling (zoom / scroll).
            RTB_Text.MouseWheel += RTB_Text_MouseWheel;
            PNL_LineNumber.MouseWheel += PNL_LineNumber_MouseWheel;

            // Do NOT grab focus in the line number panel (keeps selection in RTB_Text).
            // PNL_LineNumber.MouseEnter += (s, e) => PNL_LineNumber.Focus();

            // Apply initial docking based on property.
            ApplyLineNumberDock();
        }

        /// <summary>
        /// Enables double buffering on a control that does not expose the property publicly.
        /// </summary>
        private static void EnableDoubleBuffering(Control? control)
        {
            if(control == null)
            {
                return;
            }

            PropertyInfo? doubleBufferProperty =
                control.GetType().GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            doubleBufferProperty?.SetValue(control, true, null);
        }

        private void InitEvents()
        {
            // Redraw line numbers whenever the text area changes visually.
            RTB_Text.VScroll += (_, _) => PNL_LineNumber.Invalidate();
            RTB_Text.TextChanged += RTB_Text_TextChanged;
            RTB_Text.Resize += (_, _) => PNL_LineNumber.Invalidate();
            RTB_Text.FontChanged += (_, _) => SyncLineNumberFontFromText();
        }

        // ---------------------------------------------------------
        //  Public API – save / change tracking
        // ---------------------------------------------------------

        /// <summary>
        /// Marks the current text as saved.
        /// The current lines are stored as the baseline for future comparisons.
        /// </summary>
        public void MarkAsSaved()
        {
            string[] lines = RTB_Text.Lines;
            _savedLines = new string[lines.Length];
            Array.Copy(lines, _savedLines, lines.Length);

            _hasSavedSnapshot = true;

            PNL_LineNumber.Invalidate();
        }

        /// <summary>
        /// Returns true if there is a saved snapshot and all lines are identical to it.
        /// This method performs a full comparison only when called.
        /// </summary>
        public bool IsSaved()
        {
            if(!_hasSavedSnapshot)
            {
                return false;
            }

            string[] currentLines = RTB_Text.Lines;

            if(currentLines.Length != _savedLines.Length)
            {
                return false;
            }

            for(int i = 0; i < currentLines.Length; i++)
            {
                if(!string.Equals(currentLines[i], _savedLines[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        // ---------------------------------------------------------
        //  Hide inherited visual properties on the UserControl
        //  (use 'new' instead of 'override' to avoid nullability warnings)
        // ---------------------------------------------------------

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => base.ForeColor;
            set => base.ForeColor = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Image? BackgroundImage
        {
            get => base.BackgroundImage;
            set => base.BackgroundImage = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ImageLayout BackgroundImageLayout
        {
            get => base.BackgroundImageLayout;
            set => base.BackgroundImageLayout = value;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Font Font
        {
            get => base.Font;
            set => base.Font = value;
        }

        // ---------------------------------------------------------
        //  Public properties – code area
        // ---------------------------------------------------------

        [Category("Code")]
        [Description("Background colour of the code area (RichTextBox).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CodeBackColor
        {
            get => RTB_Text.BackColor;
            set => RTB_Text.BackColor = value;
        }

        [Category("Code")]
        [Description("Text colour of the code (RichTextBox).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color CodeForeColor
        {
            get => RTB_Text.ForeColor;
            set => RTB_Text.ForeColor = value;
        }

        [Category("Code")]
        [Description("Font used for the code (RichTextBox).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Font CodeFont
        {
            get => RTB_Text.Font;
            set
            {
                if(value == null)
                {
                    return;
                }

                RTB_Text.Font = value;
                SyncLineNumberFontFromText();
            }
        }

        [Category("Code")]
        [Description("Enables or disables word wrapping in the code area.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CodeWordWrap
        {
            get => RTB_Text.WordWrap;
            set
            {
                if(RTB_Text.WordWrap == value)
                {
                    return;
                }

                RTB_Text.WordWrap = value;
                PNL_LineNumber.Invalidate();
            }
        }

        // ---------------------------------------------------------
        //  Public properties – line numbers
        // ---------------------------------------------------------

        [Category("Line Numbers")]
        [Description("Background colour of the line number panel.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color LineNumberBackColor
        {
            get => PNL_LineNumber.BackColor;
            set
            {
                PNL_LineNumber.BackColor = value;
                PNL_LineNumber.Invalidate();
            }
        }

        [Category("Line Numbers")]
        [Description("Text colour of the line numbers.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color LineNumberForeColor
        {
            get;
            set
            {
                field = value;
                PNL_LineNumber.Invalidate();
            }
        } = Color.Gray;

        [Category("Line Numbers")]
        [Description("Font used to draw the line numbers (always Arial; size is derived from the code font).")]
        [Browsable(false)] // read-only in designer
        public Font? LineNumberFont { get; private set; }

        [Category("Line Numbers")]
        [Description("Colour of the separator line between line numbers and code.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color LineNumberSeperatorColor
        {
            get;
            set
            {
                field = value;
                PNL_LineNumber.Invalidate();
            }
        } = Color.Silver;

        [Category("Line Numbers")]
        [Description("Colour of separator segments when a line has changed since it was last saved.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color LineNumberChangedColor { get; set; } = Color.Red;

        [Category("Line Numbers")]
        [Description("Width of the separator line between line numbers and code (in pixels).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int LineNumberSeperatorWith
        {
            get;
            set
            {
                int v = value;
                if(v < 1)
                {
                    v = 1;
                }

                field = v;
                PNL_LineNumber.Invalidate();
            }
        } = 4;

        [Category("Line Numbers")]
        [Description("Dock position of the line number panel (left or right of the code area).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public LineNumberDockSide LineNumberDock
        {
            get;
            set
            {
                if(field == value)
                {
                    return;
                }

                field = value;
                ApplyLineNumberDock();
                PNL_LineNumber.Invalidate();
            }
        } = LineNumberDockSide.Left;

        private void ApplyLineNumberDock()
        {
            if(LineNumberDock == LineNumberDockSide.Left)
            {
                PNL_LineNumber.Dock = DockStyle.Left;
                RTB_Text.Dock = DockStyle.Fill;
            }
            else
            {
                PNL_LineNumber.Dock = DockStyle.Right;
                RTB_Text.Dock = DockStyle.Fill;
            }
        }

        // ---------------------------------------------------------
        //  Public properties – zoom / text
        // ---------------------------------------------------------

        [Category("Behaviour")]
        [Description("Minimum zoom factor (>= 0.1 and <= MaxZoomFactor).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float MinZoomFactor
        {
            get;
            set
            {
                float v = Math.Max(0.1f, value);
                if(v > MaxZoomFactor)
                {
                    v = MaxZoomFactor;
                }

                field = v;

                if(ZoomFactor < field)
                {
                    ZoomFactor = field;
                }
            }
        } = 0.5f;

        [Category("Behaviour")]
        [Description("Maximum zoom factor (<= 64 and >= MinZoomFactor).")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float MaxZoomFactor
        {
            get;
            set
            {
                float v = Math.Min(64f, value);
                if(v < MinZoomFactor)
                {
                    v = MinZoomFactor;
                }

                field = v;

                if(ZoomFactor > field)
                {
                    ZoomFactor = field;
                }
            }
        } = 5.0f;

        [Category("Behaviour")]
        [Description("Current zoom factor (1.0 = 100%). Affects both code and line numbers.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public float ZoomFactor
        {
            get => RTB_Text.ZoomFactor;
            set
            {
                float v = value;

                if(v < MinZoomFactor)
                {
                    v = MinZoomFactor;
                }

                if(v > MaxZoomFactor)
                {
                    v = MaxZoomFactor;
                }

                // Safety net for RichTextBox limits.
                if(v < 0.1f)
                {
                    v = 0.1f;
                }

                if(v > 64f)
                {
                    v = 64f;
                }

                RTB_Text.ZoomFactor = v;
                PNL_LineNumber.Invalidate();
            }
        }

        [Category("Data")]
        [Description("Text content of the code editor.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [AllowNull]
        public override string Text
        {
            get => RTB_Text.Text;
            set => RTB_Text.Text = value ?? string.Empty;
        }

        // ---------------------------------------------------------
        //  TextChanged – lightweight: repaint line numbers only
        // ---------------------------------------------------------

        private void RTB_Text_TextChanged(object? sender, EventArgs e)
        {
            // No full diff here – just trigger a repaint of line numbers and change markers.
            PNL_LineNumber.Invalidate();
        }

        // ---------------------------------------------------------
        //  Mouse wheel handling
        // ---------------------------------------------------------

        private void RTB_Text_MouseWheel(object? sender, MouseEventArgs e)
        {
            if((ModifierKeys & Keys.Control) == Keys.Control)
            {
                // RichTextBox handles the zoom internally – we just redraw the line numbers.
                PNL_LineNumber.Invalidate();
            }
        }

        private void PNL_LineNumber_MouseWheel(object? sender, MouseEventArgs e)
        {
            if((ModifierKeys & Keys.Control) == Keys.Control)
            {
                HandleZoomFromMouseWheel(e);
            }
            else
            {
                HandleScrollFromMouseWheel(e);
            }
        }

        private void HandleZoomFromMouseWheel(MouseEventArgs e)
        {
            const float step = 0.1f;

            if(e.Delta > 0)
            {
                ZoomFactor += step;
            }
            else
            {
                ZoomFactor -= step;
            }
        }

        private void HandleScrollFromMouseWheel(MouseEventArgs e)
        {
            int linesPerNotch = SystemInformation.MouseWheelScrollLines;

            if(linesPerNotch <= 0)
            {
                return;
            }

            int direction = e.Delta > 0 ? -1 : 1;
            int linesToScroll = direction * linesPerNotch;

            _ = SendMessage(
                RTB_Text.Handle,
                EM_LINESCROLL,
                IntPtr.Zero,
                (IntPtr)linesToScroll);

            // We *could* skip this Invalidate and rely only on RTB_Text.VScroll,
            // but keeping it here guarantees the panel updates even if the scroll
            // message does not raise VScroll for some reason.
            PNL_LineNumber.Invalidate();
        }

        // ---------------------------------------------------------
        //  Drawing line numbers
        // ---------------------------------------------------------

        /// <summary>
        /// Creates or updates the line number font based on the code font
        /// without triggering a repaint.
        /// </summary>
        private void RecreateLineNumberFont()
        {
            Font textFont = RTB_Text.Font;
            float size = Math.Max(1f, textFont.Size - 1f); // never smaller than 1

            LineNumberFont?.Dispose();
            LineNumberFont = new Font("Arial", size, textFont.Style);
        }

        private void SyncLineNumberFontFromText()
        {
            RecreateLineNumberFont();
            PNL_LineNumber.Invalidate();
        }

        private void PNL_LineNumber_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.Clear(LineNumberBackColor);

            string[] lines = RTB_Text.Lines;
            if(lines.Length == 0)
            {
                DrawSeparator(e);
                return;
            }

            if(LineNumberFont == null)
            {
                // Avoid calling SyncLineNumberFontFromText() here to prevent recursive invalidation.
                RecreateLineNumberFont();
            }

            Font baseFont = LineNumberFont!;

            // Draw base separator line over the full height.
            DrawSeparator(e);

            // Apply zoom to the line number font.
            float zoom = RTB_Text.ZoomFactor;
            using Font lnFont = new(
                baseFont.FontFamily,
                baseFont.Size * zoom,
                baseFont.Style);

            using SolidBrush brush = new(LineNumberForeColor);

            int sepWidth = LineNumberSeperatorWith <= 0 ? 1 : LineNumberSeperatorWith;
            int halfWidth = sepWidth / 2;

            // X position of the separator line (centre of the line).
            int separatorX = LineNumberDock == LineNumberDockSide.Left
                ? PNL_LineNumber.Width - halfWidth - 1
                : halfWidth;

            using Pen changedPen = new(LineNumberChangedColor, sepWidth);

            // First visible logical line index.
            int firstCharIndex = RTB_Text.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = RTB_Text.GetLineFromCharIndex(firstCharIndex);
            if(firstLine < 0)
            {
                firstLine = 0;
            }

            int lastLogicalLine = lines.Length - 1;
            float maxNumberWidth = 0f;
            int panelHeight = PNL_LineNumber.Height;

            for(int line = firstLine; line <= lastLogicalLine; line++)
            {
                int charIndex;

                // For the first visible line, use the character at the top of the control
                // (important for wrapped lines). For all other lines, use the logical line start.
                if(line == firstLine)
                {
                    charIndex = firstCharIndex;
                }
                else
                {
                    charIndex = RTB_Text.GetFirstCharIndexFromLine(line);
                    if(charIndex < 0)
                    {
                        continue;
                    }
                }

                Point pos = RTB_Text.GetPositionFromCharIndex(charIndex);
                float y = pos.Y;

                // Stop once we are below the visible area – only visible lines are processed.
                if(y > panelHeight)
                {
                    break;
                }

                string lineNumberText = (line + 1).ToString();

                SizeF size = e.Graphics.MeasureString(lineNumberText, lnFont);
                if(size.Width > maxNumberWidth)
                {
                    maxNumberWidth = size.Width;
                }

                float x = LineNumberDock == LineNumberDockSide.Left
                    ? PNL_LineNumber.Width - size.Width - 4
                    : 4;

                e.Graphics.DrawString(lineNumberText, lnFont, brush, x, y);

                // Decide whether this line differs from the saved snapshot.
                bool isChanged = false;

                if(_hasSavedSnapshot)
                {
                    if(line >= _savedLines.Length)
                    {
                        isChanged = true;
                    }
                    else if(!string.Equals(lines[line], _savedLines[line], StringComparison.Ordinal))
                    {
                        isChanged = true;
                    }
                }
                else
                {
                    // Before the first save: treat any non-empty line, or any line after the first,
                    // as "changed" compared to an empty document baseline.
                    if(!string.IsNullOrEmpty(lines[line]) || line > 0)
                    {
                        isChanged = true;
                    }
                }

                if(isChanged)
                {
                    int y1 = (int)y;
                    int y2 = (int)(y + size.Height);

                    e.Graphics.DrawLine(changedPen, separatorX, y1, separatorX, y2);
                }
            }

            // Adjust panel width so that numbers are not cut off.
            int desiredWidth = (int)Math.Ceiling(maxNumberWidth) + 8; // padding
            const int minWidth = 24;

            if(desiredWidth < minWidth)
            {
                desiredWidth = minWidth;
            }

            if(PNL_LineNumber.Width != desiredWidth)
            {
                PNL_LineNumber.Width = desiredWidth;
            }
        }

        private void DrawSeparator(PaintEventArgs e)
        {
            int sepWidth = LineNumberSeperatorWith <= 0 ? 1 : LineNumberSeperatorWith;
            int halfWidth = sepWidth / 2;

            // Centre of the separator line.
            int separatorX = LineNumberDock == LineNumberDockSide.Left
                ? PNL_LineNumber.Width - halfWidth - 1
                : halfWidth;

            using Pen pen = new(LineNumberSeperatorColor, sepWidth);
            e.Graphics.DrawLine(pen, separatorX, 0, separatorX, PNL_LineNumber.Height);
        }

        // ---------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                LineNumberFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}