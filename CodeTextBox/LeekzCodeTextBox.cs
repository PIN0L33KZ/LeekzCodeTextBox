using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodeTextBox;

public partial class LeekzCodeTextBox : UserControl
{
    private const int EM_LINESCROLL = 0x00B6;
    private const int WM_SETREDRAW = 0x000B;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public enum LineNumberDockSide
    {
        Left,
        Right
    }

    private string[] _savedLines = Array.Empty<string>();
    private bool _hasSavedSnapshot;

    private Color _lineNumberForeColor = Color.Gray;
    private Color _lineNumberSeparatorColor = Color.Silver;
    private int _lineNumberSeparatorWidth = 4;
    private LineNumberDockSide _lineNumberDock = LineNumberDockSide.Left;
    private float _minZoomFactor = 0.5f;
    private float _maxZoomFactor = 5.0f;

    private int _redrawSuspendCount;

    public LeekzCodeTextBox()
    {
        InitializeComponent();

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

        UpdateStyles();

        EnableDoubleBuffering(PNL_LineNumber);
        EnableDoubleBuffering(RTB_Text);

        RTB_Text.HideSelection = false;

        PNL_LineNumber.Paint += PNL_LineNumber_Paint;

        InitEvents();

        RTB_Text.MouseWheel += RTB_Text_MouseWheel;
        PNL_LineNumber.MouseWheel += PNL_LineNumber_MouseWheel;

        ApplyLineNumberDock();
    }

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
        RTB_Text.VScroll += RTB_Text_VScroll;
        RTB_Text.TextChanged += RTB_Text_TextChanged;
        RTB_Text.Resize += RTB_Text_Resize;
        RTB_Text.FontChanged += RTB_Text_FontChanged;
    }

    private void RTB_Text_VScroll(object? sender, EventArgs e)
    {
        PNL_LineNumber.Invalidate();
    }

    private void RTB_Text_Resize(object? sender, EventArgs e)
    {
        PNL_LineNumber.Invalidate();
    }

    private void RTB_Text_FontChanged(object? sender, EventArgs e)
    {
        SyncLineNumberFontFromText();
    }

    // ---------------------------------------------------------
    //  Public API – save / change tracking
    // ---------------------------------------------------------

    public void MarkAsSaved()
    {
        var lines = RTB_Text.Lines;

        _savedLines = new string[lines.Length];

        Array.Copy(lines, _savedLines, lines.Length);

        _hasSavedSnapshot = true;

        PNL_LineNumber.Invalidate();
    }

    public bool IsSaved()
    {
        if(!_hasSavedSnapshot)
        {
            return false;
        }

        var currentLines = RTB_Text.Lines;

        if(currentLines.Length != _savedLines.Length)
        {
            return false;
        }

        for(var i = 0; i < currentLines.Length; i++)
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
    [Description("Background colour of the code area.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color CodeBackColor
    {
        get => RTB_Text.BackColor;
        set => RTB_Text.BackColor = value;
    }

    [Category("Code")]
    [Description("Text colour of the code area.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color CodeForeColor
    {
        get => RTB_Text.ForeColor;
        set => RTB_Text.ForeColor = value;
    }

    [Category("Code")]
    [Description("Font used for the code area.")]
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
        get => _lineNumberForeColor;
        set
        {
            if(_lineNumberForeColor == value)
            {
                return;
            }

            _lineNumberForeColor = value;
            PNL_LineNumber.Invalidate();
        }
    }

    [Category("Line Numbers")]
    [Description("Font used to draw the line numbers.")]
    [Browsable(false)]
    public Font? LineNumberFont { get; private set; }

    [Category("Line Numbers")]
    [Description("Colour of the separator line between line numbers and code.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color LineNumberSeperatorColor
    {
        get => _lineNumberSeparatorColor;
        set
        {
            if(_lineNumberSeparatorColor == value)
            {
                return;
            }

            _lineNumberSeparatorColor = value;
            PNL_LineNumber.Invalidate();
        }
    }

    [Category("Line Numbers")]
    [Description("Colour of separator segments when a line has changed since it was last saved.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color LineNumberChangedColor { get; set; } = Color.Red;

    [Category("Line Numbers")]
    [Description("Width of the separator line between line numbers and code in pixels.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int LineNumberSeperatorWith
    {
        get => _lineNumberSeparatorWidth;
        set
        {
            var safeValue = value < 1 ? 1 : value;

            if(_lineNumberSeparatorWidth == safeValue)
            {
                return;
            }

            _lineNumberSeparatorWidth = safeValue;
            PNL_LineNumber.Invalidate();
        }
    }

    [Category("Line Numbers")]
    [Description("Dock position of the line number panel.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public LineNumberDockSide LineNumberDock
    {
        get => _lineNumberDock;
        set
        {
            if(_lineNumberDock == value)
            {
                return;
            }

            _lineNumberDock = value;
            ApplyLineNumberDock();
            PNL_LineNumber.Invalidate();
        }
    }

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
    [Description("Minimum zoom factor.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float MinZoomFactor
    {
        get => _minZoomFactor;
        set
        {
            var safeValue = Math.Max(0.1f, value);

            if(safeValue > MaxZoomFactor)
            {
                safeValue = MaxZoomFactor;
            }

            if(Math.Abs(_minZoomFactor - safeValue) < float.Epsilon)
            {
                return;
            }

            _minZoomFactor = safeValue;

            if(ZoomFactor < _minZoomFactor)
            {
                ZoomFactor = _minZoomFactor;
            }
        }
    }

    [Category("Behaviour")]
    [Description("Maximum zoom factor.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float MaxZoomFactor
    {
        get => _maxZoomFactor;
        set
        {
            var safeValue = Math.Min(64f, value);

            if(safeValue < MinZoomFactor)
            {
                safeValue = MinZoomFactor;
            }

            if(Math.Abs(_maxZoomFactor - safeValue) < float.Epsilon)
            {
                return;
            }

            _maxZoomFactor = safeValue;

            if(ZoomFactor > _maxZoomFactor)
            {
                ZoomFactor = _maxZoomFactor;
            }
        }
    }

    [Category("Behaviour")]
    [Description("Current zoom factor. 1.0 equals 100 percent.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float ZoomFactor
    {
        get => RTB_Text.ZoomFactor;
        set
        {
            var safeValue = value;

            if(safeValue < MinZoomFactor)
            {
                safeValue = MinZoomFactor;
            }

            if(safeValue > MaxZoomFactor)
            {
                safeValue = MaxZoomFactor;
            }

            if(safeValue < 0.1f)
            {
                safeValue = 0.1f;
            }

            if(safeValue > 64f)
            {
                safeValue = 64f;
            }

            RTB_Text.ZoomFactor = safeValue;
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
    //  Public API – selection / highlighting / caret
    // ---------------------------------------------------------

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TextLength => RTB_Text.TextLength;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionStart
    {
        get => RTB_Text.SelectionStart;
        set => RTB_Text.SelectionStart = ClampTextIndex(value);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectionLength
    {
        get => RTB_Text.SelectionLength;
        set => RTB_Text.SelectionLength = ClampSelectionLength(RTB_Text.SelectionStart, value);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectionBackColor
    {
        get => RTB_Text.SelectionBackColor;
        set => RTB_Text.SelectionBackColor = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SelectedText
    {
        get => RTB_Text.SelectedText;
        set => RTB_Text.SelectedText = value ?? string.Empty;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HideSelection
    {
        get => RTB_Text.HideSelection;
        set => RTB_Text.HideSelection = value;
    }

    public new void Select()
    {
        RTB_Text.Select();
    }

    public void Select(int start, int length)
    {
        var safeStart = ClampTextIndex(start);
        var safeLength = ClampSelectionLength(safeStart, length);

        RTB_Text.Select(safeStart, safeLength);
    }

    public void SetSelectionBackColor(int start, int length, Color color)
    {
        var safeStart = ClampTextIndex(start);
        var safeLength = ClampSelectionLength(safeStart, length);

        if(safeLength <= 0)
        {
            return;
        }

        RTB_Text.Select(safeStart, safeLength);
        RTB_Text.SelectionBackColor = color;
    }

    public void ResetSelectionBackColor(int start, int length)
    {
        var safeStart = ClampTextIndex(start);
        var safeLength = ClampSelectionLength(safeStart, length);

        if(safeLength <= 0)
        {
            return;
        }

        RTB_Text.Select(safeStart, safeLength);
        RTB_Text.SelectionBackColor = RTB_Text.BackColor;
    }

    public void ScrollToCaret()
    {
        RTB_Text.ScrollToCaret();
        PNL_LineNumber.Invalidate();
    }

    public bool FocusCodeArea()
    {
        return RTB_Text.Focus();
    }

    public void BeginUpdate()
    {
        _redrawSuspendCount++;

        if(_redrawSuspendCount > 1)
        {
            return;
        }

        if(RTB_Text.IsHandleCreated)
        {
            _ = SendMessage(RTB_Text.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        if(PNL_LineNumber.IsHandleCreated)
        {
            _ = SendMessage(PNL_LineNumber.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public void EndUpdate()
    {
        if(_redrawSuspendCount <= 0)
        {
            return;
        }

        _redrawSuspendCount--;

        if(_redrawSuspendCount > 0)
        {
            return;
        }

        if(RTB_Text.IsHandleCreated)
        {
            _ = SendMessage(RTB_Text.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
        }

        if(PNL_LineNumber.IsHandleCreated)
        {
            _ = SendMessage(PNL_LineNumber.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
        }

        RTB_Text.Invalidate();
        PNL_LineNumber.Invalidate();
        Invalidate();
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);

        if(!RTB_Text.Focused)
        {
            _ = RTB_Text.Focus();
        }
    }

    private int ClampTextIndex(int index)
    {
        return index < 0 ? 0 : index > RTB_Text.TextLength ? RTB_Text.TextLength : index;
    }

    private int ClampSelectionLength(int start, int length)
    {
        if(length < 0)
        {
            return 0;
        }

        var availableLength = RTB_Text.TextLength - start;

        return length > availableLength ? availableLength : length;
    }

    // ---------------------------------------------------------
    //  TextChanged
    // ---------------------------------------------------------

    private void RTB_Text_TextChanged(object? sender, EventArgs e)
    {
        PNL_LineNumber.Invalidate();
    }

    // ---------------------------------------------------------
    //  Mouse wheel handling
    // ---------------------------------------------------------

    private void RTB_Text_MouseWheel(object? sender, MouseEventArgs e)
    {
        if((ModifierKeys & Keys.Control) == Keys.Control)
        {
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
        var linesPerNotch = SystemInformation.MouseWheelScrollLines;

        if(linesPerNotch <= 0)
        {
            return;
        }

        var direction = e.Delta > 0 ? -1 : 1;
        var linesToScroll = direction * linesPerNotch;

        _ = SendMessage(
            RTB_Text.Handle,
            EM_LINESCROLL,
            IntPtr.Zero,
            linesToScroll);

        PNL_LineNumber.Invalidate();
    }

    // ---------------------------------------------------------
    //  Drawing line numbers
    // ---------------------------------------------------------

    private void RecreateLineNumberFont()
    {
        Font textFont = RTB_Text.Font;
        var size = Math.Max(1f, textFont.Size - 1f);

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

        var lines = RTB_Text.Lines;

        if(lines.Length == 0)
        {
            DrawSeparator(e);
            return;
        }

        if(LineNumberFont == null)
        {
            RecreateLineNumberFont();
        }

        Font baseFont = LineNumberFont;

        DrawSeparator(e);

        var zoom = RTB_Text.ZoomFactor;

        using Font lineNumberFont = new(
            baseFont.FontFamily,
            baseFont.Size * zoom,
            baseFont.Style);

        using SolidBrush brush = new(LineNumberForeColor);

        var separatorWidth = LineNumberSeperatorWith <= 0 ? 1 : LineNumberSeperatorWith;
        var halfWidth = separatorWidth / 2;

        var separatorX = LineNumberDock == LineNumberDockSide.Left
            ? PNL_LineNumber.Width - halfWidth - 1
            : halfWidth;

        using Pen changedPen = new(LineNumberChangedColor, separatorWidth);

        var firstCharIndex = RTB_Text.GetCharIndexFromPosition(new Point(0, 0));
        var firstLine = RTB_Text.GetLineFromCharIndex(firstCharIndex);

        if(firstLine < 0)
        {
            firstLine = 0;
        }

        var lastLogicalLine = lines.Length - 1;
        var maxNumberWidth = 0f;
        var panelHeight = PNL_LineNumber.Height;

        for(var line = firstLine; line <= lastLogicalLine; line++)
        {
            int charIndex;

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

            Point position = RTB_Text.GetPositionFromCharIndex(charIndex);
            float y = position.Y;

            if(y > panelHeight)
            {
                break;
            }

            var lineNumberText = (line + 1).ToString();

            SizeF size = e.Graphics.MeasureString(lineNumberText, lineNumberFont);

            if(size.Width > maxNumberWidth)
            {
                maxNumberWidth = size.Width;
            }

            var x = LineNumberDock == LineNumberDockSide.Left
                ? PNL_LineNumber.Width - size.Width - 4
                : 4;

            e.Graphics.DrawString(lineNumberText, lineNumberFont, brush, x, y);

            var isChanged = false;

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
                if(!string.IsNullOrEmpty(lines[line]) || line > 0)
                {
                    isChanged = true;
                }
            }

            if(isChanged)
            {
                var y1 = (int)y;
                var y2 = (int)(y + size.Height);

                e.Graphics.DrawLine(changedPen, separatorX, y1, separatorX, y2);
            }
        }

        var desiredWidth = (int)Math.Ceiling(maxNumberWidth) + 8;
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
        var separatorWidth = LineNumberSeperatorWith <= 0 ? 1 : LineNumberSeperatorWith;
        var halfWidth = separatorWidth / 2;

        var separatorX = LineNumberDock == LineNumberDockSide.Left
            ? PNL_LineNumber.Width - halfWidth - 1
            : halfWidth;

        using Pen pen = new(LineNumberSeperatorColor, separatorWidth);

        e.Graphics.DrawLine(
            pen,
            separatorX,
            0,
            separatorX,
            PNL_LineNumber.Height);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing)
        {
            LineNumberFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}