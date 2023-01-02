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
            var f = new frmGrpcClient(txtGrpcAddress.Text, txtGRPCQueue.Text);
            f.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var f = new frmSignalRClient(txtSignalRAddress.Text, txtSignalRQQueue.Text);
            f.Show();
        }
    }
}
