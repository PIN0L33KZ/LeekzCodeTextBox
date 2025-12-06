namespace TestApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //leekzCodeTextBox1.CodeBackColor = Color.AliceBlue;
            //leekzCodeTextBox1.CodeForeColor = Color.Red;
            //leekzCodeTextBox1.CodeFont = new Font("Segoe Print", 12f, FontStyle.Bold);
            //leekzCodeTextBox1.LineNumberBackColor = Color.Black;
            //leekzCodeTextBox1.LineNumberForeColor = Color.White;
            //leekzCodeTextBox1.LineNumberSeperatorColor = Color.Gold;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            leekzCodeTextBox1.MarkAsSaved();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show(leekzCodeTextBox1.IsSaved().ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            leekzCodeTextBox1.CodeWordWrap = checkBox1.Checked;
        }
    }
}
