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
    public partial class ShowExtractResult : Form
    {
        public ShowExtractResult()
        {
            InitializeComponent();
        }

        public void ShowFile()
        {
            string Filename = Global.GetProjectDirectory() + @"\\extract_temp";
            StreamReader st = new StreamReader(Filename);
            string sLine = "";
            while(sLine  != null)
            {
                sLine = st.ReadLine();
                this.richTextBox1.Text += sLine;
                this.richTextBox1.Text += "\n";
            }
            st.Close();
            //FileStream fs = new FileStream(Filename, FileMode.Open, FileAccess.Read);
            //StreamReader sr = new StreamReader(fs);
            //sr.BaseStream.Seek(0, SeekOrigin.Begin);
            //String str = str.ReadToEnd();
            //this.textBox1.Text = str;
            //sr.Close();
            //fs.Close();
        }

 

        private void button1_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Global.mainForm.treeView1.CheckBoxes = false;
            Global.mainForm.treeView1.ExpandAll();
        }


 
    }
}
