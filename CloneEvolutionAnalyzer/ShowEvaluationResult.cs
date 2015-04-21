using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CloneEvolutionAnalyzer
{
    public partial class ShowEvaluationResult : Form
    {
        public ShowEvaluationResult()
        {
            InitializeComponent();
        }

        public void ShowFile()
        {
            string Filename = Global.GetProjectDirectory() + @"\\result_temp";
            StreamReader st = new StreamReader(Filename);
            string sLine = "";
            while (sLine != null)
            {
                sLine = st.ReadLine();
                this.richTextBox1.Text += sLine;
                this.richTextBox1.Text += "\n";
            }
            st.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Global.mainForm.treeView1.CheckBoxes = false;
            Global.mainForm.treeView1.ExpandAll();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
