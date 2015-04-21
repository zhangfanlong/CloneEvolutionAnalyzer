using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;


namespace CloneEvolutionAnalyzer
{
    public partial class FragmentSettingForm : Form
    {
        public static List<string> SrcFragment = new List<string>();
        public static string SrcPath;
        public static int flag = 0;

        public FragmentSettingForm()
        {
            InitializeComponent();
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            SrcFragment.Clear();
            OpenFileDialog OpenFileDialog1 = new OpenFileDialog();
            OpenFileDialog1.ShowDialog();
            //获取打开文件夹的路径
            textBox1.Text = OpenFileDialog1.FileName;
            SrcPath = textBox1.Text;
            string[] data = File.ReadAllLines(OpenFileDialog1.FileName);
            for (int i = 0; i < data.Length; i++)
                SrcFragment.Add(data[i]); 

        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        //检索是否存在与给定代码片段相克隆的代码片段
        //首先在CRD文件中搜索文本相似度大于阈值的代码片段(取每个克隆群的首个代码片段计算文本相似度)
        //！！！添加验证：与检测出来的克隆类互为MAP的克隆群，需对其检测是否已经检测出来，如果没检测出来则添加
        private void button2_Click(object sender, EventArgs e)
        {
            CCSetForm.flag = 0;                
            flag = 1;
            Confirm(SrcFragment);
        }

        public void Confirm(List<string> SrcFragmentlist)
        {            
            List<XmlElement> dest_xml = new List<XmlElement>();
            //string[] fileList = System.IO.Directory.GetFileSystemEntries(Global.mainForm._folderPath + @"\blocks\");
            List<String> fileList = new List<String>();
            DirectoryInfo dir = new DirectoryInfo(Global.mainForm._folderPath + @"\blocks\");
            FileInfo[] genFiles = dir.GetFiles();
            foreach (FileInfo info in genFiles)
            {
                if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    continue;
                else
                    fileList.Add(info.Name);
            }
            for (int i = 0; i < fileList.Count(); i++)
            {
                XmlDocument crd_Xml = new XmlDocument();
                crd_Xml.Load(Global.mainForm._folderPath + @"\blocks\" + fileList[i]);
                foreach (XmlElement classNode in crd_Xml.DocumentElement.SelectNodes("class"))
                {
                    List<string> fullSource = new List<string>();
                    string sourcePath = ((XmlElement)classNode.ChildNodes[0]).GetAttribute("file");
                    //获取目标系统文件夹起始路径
                    string subSysStartPath = Global.mainForm.subSysDirectory;
                    //绝对路径=起始路径+相对路径
                    string subSysPath = subSysStartPath + "\\" + sourcePath;
                    //获得源文件代码
                    fullSource = Global.GetFileContent(subSysPath);
                    //提取对应行号的代码片段
                    List<string> DestFragment = new List<string>();
                    int startLine = int.Parse(((XmlElement)classNode.ChildNodes[0]).GetAttribute("startline"));
                    int endLine = int.Parse(((XmlElement)classNode.ChildNodes[0]).GetAttribute("endline"));
                    for (int j = startLine - 1; j < endLine; j++) //注意，索引从0起算，而行号从1起算
                        DestFragment.Add(fullSource[j]);

                    //使用Diff类计算两段代码的相似度
                    Diff.UseDefaultStrSimTh();  //使用行相似度阈值默认值0.5
                    Diff.DiffInfo diffFile = Diff.DiffFiles(SrcFragmentlist, DestFragment);
                    float sim = Diff.FileSimilarity(diffFile, SrcFragmentlist.Count, DestFragment.Count, true);

                    //判断相似度sim，超过阈值0.5即为候选克隆代码
                    if (sim >= 0.5)
                        dest_xml.Add(classNode);

                }

            }
            Global.mainForm.tabControl1.TabPages.Add("result");//创建新的tabPage，显示文件名
            Global.mainForm.tabControl1.SelectedTab = Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;

            if (dest_xml.Count == 0)
                MessageBox.Show("No match");
            else
                ShowXml(dest_xml, ref newTreeView);
            
            newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(Global.mainForm.newTreeView_NodeMouseClick1);
            //添加预定义的上下文菜单
            newTreeView.ContextMenuStrip = Global.mainForm.contextMenuStrip3;
            //Global.mainForm.contextMenuStrip3.Items[0].Enabled = true;    //ViewCloneFragment项
            //Global.mainForm.contextMenuStrip3.Items[1].Enabled = true;    //ViewFullCode项
            //Global.mainForm.contextMenuStrip3.Items[2].Enabled = true;   //ViewSourceDiff项

            newTreeView.ExpandAll();
            this.Close();
        }
        public void ShowXml(List<XmlElement> xmlNode, ref TreeView treeViewControl)
        {
            treeViewControl.Nodes.Clear();
            TreeNode treeRoot = new TreeNode("ShowResult(" + xmlNode.Count.ToString() + ")");
            treeViewControl.Nodes.Add(treeRoot);
            //由根节点的孩子节点名称，判断要显示的文件内容
            for (int i = 0; i < xmlNode.Count; i++)
            {               
                if (xmlNode[i].Name == "class")    //显示的是-classes.xml或-withCRD.xml文件
                {
                    TreeNode treeNode = new TreeNode(((XmlElement)xmlNode[i].ParentNode.SelectSingleNode("systeminfo")).GetAttribute("system"));
                    treeNode.Name = ((XmlElement)xmlNode[i].ParentNode).GetAttribute("system");   //树节点名字为版本名
                    TreeNode attr1 = new TreeNode("classid = " + xmlNode[i].GetAttribute("classid"));
                    treeNode.Nodes.Add(attr1);

                    AddNode(xmlNode[i], attr1);
                    treeNode.Expand();
                    treeViewControl.Nodes[0].Nodes.Add(treeNode);
                    treeViewControl.Nodes[0].Expand();
                }
                else if (xmlNode[i].Name == "source")
                {
                    TreeNode tNode = new TreeNode(((XmlNode)xmlNode[i]).OuterXml.Substring(0, ((XmlNode)xmlNode[i]).OuterXml.IndexOf(">")));
                    tNode.Name = xmlNode[i].Name;    //保存节点名称（注：名称与Text不同）                        

                    CloneSourceInfo info = new CloneSourceInfo();
                    info.sourcePath = xmlNode[i].Attributes[0].Value;
                    Int32.TryParse(xmlNode[i].Attributes[1].Value, out info.startLine);
                    Int32.TryParse(xmlNode[i].Attributes[2].Value, out info.endLine);
                    tNode.Tag = info;

                    treeViewControl.Nodes[0].Nodes.Add(tNode);  //添加新的树节点并获取其索引号
                  
                   //!!!!!!!! treeNode.Text = ((XmlNode)xmlNode[i]).OuterXml.Substring(0, ((XmlNode)xmlNode[i]).OuterXml.IndexOf(">"));
                 
                }
            }
           
        }

        public void AddNode(XmlNode inXmlNode, TreeNode inTreeNode)
        {
            XmlNodeList childNodes;
            if (inXmlNode.HasChildNodes)
            {
                childNodes = inXmlNode.ChildNodes;
                foreach (XmlNode xNode in childNodes)
                {
                    TreeNode tNode = new TreeNode(xNode.Name);
                    tNode.Name = xNode.Name;    //保存节点名称（注：名称与Text不同）
                    //在Tag中保存必要的信息
                    if (xNode.Name == "source")
                    {
                        CloneSourceInfo info = new CloneSourceInfo();
                        info.sourcePath = xNode.Attributes[0].Value;
                        Int32.TryParse(xNode.Attributes[1].Value, out info.startLine);
                        Int32.TryParse(xNode.Attributes[2].Value, out info.endLine);
                        tNode.Tag = info;
                    }                   
                    int index = inTreeNode.Nodes.Add(tNode);  //添加新的树节点并获取其索引号
                    AddNode(xNode, inTreeNode.Nodes[index]);
                }
            }
            else
            {
                //OuterXml:the markup containing the current node and all its child(but there is not child).
                //Trim method remove the leading and trailing space of a string.
                inTreeNode.Text = (inXmlNode.OuterXml).Trim();
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void FragmentSettingForm_Load(object sender, EventArgs e)
        {

        }
       
    }
}
