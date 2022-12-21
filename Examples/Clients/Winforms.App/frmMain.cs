using GrpcClient2;

namespace GrpcClient4
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var f = new Form1(textBox1.Text, txtQueue.Text);
            f.Show();
        }
    }
}
