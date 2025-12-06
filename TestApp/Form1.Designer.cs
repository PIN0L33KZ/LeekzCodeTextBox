namespace TestApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            button2 = new Button();
            checkBox1 = new CheckBox();
            leekzCodeTextBox1 = new CodeTextBox.LeekzCodeTextBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 22);
            button1.Name = "button1";
            button1.Size = new Size(111, 23);
            button1.TabIndex = 1;
            button1.Text = "Mark as saved";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(129, 22);
            button2.Name = "button2";
            button2.Size = new Size(111, 23);
            button2.TabIndex = 1;
            button2.Text = "Is saved?";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(246, 26);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(83, 19);
            checkBox1.TabIndex = 2;
            checkBox1.Text = "WordWrap";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // leekzCodeTextBox1
            // 
            leekzCodeTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            leekzCodeTextBox1.CodeBackColor = SystemColors.Window;
            leekzCodeTextBox1.CodeFont = new Font("Segoe UI", 9F);
            leekzCodeTextBox1.CodeForeColor = SystemColors.WindowText;
            leekzCodeTextBox1.CodeWordWrap = false;
            leekzCodeTextBox1.LineNumberBackColor = SystemColors.Control;
            leekzCodeTextBox1.LineNumberChangedColor = Color.Red;
            leekzCodeTextBox1.LineNumberDock = CodeTextBox.LeekzCodeTextBox.LineNumberDockSide.Left;
            leekzCodeTextBox1.LineNumberForeColor = Color.Gray;
            leekzCodeTextBox1.LineNumberSeperatorColor = Color.Silver;
            leekzCodeTextBox1.LineNumberSeperatorWith = 4;
            leekzCodeTextBox1.Location = new Point(12, 51);
            leekzCodeTextBox1.MaxZoomFactor = 5F;
            leekzCodeTextBox1.MinZoomFactor = 0.5F;
            leekzCodeTextBox1.Name = "leekzCodeTextBox1";
            leekzCodeTextBox1.Size = new Size(776, 387);
            leekzCodeTextBox1.TabIndex = 3;
            leekzCodeTextBox1.Text = "leekzCodeTextBox1";
            leekzCodeTextBox1.ZoomFactor = 1F;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(leekzCodeTextBox1);
            Controls.Add(checkBox1);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button button1;
        private Button button2;
        private CheckBox checkBox1;
        private CodeTextBox.LeekzCodeTextBox leekzCodeTextBox1;
    }
}
