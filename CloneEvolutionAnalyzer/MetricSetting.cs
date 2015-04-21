using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace CloneEvolutionAnalyzer
{
    public partial class MetricSettingForm : Form
    {
        public MetricSettingForm()
        {
            InitializeComponent();
        }

        //符合条件为1不符合为0
        private int judge(string texta, string textb, string matrix)
        {
            if ((texta != "") && (textb != ""))
            {
                if ((float.Parse(matrix) < float.Parse(texta)) || (float.Parse(matrix) > float.Parse(textb)))
                    return 0;
                else
                    return 1;                    
            }
            else if ((texta == "") && (textb != ""))
            {
                if (float.Parse(matrix) <= float.Parse(textb))
                    return 1;
                else
                    return 0; 
            }
            else if ((texta != "") && (textb == ""))
            {
                if (float.Parse(matrix) >= float.Parse(texta))
                    return 1;
                else
                    return 0;
            }

            else
                return 1;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Clustering pre = new Clustering();
            pre.CreatMatrix();
            List<XmlElement> dest_xml = new List<XmlElement>();
            for (int k = 0; k < (pre.xtemp_matrix.Count - 1); k++)
            {
                if (judge(textBox1.Text, textBox2.Text, pre.xtemp_matrix[k + 1][1]) == 0)
                    continue;
                if (judge(textBox3.Text, textBox4.Text, pre.xtemp_matrix[k + 1][7]) == 0)
                    continue;
                if (judge(textBox5.Text, textBox6.Text, pre.xtemp_matrix[k + 1][8]) == 0)
                    continue;
                if (judge(textBox7.Text, textBox8.Text, pre.xtemp_matrix[k + 1][9]) == 0)
                    continue;
                if (judge(textBox9.Text, textBox10.Text, pre.xtemp_matrix[k + 1][10]) == 0)
                    continue;
                if (judge(textBox11.Text, textBox12.Text, pre.xtemp_matrix[k + 1][11]) == 0)
                    continue;
                if (judge(textBox13.Text, textBox14.Text, pre.xtemp_matrix[k + 1][12]) == 0)
                    continue;
                if (judge(textBox15.Text, textBox16.Text, pre.xtemp_matrix[k + 1][13]) == 0)
                    continue;
                if (judge(textBox17.Text, textBox18.Text, pre.xtemp_matrix[k + 1][14]) == 0)
                    continue;
                dest_xml.Add(pre.CFinfo[k]);
            }
            Global.mainForm.tabControl1.TabPages.Add(" ");//创建新的tabPage，显示文件名
            Global.mainForm.tabControl1.SelectedTab = Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;

            if (dest_xml.Count == 0)
                MessageBox.Show("No match");
            else
            {
                FragmentSettingForm sxml = new FragmentSettingForm();
                sxml.ShowXml(dest_xml, ref newTreeView);
            }

            this.Close();
            newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(Global.mainForm.newTreeView_NodeMouseClick);
            //添加预定义的上下文菜单
            newTreeView.ContextMenuStrip = Global.mainForm.contextMenuStrip1;
            newTreeView.ExpandAll();
        }

       
    }
}
