using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CloneEvolutionAnalyzer
{
    public partial class ShowDiffForm : Form
    {
        public ShowDiffForm()
        {
            InitializeComponent();
            //下面代码不要写在InitializeComponent方法中，会被自动修改掉
            this.richTextBox1.SetOtherRichTextBox(this.richTextBox3);
            this.richTextBox3.SetOtherRichTextBox(this.richTextBox1);
            this.richTextBox2.SetOtherRichTextBox(this.richTextBox4);
            this.richTextBox4.SetOtherRichTextBox(this.richTextBox2);
        }
        /// <summary>
        /// 显示两个完整文件diff的结果（行号从1开始）
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <param name="diffInfo"></param>
        public void ShowFileDiff(string file1, string file2, Diff.DiffInfo diffInfo)
        {
            this.label1.Text = file1;
            this.label2.Text = file2;
            int lineNo1 = 0;
            int lineNo2 = 0;

            this.richTextBox1.Text = "";

            this.richTextBox3.Text = "";
            foreach (Object item in diffInfo)
            {
                if (item is string) //相同的行
                {
                    lineNo1++;
                    this.richTextBox1.SelectionColor = Color.Black;
                    this.richTextBox1.AppendText(lineNo1.ToString() + "\r\n");//构建file1行号字符串

                    this.richTextBox3.SelectionColor = Color.Black;
                    this.richTextBox3.AppendText((string)item + "\r\n"); //构建file1内容字符串

                    lineNo2++;
                    this.richTextBox2.SelectionColor = Color.Black;
                    this.richTextBox2.AppendText(lineNo2.ToString() + "\r\n");//构建file2行号字符串

                    this.richTextBox4.SelectionColor = Color.Black;
                    this.richTextBox4.AppendText((string)item + "\r\n"); //构建file2内容字符串
                }
                else//冲突的行
                {
                    if (((Diff.ConfictItem)item).contentA != null)
                    {
                        foreach (string line in ((Diff.ConfictItem)item).contentA)
                        {
                            lineNo1++;
                            this.richTextBox1.SelectionColor = Color.Red;
                            this.richTextBox1.SelectionBackColor = Color.LightYellow;
                            this.richTextBox1.AppendText(lineNo1.ToString() + "\r\n");

                            this.richTextBox3.SelectionColor = Color.Red;   //设定新增行颜色为红色（冲突行用红色显示）
                            this.richTextBox3.SelectionBackColor = Color.LightYellow;
                            //this.richTextBox3.SelectionFont = FontStyle.Bold;    //设定新增行加粗
                            this.richTextBox3.AppendText(line + "\r\n");
                        }
                    }
                    if (((Diff.ConfictItem)item).contentB != null)
                    {
                        foreach (string line in ((Diff.ConfictItem)item).contentB)
                        {
                            lineNo2++;
                            this.richTextBox2.SelectionColor = Color.Red;   //设定新增行颜色为红色
                            this.richTextBox2.SelectionBackColor = Color.LightYellow;
                            this.richTextBox2.AppendText(lineNo2.ToString() + "\r\n");
                            //this.richTextBox4.ForeColor = Color.Red;
                            this.richTextBox4.SelectionColor = Color.Red;
                            this.richTextBox4.SelectionBackColor = Color.LightYellow;
                            this.richTextBox4.AppendText(line + "\r\n");
                        }
                    }
                }
            }
            this.Show();
        }

        /// <summary>
        /// 显示两段克隆代码diff的结果（行号从startline开始）（与showFileDiff唯一区别）
        /// </summary>
        /// <param name="info1">输入参数为CloneSourceInfo对象</param>
        /// <param name="info2"></param>
        /// <param name="diffInfo"></param>
        internal void ShowCloneFragmentDiff(CloneSourceInfo info1, CloneSourceInfo info2, Diff.DiffInfo diffInfo)
        {
            this.label1.Text = Global.mainForm.subSysDirectory + "\\" + info1.sourcePath;
            this.label2.Text = Global.mainForm.subSysDirectory + "\\" + info2.sourcePath;
            int lineNo1 = info1.startLine - 1;
            int lineNo2 = info2.startLine - 1;

            this.richTextBox1.Text = "";

            this.richTextBox3.Text = "";
            foreach (Object item in diffInfo)
            {
                if (item is string) //相同的行
                {
                    lineNo1++;
                    this.richTextBox1.SelectionColor = Color.Black;
                    this.richTextBox1.AppendText(lineNo1.ToString() + "\r\n");//构建file1行号字符串

                    this.richTextBox3.SelectionColor = Color.Black;
                    this.richTextBox3.AppendText((string)item + "\r\n"); //构建file1内容字符串

                    lineNo2++;
                    this.richTextBox2.SelectionColor = Color.Black;
                    this.richTextBox2.AppendText(lineNo2.ToString() + "\r\n");//构建file2行号字符串

                    this.richTextBox4.SelectionColor = Color.Black;
                    this.richTextBox4.AppendText((string)item + "\r\n"); //构建file2内容字符串
                }
                else//冲突的行
                {
                    if (((Diff.ConfictItem)item).contentA != null)
                    {
                        foreach (string line in ((Diff.ConfictItem)item).contentA)
                        {
                            lineNo1++;
                            this.richTextBox1.SelectionColor = Color.Red;
                            this.richTextBox1.SelectionBackColor = Color.LightYellow;
                            this.richTextBox1.AppendText(lineNo1.ToString() + "\r\n");

                            this.richTextBox3.SelectionColor = Color.Red;   //设定新增行颜色为红色（冲突行用红色显示）
                            this.richTextBox3.SelectionBackColor = Color.LightYellow;
                            //this.richTextBox3.SelectionFont = FontStyle.Bold;    //设定新增行加粗
                            this.richTextBox3.AppendText(line + "\r\n");
                        }
                    }
                    if (((Diff.ConfictItem)item).contentB != null)
                    {
                        foreach (string line in ((Diff.ConfictItem)item).contentB)
                        {
                            lineNo2++;
                            this.richTextBox2.SelectionColor = Color.Red;   //设定新增行颜色为红色
                            this.richTextBox2.SelectionBackColor = Color.LightYellow;
                            this.richTextBox2.AppendText(lineNo2.ToString() + "\r\n");
                            //this.richTextBox4.ForeColor = Color.Red;
                            this.richTextBox4.SelectionColor = Color.Red;
                            this.richTextBox4.SelectionBackColor = Color.LightYellow;
                            this.richTextBox4.AppendText(line + "\r\n");
                        }
                    }
                }
            }
            this.Show();
        }
    }
}
