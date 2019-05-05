using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Update.Incremental;
using xuexue.utility.Incremental;

namespace Update
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("./update");
            DoUpdate.CreatConfigFile("./UpdateConfig.json");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IncrementalUpdate.CreateSoftVersionFile(@"C:\work\VRMachine\UpdateFile\v1.0\MRSystem", new uint[] { 1, 0, 0, 0 }, "http://127.0.0.1/download", @"C:\work\VRMachine\UpdateFile\v1.0\SoftVer.json");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DoUpdate doUpdate = new DoUpdate();
            doUpdate.Start("./UpdateConfig.json");
        }
    }
}
