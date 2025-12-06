namespace CodeTextBox
{
    partial class LeekzCodeTextBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components;

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            PNL_LineNumber = new Panel();
            RTB_Text = new RichTextBox();
            SuspendLayout();
            // 
            // PNL_LineNumber
            // 
            PNL_LineNumber.Dock = DockStyle.Left;
            PNL_LineNumber.Location = new Point(0, 0);
            PNL_LineNumber.Name = "PNL_LineNumber";
            PNL_LineNumber.Size = new Size(34, 450);
            PNL_LineNumber.TabIndex = 0;
            // 
            // RTB_Text
            // 
            RTB_Text.BorderStyle = BorderStyle.None;
            RTB_Text.Dock = DockStyle.Fill;
            RTB_Text.Location = new Point(34, 0);
            RTB_Text.Name = "RTB_Text";
            RTB_Text.Size = new Size(766, 450);
            RTB_Text.TabIndex = 1;
            RTB_Text.Text = "";
            RTB_Text.WordWrap = false;
            // 
            // LeekzCodeTextBox
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(RTB_Text);
            Controls.Add(PNL_LineNumber);
            Name = "LeekzCodeTextBox";
            Size = new Size(800, 450);
            ResumeLayout(false);
        }

        #endregion

        private Panel PNL_LineNumber;
        private RichTextBox RTB_Text;
    }
}