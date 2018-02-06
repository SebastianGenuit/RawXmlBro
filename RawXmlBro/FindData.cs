using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyXmlBrowser
{
    public partial class FindData : Form
    {
        public FindData()
        {
            InitializeComponent();
        }

        public string Command
        {
            set { this.textBox1.Text = value; }
            get { return this.textBox1.Text; }
        }

        private void button_Ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
