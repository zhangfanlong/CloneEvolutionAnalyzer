using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Collections;

namespace CloneEvolutionAnalyzer
{
    public partial class ShowCodeForm : Form
    {
        public bool IsShowFullCode { get; set; }    //是否显示完整代码
        private CloneSourceInfo cloneSourceInfo;    //保存克隆代码的信息
        private List<string> fullCode;     //保存文件完整代码
        private List<string> cloneFragment;    //保存克隆代码段
        public ShowCodeForm()
        {
            InitializeComponent();
            //添加的代码不写在InitializeComponent中，因为在自动生成代码区域内，会被修改掉
            this.richTextBox_lineNo.SetOtherRichTextBox(this.richTextBox_Code);
            this.richTextBox_Code.SetOtherRichTextBox(this.richTextBox_lineNo);
        }

        public void SetCloneSourceInfo(CloneSourceInfo info)
        {
            cloneSourceInfo = new CloneSourceInfo();
            cloneSourceInfo = info;
        }

        //覆盖基类Show方法
        public new void Show()
        {
            GetCode();
            if (IsShowFullCode)
            {
                ShowFullCode();
            }
            else
            {
                ShowCloneFragment();
            }
            base.Show();
        }

        private void GetCode()
        {
            string fullFileName = Global.mainForm.subSysDirectory + "\\" + cloneSourceInfo.sourcePath;
            this.label_fileName.Text = fullFileName;
            this.fullCode = Global.GetFileContent(fullFileName);
            this.cloneFragment = CloneRegionDescriptor.GetCFSourceFromSourcInfo(this.fullCode, cloneSourceInfo);
        }
        
        private void ShowCloneFragment()
        {
            int lineNo = cloneSourceInfo.startLine - 1; //显示克隆代码的行号区间（不从1开始）
            //在RichTextBox中显示
            foreach (string line in this.cloneFragment)
            {
                lineNo++;
                this.richTextBox_lineNo.AppendText(lineNo.ToString()+"\r\n");  //显示行号
                this.richTextBox_Code.AppendText(line+"\r\n"); //显示行文本（WordWrap属性设为false，取消自动换行）
            }
        }

        private void ShowFullCode()
        {
            int lineNo = 0;
            foreach (string line in this.fullCode)
            {
                lineNo++;
                //以蓝色显示克隆代码段，黑色显示其余部分
                if (lineNo >= cloneSourceInfo.startLine && lineNo <= cloneSourceInfo.endLine)
                {
                    this.richTextBox_lineNo.SelectionBackColor = Color.LightYellow;
                    this.richTextBox_lineNo.SelectionColor = Color.Blue;
                    this.richTextBox_lineNo.AppendText(lineNo.ToString() + "\r\n");

                    this.richTextBox_Code.SelectionBackColor = Color.LightYellow;
                    this.richTextBox_Code.SelectionColor = Color.Blue;
                    this.richTextBox_Code.AppendText(line + "\r\n");
                }
                else
                {
                    this.richTextBox_lineNo.AppendText(lineNo.ToString() + "\r\n");  //显示行号
                    this.richTextBox_Code.AppendText(line + "\r\n"); 
                }
            }
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ShowCodeForm_Load(object sender, EventArgs e)
        {

        }

        private void richTextBox_Code_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
