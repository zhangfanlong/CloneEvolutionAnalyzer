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
using System.Drawing.Drawing2D;
//using System.Collections;
//using CMDiffClassLibrary;   //直接将Diff类（及 LevenshteinDistance类）加入工程，不使用DLL方式

namespace CloneEvolutionAnalyzer
{
    public partial class MainForm : Form
    {
        //全局变量（在C#中不应该叫全局变量，那应该叫什么？）
        #region 全局变量
        internal string subSysDirectory = null;//当前加载的目标系统源代码的绝对路径
        internal string _folderPath = null;//当前加载的文件夹的绝对路径
        //public Point ptr;//鼠标的当前位置，已改用GlobalOperation中的mousePtr表示
        internal MapSettingForm mapSettingForm;
        internal FragmentSettingForm fragmentSettingForm;
        internal CCSetForm ccSetForm;
        internal MetricSettingForm metricSettingForm;
        internal BuildGenealogySettingForm buildGenealogySettingForm;
        internal List<TreeNode> treeView1_LeafNodes = new List<TreeNode>();    //采用泛型集合类，存储treeView1的所有叶子节点
        #endregion

        public MainForm()
        {
            InitializeComponent();
            AddGroupBox();
        }

        /*截取鼠标右键消息，控制快捷菜单的显示（暂未实现），考虑继承treeView类，重写它的WndProc方法
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Global.WindowsMessage.WM_RBUTTONDOWN || (m.Msg ==(int)Global.WindowsMessage.WM_MOUSEMOVE))
            {

            }
            base.WndProc(ref m);
        }*/

        //定义MyContextMenuItem类，继承自ToolStripMenuItem
        public class MyContextMenuItem : ToolStripMenuItem
        {
            //定义委托，处理上下文菜单事件
            public delegate void ContextMenuEventHandler(TreeView treeView, object sender, EventArgs e);
            public new event ContextMenuEventHandler Click; //重新定义Click事件，覆盖父类成员
            //定义事件触发方法
            protected override void OnClick(EventArgs e)
            {
                if (Click != null)
                {
                    try
                    {
                        TreeView treeView = (TreeView)(Global.mainForm.tabControl1.SelectedTab.Controls[0]);
                        Click(treeView, this, e);
                    }
                    catch (Exception ee)
                    {
                        Click(null, this, e);
                    }
                }
            }
        }


        /// <summary>
        /// 菜单栏File->Import...->Folder项响应方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog2.ShowDialog();
            //获取打开文件夹的路径
            subSysDirectory = folderBrowserDialog2.SelectedPath;
            DirectoryInfo dir = null;
            try
            {
                dir = new DirectoryInfo(subSysDirectory);
            }
            catch (Exception ee)
            {
                MessageBox.Show("Get directory path failed! " + ee.Message);
                return;
            }
            MessageBox.Show("Set SourceCode Success!");
        }

        /// <summary>
        /// 菜单栏File->Import...->Folder项响应方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            //获取打开文件夹的路径
            _folderPath = folderBrowserDialog1.SelectedPath;
            //为treeview控件添加节点
            this.treeView1.Nodes.Clear();
            TreeNode root = new TreeNode();
            root.Text = _folderPath + @" (press the right key to refresh)";
            this.treeView1.Nodes.Add(root);
            GetSubDirectoryNodes(root, _folderPath, true);
            this.treeView1.ExpandAll();
        }

        /// <summary>
        /// 遍历文件夹并将子文件夹和子文件显示到treeview中
        /// </summary>
        /// <param name="parentNode">当前的要扩展的树节点</param>
        /// <param name="path">当前文件夹的路径</param>
        /// <param name="getSubFiles">此参数为true时，加载子文件；反之，不加载子文件（仅加载子文件夹）</param>
        private void GetSubDirectoryNodes(TreeNode parentNode, string path, bool getSubFiles)
        {
            DirectoryInfo dir = null;
            try
            {
                dir = new DirectoryInfo(path);
            }
            catch (Exception ee)
            {
                MessageBox.Show("Get directory path failed! " + ee.Message);
                return;
            }

            //添加子文件夹节点
            DirectoryInfo[] subdir = null;
            try
            {
                subdir = dir.GetDirectories();
            }
            catch (Exception ee)
            {
                MessageBox.Show("Get SubDirectories failed! " + ee.Message);
            }
            if (subdir != null)
            {
                foreach (DirectoryInfo info in subdir)
                {
                    if ((info.Attributes & FileAttributes.Hidden) != 0)  //不显示隐藏文件夹
                    {
                        continue;
                    }
                    TreeNode childNode = new TreeNode(info.Name);
                    childNode.Tag = info.FullName;//Tag属性存放文件夹的路径信息
                    parentNode.Nodes.Add(childNode);
                    GetSubDirectoryNodes(childNode, info.FullName, true); //递归调用
                }
            }

            //添加子文件节点
            if (getSubFiles)
            {
                FileInfo[] subFile = null;
                try
                {
                    subFile = dir.GetFiles();
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Get SubFiles failed! " + ee.Message);
                }
                if (subFile != null)
                {
                    foreach (FileInfo info in subFile)
                    {
                        if ((info.Attributes & FileAttributes.Hidden) != 0)  //不显示隐藏文件
                        {
                            continue;
                        }
                        TreeNode childNode = new TreeNode(info.Name);
                        childNode.Tag = info.FullName;//Tag属性存放文件的路径信息
                        parentNode.Nodes.Add(childNode);
                    }
                }
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = this.treeView1.GetNodeAt(Global.mousePtr);//根据鼠标位置获取当前活动节点
            if (selectedNode.Parent != null && selectedNode.Nodes.Count == 0)//判断是否为叶子节点
            {
                if (Global.mousePtr.X < selectedNode.Bounds.Left || Global.mousePtr.X > selectedNode.Bounds.Right)//判断鼠标是否双击在节点文字区域之外
                {
                    return;
                }
                else
                {
                    string filePath = this.treeView1.SelectedNode.Tag.ToString();//获取文件路径

                    #region 在tabPage中显示
                    try
                    {
                        //Load XML
                        XmlDocument xdoc = new XmlDocument();
                        xdoc.Load(filePath);

                        //若当前tabControl中tabPage未使用，则在其中显示，否则创建新的tabPage显示XML
                        if (tabControl1.TabPages.Count != 0 && tabControl1.TabPages[0].Text == "ShowXMLTree")
                        {
                            tabControl1.TabPages[0].Text = selectedNode.Text; //标签名显示文件名
                        }
                        else
                        {
                            tabControl1.TabPages.Add(selectedNode.Text);    //创建新的tabPage
                        }
                        tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];

                        //创建TreeView控件
                        TreeView newTreeView = new TreeView();
                        tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
                        newTreeView.Dock = DockStyle.Fill;
                        ShowXMLTree(xdoc, ref newTreeView);
                        newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.newTreeView_NodeMouseClick);
                        //添加预定义的上下文菜单
                        newTreeView.ContextMenuStrip = this.contextMenuStrip1;
                        newTreeView.ExpandAll();
                    }
                    catch (XmlException xe)
                    {
                        MessageBox.Show("Load XML file failed! " + xe.Message);
                        return;
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }
                    #endregion
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// 将指定的xml文件显示在指定的treeview控件中
        /// </summary>
        /// <param name="xDoc"></param>
        /// <param name="treeViewControl"></param>
        public void ShowXMLTree(XmlDocument xDoc, ref TreeView treeViewControl)
        {
            treeViewControl.Nodes.Clear();
            TreeNode treeRoot = new TreeNode(xDoc.DocumentElement.Name);
            treeViewControl.Nodes.Add(treeRoot);
            //由根节点的孩子节点名称，判断要显示的文件内容
            foreach (XmlElement xmlNode in xDoc.DocumentElement.ChildNodes)
            {
                TreeNode treeNode = new TreeNode(xmlNode.Name);
                treeNode.Name = xmlNode.Name;   //树节点名字与xmlNode名字相同
                if (xmlNode.Name == "class")    //显示的是-classes.xml或-withCRD.xml文件
                {
                    TreeNode attr1 = new TreeNode("classid = " + xmlNode.GetAttribute("classid"));
                    treeNode.Nodes.Add(attr1);
                }
                if (xmlNode.Name == "CGMap")    //显示的是-MapOnFunctions/Blocks.xml文件
                {
                    TreeNode attr1 = new TreeNode("cgMapid = " + xmlNode.GetAttribute("cgMapid"));
                    treeNode.Nodes.Add(attr1);
                    TreeNode attr2 = new TreeNode("srcCGid = " + xmlNode.GetAttribute("srcCGid"));
                    treeNode.Nodes.Add(attr2);
                    TreeNode attr3 = new TreeNode("destCGid = " + xmlNode.GetAttribute("destCGid"));
                    treeNode.Nodes.Add(attr3);
                }
                AddNode(xmlNode, treeNode);
                treeNode.Expand();
                treeViewControl.Nodes[0].Nodes.Add(treeNode);
                treeViewControl.Nodes[0].Expand();
                //treeViewControl.Refresh();
            }
        }

        /// <summary>
        /// 添加xml节点的子节点到tree中
        /// </summary>
        /// <param name="inXmlNode">当前的xml节点</param>
        /// <param name="inTreeNode">当前的tree节点</param>
        private void AddNode(XmlNode inXmlNode, TreeNode inTreeNode)
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
                    if (xNode.Name == "CFMap")
                    {
                        tNode.Tag = xNode;  //将节点对应的xml节点保存在Tag中
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

        //treeview1的MouseDown事件响应函数，获取鼠标当前位置
        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            Global.mousePtr = new Point(e.X, e.Y);
        }

        private void toolStripGenerateCRD_Click(object sender, EventArgs e)
        {
            CloneRegionDescriptor crd = new CloneRegionDescriptor();
            //获取鼠标指针所在的树节点
            treeView1.SelectedNode = Global.mainForm.treeView1.GetNodeAt(Global.mousePtr);
            //判断所选节点是否为叶子节点
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Parent != null && treeView1.SelectedNode.Nodes.Count == 0)
            {
                //判断所在文件夹
                if (treeView1.SelectedNode.Parent.Text == "blocks" || treeView1.SelectedNode.Parent.Text == "functions")
                {
                    string path = treeView1.SelectedNode.Tag.ToString();
                    XmlDocument xmlFile = new XmlDocument();
                    xmlFile.Load(path);
                    crd.GenerateForSys(xmlFile, path.Substring(path.LastIndexOf("\\") + 1));   //第二个参数传入文件名
                    Global.CrdGenerationState = 1;  //置生成crd标志位1（生成单个crd）

                    #region 在tabPage中显示
                    try
                    {
                        //Load XML
                        XmlDocument xdoc = new XmlDocument();
                        xdoc.Load(crd.Path);
                        string crdFileName = path.Substring(path.LastIndexOf("\\") + 1).Replace(".xml", "-withCRD.xml");
                        //若当前tabControl中tabPage未使用，则在其中显示，否则创建新的tabPage显示XML
                        if (tabControl1.TabPages.Count != 0 && tabControl1.TabPages[0].Text == "ShowXMLTree")
                        {
                            tabControl1.TabPages[0].Text = crdFileName; //标签名显示文件名
                        }
                        else
                        {
                            tabControl1.TabPages.Add(crdFileName);    //创建新的tabPage
                        }
                        tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];

                        //创建TreeView控件
                        TreeView newTreeView = new TreeView();
                        tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
                        newTreeView.Dock = DockStyle.Fill;
                        ShowXMLTree(xdoc, ref newTreeView);
                        newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.newTreeView_NodeMouseClick);
                        //添加预定义的上下文菜单
                        newTreeView.ContextMenuStrip = this.contextMenuStrip1;
                    }
                    catch (XmlException xe)
                    {
                        MessageBox.Show("Load XML file failed! " + xe.Message);
                        return;
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message);
                        return;
                    }
                    #endregion

                    RefreshTreeView1(); //因为生成了新的文件，因此刷新treeview1 
                }
                else
                { MessageBox.Show("Please select a node under \"blocks\" or \"funtions\"!"); }
            }
        }

        /// <summary>
        /// GenerateForAll按钮的响应函数。为当前文件夹（包括子文件夹）下所有*-classes.xml文件生成CRD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripGenerateForAll_Click(object sender, EventArgs e)
        {
            CloneRegionDescriptor crd = new CloneRegionDescriptor();
            GetAllLeafNodes(treeView1.Nodes[0]);    //获取treeView1所有叶子节点，保存在this.treeView1_LeafNodes中

            foreach (TreeNode node in this.treeView1_LeafNodes)
            {
                if (node.Parent.Text == "blocks" || node.Parent.Text == "functions")    //判断xml所在文件夹
                {
                    string path = node.Tag.ToString();
                    XmlDocument xmlFile = new XmlDocument();
                    if (path.IndexOf(@"-classes.xml") != -1 && path.IndexOf("-withCRD.xml") == -1)
                    {
                        xmlFile.Load(path);
                        crd.GenerateForSys(xmlFile, path.Substring(path.LastIndexOf(@"\") + 1));
                    }
                }
            }
            //已删除ShowCRD选项卡，改在ShowXMLTree中显示
            //tabControl1.SelectedTab = tabControl1.TabPages[2];
            //treeView3.Nodes[0].Text = "Click on a file in the filetree to show its CRD.";
            Global.CrdGenerationState = 2;  //置生成crd标志位2（生成所有文件crd）
            RefreshTreeView1();
        }

        /// <summary>
        /// 根据treeView1中CRD文件的路径获取crd文件夹路径
        /// </summary>
        internal void GetCRDDirFromTreeView1()
        {
            if (Global.CrdGenerationState == 0 || CloneRegionDescriptor.CrdDir == null) //只有在没有生成CRD步操作时才需要此操作
            {
                GetAllLeafNodes(this.treeView1.Nodes[0]);  //获取treeView1所有叶子节点
                foreach (TreeNode node in this.treeView1_LeafNodes)
                {
                    if (node.Text.IndexOf("-withCRD.xml") != -1)    //寻找"_withCRD.xml"节点
                    {
                        //路径中的层次分割符用"\"或'/'
                        if (node.Tag.ToString().IndexOf(@"\CRDFiles\") != -1)
                        {
                            int index = node.Tag.ToString().IndexOf(@"CRDFiles");
                            //从节点的Tag属性中截取CRDFiles文件夹路径（Tag属性中存放的是该文件的完整的路径）
                            CloneRegionDescriptor.CrdDir = node.Tag.ToString().Substring(0, index + 8);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据treeView1中MAP文件的路径获取map文件夹路径
        /// </summary>
        internal void GetMAPDirFromTreeView1()
        {
            if (Global.MapState == 0 || AdjacentVersionMapping.MapFileDir == null)   //只有在没有经历映射步操作时才需要此操作
            {
                GetAllLeafNodes(this.treeView1.Nodes[0]);  //获取treeView1所有叶子节点
                foreach (TreeNode node in this.treeView1_LeafNodes)
                {
                    if (node.Text.IndexOf("-MapOnBlocks.xml") != -1 || node.Text.IndexOf("-MapOnFunctions.xml") != -1)
                    {
                        //路径中的层次分割符用"\"或'/'
                        if (node.Tag.ToString().IndexOf(@"\MAPFiles\") != -1)
                        {
                            int index = node.Tag.ToString().IndexOf(@"MAPFiles");
                            //从节点的Tag属性中截取MAPFiles文件夹路径（Tag属性中存放的是该文件的完整的路径）
                            AdjacentVersionMapping.MapFileDir = node.Tag.ToString().Substring(0, index + 8);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 选取以当前节点为根的子树的所有叶子节点（递归算法）
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public void GetAllLeafNodes(TreeNode node)
        {
            if (node.Parent != null && node.Nodes.Count == 0)  //如果当前节点是叶子节点
            {
                this.treeView1_LeafNodes.Add(node);
                return;
            }
            else
            {
                foreach (TreeNode childNode in node.Nodes)
                {
                    GetAllLeafNodes(childNode);
                }
            }
        }

        /// <summary>
        /// 刷新treeView1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_Refresh_Click(object sender, EventArgs e)
        {
            RefreshTreeView1();
        }

        /// <summary>
        /// 刷新treeview1
        /// </summary>
        internal void RefreshTreeView1()
        {
            //为treeview控件添加节点
            this.treeView1.Nodes.Clear();
            TreeNode root = new TreeNode();
            root.Text = _folderPath + @" (press the right key to refresh)";
            this.treeView1.Nodes.Add(root);
            GetSubDirectoryNodes(root, _folderPath, true);
            this.treeView1.ExpandAll();
        }

        /// <summary>
        /// 单击treeview控件的叶子节点，显示该文件对应的CRDtree
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void treeView1_Click(object sender, EventArgs e)
        //{
        //    this.treeView1.SelectedNode = treeView1.GetNodeAt(Global.mousePtr);
        //    if (this.treeView1.SelectedNode.Parent != null&&this.treeView1.SelectedNode.Nodes.Count == 0)   //如果是叶子节点
        //    {
        //        if (Global.CrdGenerationState == 2)
        //        {
        //            string name = this.treeView1.SelectedNode.Text.Replace(@".xml", @"-withCRD.xml");
        //            string path = CloneRegionDescriptor.CrdDir + "\\" + name;

        //            try
        //            {
        //                //Load XML
        //                XmlDocument xdoc = new XmlDocument();
        //                xdoc.Load(path);
        //                ShowXMLTree(xdoc, ref treeView3);
        //                tabControl1.SelectedTab = tabControl1.TabPages[2];
        //            }
        //            catch (XmlException xe)
        //            {
        //                MessageBox.Show("Load XML file failed! " + xe.Message);
        //                return;
        //            }
        //            catch (Exception ee)
        //            {
        //                MessageBox.Show(ee.Message);
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            treeView3.Nodes[0].Text = "The CRD file for this xml doesn't exist. Press\"GenerateCRD\" or\"GenerateForAll\". ";
        //        }
        //    }
        //}

        //工具栏Mapping按钮的响应函数

        private void toolStripButton_MapBetweenVersions_Click(object sender, EventArgs e)
        {
            this.mapSettingForm = new MapSettingForm();
            this.mapSettingForm.InitShowStatus1();
            this.mapSettingForm.Show();
            Global.mainForm.treeView1.CheckBoxes = true;
            Global.treeView1NodeCheckedNum = 0;
            //定义AdjacentVersionMapping对象，用于相邻版本间映射的相关操作
            AdjacentVersionMapping adjVerMap = new AdjacentVersionMapping();
            //向MapSettingFinished事件添加委托，使adVerMap的OnStartMapping方法响应此事件
            this.mapSettingForm.MapSettingFinished += new CGMapEventHandler(adjVerMap.OnStartMapping);

        }

        private void toolStripButton_MapAll_Click(object sender, EventArgs e)
        {
            this.mapSettingForm = new MapSettingForm();
            this.mapSettingForm.InitShowStatus2();
            this.mapSettingForm.Show();
        }

        //在treeview1上进行check操作的响应函数
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Checked)
            {
                if (Global.treeView1NodeCheckedNum == 0)
                {
                    //判断选择的是否是CRD文件（此处也可以使用正则表达式）
                    int index = e.Node.Text.IndexOf("-withCRD.xml");
                    if (index != -1)
                    {
                        Global.treeView1NodeCheckedNum++;
                        this.mapSettingForm.textBox1.Text = e.Node.Text;
                        this.mapSettingForm.label_Warning1.Text = "";
                    }
                    else
                    {   //选择无效的节点
                        this.mapSettingForm.label_Warning1.ForeColor = Color.Red;
                        this.mapSettingForm.label_Warning1.Text = "Invalid operation! Please check on '*_withCRD.xml' files!";
                        e.Node.Checked = false; //注：修改Checked属性会引发AfterCheck事件！！
                    }
                }
                else if (Global.treeView1NodeCheckedNum == 1)
                {
                    int index = e.Node.Text.IndexOf("-withCRD.xml");
                    if (index != -1)
                    {
                        Global.treeView1NodeCheckedNum++;
                        this.mapSettingForm.textBox2.Text = e.Node.Text;
                        this.mapSettingForm.label_Warning1.Text = "";
                    }
                    else
                    {   //选择无效的节点
                        this.mapSettingForm.label_Warning1.ForeColor = Color.Red;
                        this.mapSettingForm.label_Warning1.Text = "Invalid operation! Please check on '*_withCRD.xml' files!";
                        e.Node.Checked = false; //注：修改Checked属性会引发AfterCheck事件！！
                    }
                }
                else
                {
                    this.mapSettingForm.label_Warning1.ForeColor = Color.Red;
                    this.mapSettingForm.label_Warning1.Text =
                    "You can't check more than 2 versions!\nIf you want to change, press 'Click' button and recheck.";
                    e.Node.Checked = false;
                    //Global.treeView1NodeCheckedNum = 2;
                }
            }
            //由于其他地方修改节点的Checked属性对引发AfterCheck事件，因此此处要判断此事件是否由鼠标引发
            else if (e.Action == TreeViewAction.ByMouse)
            {
                //判断uncheck操作是否发生在crd文件节点
                int index = e.Node.Text.IndexOf("-withCRD.xml");
                if (index != -1)
                { Global.treeView1NodeCheckedNum--; }
            }
        }

        public void newTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ((TreeView)sender).SelectedNode = e.Node;
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Name == "source")
                {                    
                    contextMenuStrip1.Items[0].Enabled = true;    //ViewCloneFragment项
                    contextMenuStrip1.Items[1].Enabled = true;    //ViewFullCode项
                    contextMenuStrip1.Items[2].Enabled = false;   //ViewSourceDiff项
                }
                else if (e.Node.Name == "CFMap")
                {
                    contextMenuStrip1.Items[0].Enabled = false;
                    contextMenuStrip1.Items[1].Enabled = false;
                    contextMenuStrip1.Items[2].Enabled = true;
                }
                else
                {
                    contextMenuStrip1.Items[0].Enabled = false;
                    contextMenuStrip1.Items[1].Enabled = false;
                    contextMenuStrip1.Items[2].Enabled = false;
                }
            }
        }

        public void newTreeView_NodeMouseClick1(object sender, TreeNodeMouseClickEventArgs e)
        {
            ((TreeView)sender).SelectedNode = e.Node;
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Name == "source")
                {
                    contextMenuStrip3.Items[0].Enabled = true;    //ViewCloneFragment项
                    contextMenuStrip3.Items[1].Enabled = true;    //ViewFullCode项
                    contextMenuStrip3.Items[2].Enabled = true;   //ViewSourceDiff项
                }               
                else
                {
                    contextMenuStrip1.Items[0].Enabled = false;
                    contextMenuStrip1.Items[1].Enabled = false;
                    contextMenuStrip1.Items[2].Enabled = false;
                }
            }
        }
        private void viewdiff_Click(TreeView treeView, object sender, EventArgs e)
        {
            treeView = (TreeView)(Global.mainForm.tabControl1.SelectedTab.Controls[0]);
            CloneSourceInfo destCFInfo = (CloneSourceInfo)treeView.SelectedNode.Tag;
            int index = treeView.SelectedNode.Text.IndexOf("=");
            string file = treeView.SelectedNode.Text.Substring(index);
            string filename = file.Substring(2, (file.IndexOf(" ") - 3));

            destCFInfo.sourcePath = subSysDirectory + "\\" + filename;
            List<string> srcCF = new List<string>();
            List<string> destCF = new List<string>();
            CloneSourceInfo srcCFInfo = new CloneSourceInfo();

            if (FragmentSettingForm.flag == 1)
            {
                srcCF = FragmentSettingForm.SrcFragment;
                srcCFInfo.sourcePath = FragmentSettingForm.SrcPath;
                srcCFInfo.startLine = 1;
                srcCFInfo.endLine = FragmentSettingForm.SrcFragment.Count;
            }
            else if (CCSetForm.flag == 1)
            {
                srcCF = CCSetForm.SrcFragmentCC;
                srcCFInfo.sourcePath = CCSetForm.SrcPathCC;
                srcCFInfo.startLine = 1;
                srcCFInfo.endLine = CCSetForm.SrcFragmentCC.Count;
            }

            string[] data = File.ReadAllLines(destCFInfo.sourcePath);
            for (int i = (destCFInfo.startLine - 1); i < destCFInfo.endLine; i++)
                destCF.Add(data[i]);
            if (srcCF != null && destCF != null)
            {
                //计算Diff
                Diff.UseDefaultStrSimTh();  //注：此语句不能少。否则Diff.StrSimThreshold值为0
                Diff.DiffInfo diffInfo = Diff.DiffFiles(srcCF, destCF);
                //显示Diff
                ShowDiffForm showDiffForm = new ShowDiffForm();
                showDiffForm.ShowCloneFragmentDiff(srcCFInfo, destCFInfo, diffInfo);
            }
        }
        private void viewCloneFragmentContextMenuItem_Click(TreeView treeView, object sender, EventArgs e)
        {
            string test = treeView.SelectedNode.Text;
            CloneSourceInfo sourceInfo = (CloneSourceInfo)treeView.SelectedNode.Tag;

            ShowCodeForm showCodeForm = new ShowCodeForm(); //如何设定窗口间的父子关系？？
            showCodeForm.SetCloneSourceInfo(sourceInfo);
            showCodeForm.IsShowFullCode = false;
            showCodeForm.Show();

        }

        private void viewFullCodeContextMenuItem_Click(TreeView treeView, object sender, EventArgs e)
        {
            CloneSourceInfo sourceInfo = (CloneSourceInfo)treeView.SelectedNode.Tag;
            ShowCodeForm showCodeForm = new ShowCodeForm();
            showCodeForm.SetCloneSourceInfo(sourceInfo);
            showCodeForm.IsShowFullCode = true;
            showCodeForm.Show();
        }

        
        private void viewSourceDiffContextMenuItem_Click(TreeView treeView, object sender, EventArgs e)
        {
            string text = treeView.SelectedNode.Text;
            XmlElement node = (XmlElement)treeView.SelectedNode.Tag;
            List<string> srcCF = new List<string>();
            List<string> destCF = new List<string>();
            CloneSourceInfo srcCFInfo = new CloneSourceInfo();
            CloneSourceInfo destCFInfo = new CloneSourceInfo();
            srcCF = GetCFSourceFromCFMapNode(node, true, out srcCFInfo);
            destCF = GetCFSourceFromCFMapNode(node, false, out destCFInfo);
            if (srcCF != null && destCF != null)
            {
                //计算Diff
                Diff.UseDefaultStrSimTh();  //注：此语句不能少。否则Diff.StrSimThreshold值为0
                Diff.DiffInfo diffInfo = Diff.DiffFiles(srcCF, destCF);
                //显示Diff
                ShowDiffForm showDiffForm = new ShowDiffForm();
                showDiffForm.ShowCloneFragmentDiff(srcCFInfo, destCFInfo, diffInfo);
            }
        }

        /// <summary>
        /// 根据CFMap节点中包含的CF,CG及Maps节点包含的文件名信息，读取该CF源代码
        /// </summary>
        /// <param name="cfMapNode"></param>
        /// <param name="isSrc">指定要获取srcCF还是destCF的代码</param>
        /// <param name="sourceInfo">输出参数，保存构造好的CloneSourceInfo对象，供调用方使用</param>
        /// <returns>获取的克隆片段源代码</returns>
        internal List<string> GetCFSourceFromCFMapNode(XmlElement cfMapNode, bool isSrc, out CloneSourceInfo sourceInfo)
        {
            List<string> cloneFragment = new List<string>();
            sourceInfo = new CloneSourceInfo();
            string srcFileName, destFileName;    //文件名（不含路径）
            string fullName;    //文件名（包含绝对路径）
            string subDirName;
            string cgID;
            int cfID;
            XmlElement cgMapNode = (XmlElement)cfMapNode.ParentNode;
            XmlElement rootNode = (XmlElement)cgMapNode.ParentNode;
            Global.mainForm.GetCRDDirFromTreeView1();   //获得CloneRegionDescriptor.CrdDir
            srcFileName = ((XmlElement)rootNode.ChildNodes[0]).InnerText;
            destFileName = ((XmlElement)rootNode.ChildNodes[1]).InnerText;
            //确定子文件夹
            if (srcFileName.IndexOf("_blocks-") != -1)
            { subDirName = "blocks"; }
            else
            { subDirName = "functions"; }

            if (isSrc)   //获得源CF代码
            {
                //从srcFileName元素中获得文件名
                fullName = CloneRegionDescriptor.CrdDir + "\\" + subDirName + "\\" + srcFileName;
                cgID = cgMapNode.GetAttribute("srcCGid");
                Int32.TryParse(cfMapNode.GetAttribute("srcCFid"), out cfID);

            }
            else//获得目标CF代码
            {
                //从destFileName元素中获得文件名
                fullName = CloneRegionDescriptor.CrdDir + "\\" + subDirName + "\\" + destFileName;
                cgID = cgMapNode.GetAttribute("destCGid");
                Int32.TryParse(cfMapNode.GetAttribute("destCFid"), out cfID);
            }
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(fullName);
            }
            catch (Exception ee)
            {
                MessageBox.Show("Get withCRD.xml files failed! " + ee.Message);
            }
            if (xmlDoc.DocumentElement != null)
            {
                foreach (XmlElement cgNode in xmlDoc.DocumentElement.ChildNodes)
                {
                    //找到与cgID对应的class节点
                    if (cgNode.Name == "class" && cgNode.GetAttribute("classid") == cgID)
                    {
                        XmlElement sourceNode = (XmlElement)cgNode.ChildNodes[cfID - 1];    //获得与cfID对应的source节点
                        sourceInfo.sourcePath = sourceNode.GetAttribute("file");
                        Int32.TryParse(sourceNode.GetAttribute("startline"), out sourceInfo.startLine);
                        Int32.TryParse(sourceNode.GetAttribute("endline"), out sourceInfo.endLine);
                        //获取源代码片段
                        List<string> fileContent = Global.GetFileContent(subSysDirectory + "\\" + sourceInfo.sourcePath);
                        cloneFragment = CloneRegionDescriptor.GetCFSourceFromSourcInfo(fileContent, sourceInfo);
                        break;
                    }
                }
            }
            else
            {
                cloneFragment = null;
            }
            return cloneFragment;
        }

        private void toolStripButton_BuildGenealogy_Click(object sender, EventArgs e)
        {
            this.buildGenealogySettingForm = new BuildGenealogySettingForm();
            this.buildGenealogySettingForm.Show();
        }

        private void tabControl1_DoubleClick(object sender, EventArgs e)
        {
            tabControl1.TabPages.Remove(this.tabControl1.SelectedTab);
        }


        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void toolStripExtractCodeMetrics_Click_1(object sender, EventArgs e)
        {
            
            ExtractMetric EM = new ExtractMetric();
            EM.GetExtractProcess();
            ShowExtractResult ExtShow = new ShowExtractResult();
            ExtShow.ShowFile();
            ExtShow.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
           
            ExtractMetric EM = new ExtractMetric();
            EM.GetTrainProcess();
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            ExtractMetric EM = new ExtractMetric();
            EM.GetResultProcess();
            ShowEvaluationResult EvaShow = new ShowEvaluationResult();
            EvaShow.ShowFile();
            EvaShow.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {            
            //获取鼠标指针所在的树节点
            treeView1.SelectedNode = Global.mainForm.treeView1.GetNodeAt(Global.mousePtr);
            //判断所选节点是否为叶子节点
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Parent != null && treeView1.SelectedNode.Nodes.Count == 0)
            {
                string path = treeView1.SelectedNode.Tag.ToString();
                //加载克隆家系XmlDocument对象
                XmlDocument genealogyXml = new XmlDocument();
                genealogyXml.Load(path);
                //检查选择的是否是genealogy文件
                if (genealogyXml.DocumentElement.Name != "CloneGenealogy")
                { MessageBox.Show("This is not a genealogy file!"); }
                else
                {
                   // tabControl1.Dock = DockStyle.Top;
                   // tabControl1.Size = new Size(636, 546);
                    tabControl1.TabPages.Add(path.Substring(path.LastIndexOf("\\") + 1));//创建新的tabPage，显示文件名
                    tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];                  
                     //创建CloneGenealogyViz对象
                    CloneGenealogyViz cgv = new CloneGenealogyViz();
                    cgv.MouseDown += new MouseEventHandler(this.cgv_MouseDown);
                   
                    cgv.MouseWheel += new MouseEventHandler(this.cgv_MouseWheel);
                    cgv.Init();
                    cgv.SetGenealogyXml(genealogyXml);
                   
                    tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(cgv);
                    
                    ShowInfo(genealogyXml);
                }
            }
        }

        private void ClearText(Control ctrlTop)
        {
            if (ctrlTop.GetType() == typeof(TextBox))
                ctrlTop.Text = "";
            else
            {
                foreach (Control ctrl in ctrlTop.Controls)
                {
                    ClearText(ctrl); //循环调用
                }
            }
        }

        /// <summary>
        /// 在parentPanel上添加自定义控件
        /// </summary>
        /// <param name="parentPanel"></param>
        private void AddGroupBox()
        {
            //添加InfoZone
            this.groupBox_InfoZone = new GroupBox();
            groupBox_InfoZone.Dock = DockStyle.Bottom;
            groupBox_InfoZone.Name = "GroupBox_InfoZone";
            groupBox_InfoZone.TabIndex = 0;
            groupBox_InfoZone.TabStop = false;
            groupBox_InfoZone.Text = "InfoZone";
            groupBox_InfoZone.Size = new Size(636, 240);
            groupBox_InfoZone.BackColor = Color.LightYellow;
            groupBox_InfoZone.SuspendLayout();
            splitContainer1.Panel2.Controls.Add(groupBox_InfoZone);

            #region 添加第一组控件（GenealogyInfo）
            //添加groupBox_GenealogyInfo
            this.groupBox_GenealogyInfo = new GroupBox();
            groupBox_GenealogyInfo.Location = new Point(15, 20);
            groupBox_GenealogyInfo.Name = "groupBox_GenealogyInfo";
            groupBox_GenealogyInfo.TabIndex = 0;
            groupBox_GenealogyInfo.TabStop = false;
            groupBox_GenealogyInfo.Text = "GenealogyInfo";
            groupBox_GenealogyInfo.Size = new Size(970, 60);
            groupBox_GenealogyInfo.BackColor = Color.LightGray;
            groupBox_InfoZone.Controls.Add(groupBox_GenealogyInfo);
            //添加label_GenealogyID
            label_GenealogyID = new Label();
            label_GenealogyID.AutoSize = true;
            label_GenealogyID.Location = new System.Drawing.Point(25, 30);
            label_GenealogyID.Name = "label_GenealogyID";
            label_GenealogyID.Size = new System.Drawing.Size(70, 12);
            label_GenealogyID.TabIndex = 0;
            label_GenealogyID.Text = "GenealogyID";
            groupBox_GenealogyInfo.Controls.Add(label_GenealogyID);
            //添加textBox_GenealogyID
            textBox_GenealogyID = new TextBox();
            textBox_GenealogyID.Location = new System.Drawing.Point(110, 27);
            textBox_GenealogyID.Name = "textBox_GenealogyID";
            textBox_GenealogyID.ReadOnly = true;
            textBox_GenealogyID.Size = new System.Drawing.Size(65, 21);
            textBox_GenealogyID.TabIndex = 1;
            groupBox_GenealogyInfo.Controls.Add(textBox_GenealogyID);
            //添加label_StartVersion
            label_StartVersion = new Label();
            label_StartVersion.AutoSize = true;
            label_StartVersion.Location = new System.Drawing.Point(190, 30);
            label_StartVersion.Name = "label_StartVersion";
            label_StartVersion.Size = new System.Drawing.Size(75, 12);
            label_StartVersion.TabIndex = 0;
            label_StartVersion.Text = "StartVersion";
            groupBox_GenealogyInfo.Controls.Add(label_StartVersion);
            //添加textBox_StartVersion
            textBox_StartVersion = new TextBox();
            textBox_StartVersion.Location = new System.Drawing.Point(275, 27);
            textBox_StartVersion.Name = "textBox_StartVersion";
            textBox_StartVersion.ReadOnly = true;
            textBox_StartVersion.Size = new System.Drawing.Size(120, 21);
            textBox_StartVersion.TabIndex = 1;
            groupBox_GenealogyInfo.Controls.Add(textBox_StartVersion);
            //添加label_EndVersion
            label_EndVersion = new Label();
            label_EndVersion.AutoSize = true;
            label_EndVersion.Location = new System.Drawing.Point(415, 30);
            label_EndVersion.Name = "label_EndVersion";
            label_EndVersion.Size = new System.Drawing.Size(75, 12);
            label_EndVersion.TabIndex = 0;
            label_EndVersion.Text = "EndVersion";
            groupBox_GenealogyInfo.Controls.Add(label_EndVersion);
            //添加textBox_EndVersion
            textBox_EndVersion = new TextBox();
            textBox_EndVersion.Location = new System.Drawing.Point(495, 27);
            textBox_EndVersion.Name = "textBox_EndVersion";
            textBox_EndVersion.ReadOnly = true;
            textBox_EndVersion.Size = new System.Drawing.Size(120, 21);
            textBox_EndVersion.TabIndex = 1;
            groupBox_GenealogyInfo.Controls.Add(textBox_EndVersion);
            //添加label_Age
            label_Age = new Label();
            label_Age.AutoSize = true;
            label_Age.Location = new System.Drawing.Point(635, 30);
            label_Age.Name = "label_Age";
            label_Age.Size = new System.Drawing.Size(35, 12);
            label_Age.TabIndex = 0;
            label_Age.Text = "Age";
            groupBox_GenealogyInfo.Controls.Add(label_Age);
            //添加textBox_Age
            textBox_Age = new TextBox();
            textBox_Age.Location = new System.Drawing.Point(675, 27);
            textBox_Age.Name = "textBox_Age";
            textBox_Age.ReadOnly = true;
            textBox_Age.Size = new System.Drawing.Size(50, 21);
            textBox_Age.TabIndex = 1;
            groupBox_GenealogyInfo.Controls.Add(textBox_Age);
           
            #endregion

            #region 添加第二组控件（EvolutionPatternCount）
            //添加groupBox_EvolutionPatternCount
            groupBox_EvolutionPatternCount = new GroupBox();
            groupBox_EvolutionPatternCount.Location = new Point(15, 90);
            groupBox_EvolutionPatternCount.Name = "groupBox_EvolutionPatternCount";
            groupBox_EvolutionPatternCount.TabIndex = 0;
            groupBox_EvolutionPatternCount.TabStop = false;
            groupBox_EvolutionPatternCount.Text = "EvolutionPatternCount";
            groupBox_EvolutionPatternCount.Size = new Size(970, 80);
            groupBox_EvolutionPatternCount.BackColor = Color.LightGray;
            groupBox_InfoZone.Controls.Add(groupBox_EvolutionPatternCount);
            //添加label_STATIC
            label_STATIC = new Label();
            label_STATIC.AutoSize = true;
            label_STATIC.Location = new System.Drawing.Point(50, 20);
            label_STATIC.Name = "label_STATIC";
            label_STATIC.Size = new System.Drawing.Size(60, 12);
            label_STATIC.TabIndex = 0;
            label_STATIC.Text = "STATIC";
            groupBox_EvolutionPatternCount.Controls.Add(label_STATIC);
            //添加textBox_STATIC
            textBox_STATIC = new TextBox();
            textBox_STATIC.Location = new System.Drawing.Point(110, 17);
            textBox_STATIC.Name = "textBox_STATIC";
            textBox_STATIC.ReadOnly = true;
            textBox_STATIC.Size = new System.Drawing.Size(65, 21);
            textBox_STATIC.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_STATIC);
            //添加label_SAME
            label_SAME = new Label();
            label_SAME.AutoSize = true;
            label_SAME.Location = new System.Drawing.Point(210, 20);
            label_SAME.Name = "label_SAME";
            label_SAME.Size = new System.Drawing.Size(60, 12);
            label_SAME.TabIndex = 0;
            label_SAME.Text = "SAME";
            groupBox_EvolutionPatternCount.Controls.Add(label_SAME);
            //添加textBox_SAME
            textBox_SAME = new TextBox();
            textBox_SAME.Location = new System.Drawing.Point(260, 17);
            textBox_SAME.Name = "textBox_SAME";
            textBox_SAME.ReadOnly = true;
            textBox_SAME.Size = new System.Drawing.Size(65, 21);
            textBox_SAME.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_SAME);
            //添加label_ADD
            label_ADD = new Label();
            label_ADD.AutoSize = true;
            label_ADD.Location = new System.Drawing.Point(365, 20);
            label_ADD.Name = "label_ADD";
            label_ADD.Size = new System.Drawing.Size(60, 12);
            label_ADD.TabIndex = 0;
            label_ADD.Text = "ADD";
            groupBox_EvolutionPatternCount.Controls.Add(label_ADD);
            //添加textBox_ADD
            textBox_ADD = new TextBox();
            textBox_ADD.Location = new System.Drawing.Point(410, 17);
            textBox_ADD.Name = "textBox_ADD";
            textBox_ADD.ReadOnly = true;
            textBox_ADD.Size = new System.Drawing.Size(65, 21);
            textBox_ADD.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_ADD);
            //添加label_SUBSTRACT
            label_SUBSTRACT = new Label();
            label_SUBSTRACT.AutoSize = true;
            label_SUBSTRACT.Location = new System.Drawing.Point(520, 20);
            label_SUBSTRACT.Name = "label_SUBSTRACT";
            label_SUBSTRACT.Size = new System.Drawing.Size(75, 12);
            label_SUBSTRACT.TabIndex = 0;
            label_SUBSTRACT.Text = "SUBSTARCT";
            groupBox_EvolutionPatternCount.Controls.Add(label_SUBSTRACT);
            //添加textBox_SUBSTRACT
            textBox_SUBSTRACT = new TextBox();
            textBox_SUBSTRACT.Location = new System.Drawing.Point(600, 17);
            textBox_SUBSTRACT.Name = "textBox_SUBSTRACT";
            textBox_SUBSTRACT.ReadOnly = true;
            textBox_SUBSTRACT.Size = new System.Drawing.Size(65, 21);
            textBox_SUBSTRACT.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_SUBSTRACT);
            //添加label_CONSISTENTCHANGE
            label_CONSISTENTCHANGE = new Label();
            label_CONSISTENTCHANGE.AutoSize = true;
            label_CONSISTENTCHANGE.Location = new System.Drawing.Point(50, 50);
            label_CONSISTENTCHANGE.Name = "label_CONSISTENTCHANGE";
            label_CONSISTENTCHANGE.Size = new System.Drawing.Size(150, 12);
            label_CONSISTENTCHANGE.TabIndex = 0;
            label_CONSISTENTCHANGE.Text = "CONSISTENTCHANGE";
            groupBox_EvolutionPatternCount.Controls.Add(label_CONSISTENTCHANGE);
            //添加textBox_CONSISTENTCHANGE
            textBox_CONSISTENTCHANGE = new TextBox();
            textBox_CONSISTENTCHANGE.Location = new System.Drawing.Point(175, 47);
            textBox_CONSISTENTCHANGE.Name = "textBox_CONSISTENTCHANGE";
            textBox_CONSISTENTCHANGE.ReadOnly = true;
            textBox_CONSISTENTCHANGE.Size = new System.Drawing.Size(65, 21);
            textBox_CONSISTENTCHANGE.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_CONSISTENTCHANGE);
            //添加label_INCONSISTENTCHANGE
            label_INCONSISTENTCHANGE = new Label();
            label_INCONSISTENTCHANGE.AutoSize = true;
            label_INCONSISTENTCHANGE.Location = new System.Drawing.Point(280, 50);
            label_INCONSISTENTCHANGE.Name = "label_INCONSISTENTCHANGE";
            label_INCONSISTENTCHANGE.Size = new System.Drawing.Size(155, 12);
            label_INCONSISTENTCHANGE.TabIndex = 0;
            label_INCONSISTENTCHANGE.Text = "INCONSISTENTCHANGE";
            groupBox_EvolutionPatternCount.Controls.Add(label_INCONSISTENTCHANGE);
            //添加textBox_INCONSISTENTCHANGE
            textBox_INCONSISTENTCHANGE = new TextBox();
            textBox_INCONSISTENTCHANGE.Location = new System.Drawing.Point(420, 47);
            textBox_INCONSISTENTCHANGE.Name = "textBox_INCONSISTENTCHANGE";
            textBox_INCONSISTENTCHANGE.ReadOnly = true;
            textBox_INCONSISTENTCHANGE.Size = new System.Drawing.Size(65, 21);
            textBox_INCONSISTENTCHANGE.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_INCONSISTENTCHANGE);
            //添加label_SPLIT
            label_SPLIT = new Label();
            label_SPLIT.AutoSize = true;
            label_SPLIT.Location = new System.Drawing.Point(520, 50);
            label_SPLIT.Name = "label_SPLIT";
            label_SPLIT.Size = new System.Drawing.Size(50, 12);
            label_SPLIT.TabIndex = 0;
            label_SPLIT.Text = "SPLIT";
            groupBox_EvolutionPatternCount.Controls.Add(label_SPLIT);
            //添加textBox_SPLIT
            textBox_SPLIT = new TextBox();
            textBox_SPLIT.Location = new System.Drawing.Point(580, 47);
            textBox_SPLIT.Name = "textBox_SPLIT";
            textBox_SPLIT.ReadOnly = true;
            textBox_SPLIT.Size = new System.Drawing.Size(65, 21);
            textBox_SPLIT.TabIndex = 1;
            groupBox_EvolutionPatternCount.Controls.Add(textBox_SPLIT);
            #endregion

            #region 添加第三组控件（NodeInfo）
            //添加groupBox_NodeInfo
            groupBox_NodeInfo = new GroupBox();
            groupBox_NodeInfo.Location = new Point(15, 180);
            groupBox_NodeInfo.Name = "groupBox_NodeInfo";
            groupBox_NodeInfo.TabIndex = 0;
            groupBox_NodeInfo.TabStop = false;
            groupBox_NodeInfo.Text = "NodeInfo";
            groupBox_NodeInfo.Size = new Size(970, 50);
            groupBox_NodeInfo.BackColor = Color.LightGray;
            groupBox_InfoZone.Controls.Add(groupBox_NodeInfo);
            //添加label_Version
            label_Version = new Label();
            label_Version.AutoSize = true;
            label_Version.Location = new System.Drawing.Point(50, 20);
            label_Version.Name = "label_Version";
            label_Version.Size = new System.Drawing.Size(60, 12);
            label_Version.TabIndex = 0;
            label_Version.Text = "Version";
            groupBox_NodeInfo.Controls.Add(label_Version);
            //添加textBox_Version
            textBox_Version = new TextBox();
            textBox_Version.Location = new System.Drawing.Point(110, 17);
            textBox_Version.Name = "textBox_Version";
            textBox_Version.ReadOnly = true;
            textBox_Version.Size = new System.Drawing.Size(120, 21);
            textBox_Version.TabIndex = 1;
            groupBox_NodeInfo.Controls.Add(textBox_Version);
            //添加label_CGID
            label_CGID = new Label();
            label_CGID.AutoSize = true;
            label_CGID.Location = new System.Drawing.Point(280, 20);
            label_CGID.Name = "label_CGID";
            label_CGID.Size = new System.Drawing.Size(50, 12);
            label_CGID.TabIndex = 0;
            label_CGID.Text = "CGID";
            groupBox_NodeInfo.Controls.Add(label_CGID);
            //添加textBox_CGID
            textBox_CGID = new TextBox();
            textBox_CGID.Location = new System.Drawing.Point(330, 17);
            textBox_CGID.Name = "textBox_CGID";
            textBox_CGID.ReadOnly = true;
            textBox_CGID.Size = new System.Drawing.Size(60, 21);
            textBox_CGID.TabIndex = 1;
            groupBox_NodeInfo.Controls.Add(textBox_CGID);
            //添加label_CGSize
            label_CGSize = new Label();
            label_CGSize.AutoSize = true;
            label_CGSize.Location = new System.Drawing.Point(430, 20);
            label_CGSize.Name = "label_CGSize";
            label_CGSize.Size = new System.Drawing.Size(50, 12);
            label_CGSize.TabIndex = 0;
            label_CGSize.Text = "CGSize";
            groupBox_NodeInfo.Controls.Add(label_CGSize);
            //添加textBox_CGSize
            textBox_CGSize = new TextBox();
            textBox_CGSize.Location = new System.Drawing.Point(490, 17);
            textBox_CGSize.Name = "textBox_CGSize";
            textBox_CGSize.ReadOnly = true;
            textBox_CGSize.Size = new System.Drawing.Size(60, 21);
            textBox_CGSize.TabIndex = 1;
            groupBox_NodeInfo.Controls.Add(textBox_CGSize);
            //添加CGMAPID
            label_CGMAPID = new Label();
            label_CGMAPID.AutoSize = true;
            label_CGMAPID.Location = new System.Drawing.Point(750, 20);
            label_CGMAPID.Name = "CGMAPID";
            label_CGMAPID.Size = new System.Drawing.Size(35, 12);
            label_CGMAPID.TabIndex = 0;
            label_CGMAPID.Text = "CGMAPID";
            groupBox_NodeInfo.Controls.Add(label_CGMAPID);
            //添加textBox_CGMAPID
            textBox_CGMAPID = new TextBox();
            textBox_CGMAPID.Location = new System.Drawing.Point(810, 17);
            textBox_CGMAPID.Name = "textBox_CGMAPID";
            textBox_CGMAPID.ReadOnly = true;
            textBox_CGMAPID.Size = new System.Drawing.Size(50, 21);
            textBox_CGMAPID.TabIndex = 1;
            groupBox_NodeInfo.Controls.Add(textBox_CGMAPID);

            #region CF相关信息（暂未添加）
            //添加label_CFID
            label_CFID = new Label();
            label_CFID.AutoSize = true;
            label_CFID.Location = new System.Drawing.Point(590, 20);
            label_CFID.Name = "label_CFID";
            label_CFID.Size = new System.Drawing.Size(50, 12);
            label_CFID.TabIndex = 0;
            label_CFID.Text = "CFID";
            groupBox_NodeInfo.Controls.Add(label_CFID);
            ////添加textBox_CFID
            textBox_CFID = new TextBox();
            textBox_CFID.Location = new System.Drawing.Point(640, 17);
            textBox_CFID.Name = "textBox_CGID";
            textBox_CFID.ReadOnly = true;
            textBox_CFID.Size = new System.Drawing.Size(60, 21);
            textBox_CFID.TabIndex = 1;
            groupBox_NodeInfo.Controls.Add(textBox_CFID);
            ////添加label_FileName
            //label_FileName = new Label();
            //label_FileName.AutoSize = true;
            //label_FileName.Location = new System.Drawing.Point(190, 50);
            //label_FileName.Name = "label_FileName";
            //label_FileName.Size = new System.Drawing.Size(50, 12);
            //label_FileName.TabIndex = 0;
            //label_FileName.Text = "FileName";
            //groupBox_NodeInfo.Controls.Add(label_FileName);
            ////添加textBox_FileName
            //textBox_FileName = new TextBox();
            //textBox_FileName.Location = new System.Drawing.Point(260, 47);
            //textBox_FileName.Name = "textBox_FileName";
            //textBox_FileName.ReadOnly = true;
            //textBox_FileName.Size = new System.Drawing.Size(200, 21);
            //textBox_FileName.TabIndex = 1;
            //groupBox_NodeInfo.Controls.Add(textBox_FileName);
            ////添加label_StartLine
            //label_StartLine = new Label();
            //label_StartLine.AutoSize = true;
            //label_StartLine.Location = new System.Drawing.Point(490, 50);
            //label_StartLine.Name = "label_StartLine";
            //label_StartLine.Size = new System.Drawing.Size(70, 12);
            //label_StartLine.TabIndex = 0;
            //label_StartLine.Text = "StartLine";
            //groupBox_NodeInfo.Controls.Add(label_StartLine);
            ////添加textBox_StartLine
            //textBox_StartLine = new TextBox();
            //textBox_StartLine.Location = new System.Drawing.Point(570, 47);
            //textBox_StartLine.Name = "textBox_StartLine";
            //textBox_StartLine.ReadOnly = true;
            //textBox_StartLine.Size = new System.Drawing.Size(50, 21);
            //textBox_StartLine.TabIndex = 1;
            //groupBox_NodeInfo.Controls.Add(textBox_StartLine);
            ////添加label_EndLine
            //label_EndLine = new Label();
            //label_EndLine.AutoSize = true;
            //label_EndLine.Location = new System.Drawing.Point(650, 50);
            //label_EndLine.Name = "label_EndLine";
            //label_EndLine.Size = new System.Drawing.Size(70, 12);
            //label_EndLine.TabIndex = 0;
            //label_EndLine.Text = "EndLine";
            //groupBox_NodeInfo.Controls.Add(label_EndLine);
            ////添加textBox_EndLine
            //textBox_EndLine = new TextBox();
            //textBox_EndLine.Location = new System.Drawing.Point(720, 47);
            //textBox_EndLine.Name = "textBox_EndLine";
            //textBox_EndLine.ReadOnly = true;
            //textBox_EndLine.Size = new System.Drawing.Size(50, 21);
            //textBox_EndLine.TabIndex = 1;
            //groupBox_NodeInfo.Controls.Add(textBox_EndLine); 
            #endregion

            groupBox_NodeInfo.Enabled = false;
            #endregion
        }

        /// <summary>
        /// 根据genealogyXml显示克隆家系信息
        /// </summary>
        /// <param name="genealogyXml"></param>
        private void ShowInfo(XmlDocument genealogyXml)
        {
            XmlElement infoEle = (XmlElement)genealogyXml.DocumentElement.SelectSingleNode("GenealogyInfo");
            this.textBox_GenealogyID.Text = infoEle.GetAttribute("id");
            this.textBox_StartVersion.Text = infoEle.GetAttribute("startversion");
            this.textBox_EndVersion.Text = infoEle.GetAttribute("endversion");
            this.textBox_Age.Text = infoEle.GetAttribute("age");
            this.textBox_CGMAPID.Text = infoEle.GetAttribute("");
            infoEle = (XmlElement)genealogyXml.DocumentElement.SelectSingleNode("EvolutionPatternCount");
            this.textBox_STATIC.Text = infoEle.GetAttribute("STATIC");
            this.textBox_SAME.Text = infoEle.GetAttribute("SAME");
            this.textBox_ADD.Text = infoEle.GetAttribute("ADD");
            this.textBox_SUBSTRACT.Text = infoEle.GetAttribute("DELETE");
            this.textBox_CONSISTENTCHANGE.Text = infoEle.GetAttribute("CONSISTENTCHANGE");
            this.textBox_INCONSISTENTCHANGE.Text = infoEle.GetAttribute("INCONSISTENTCHANGE");
            this.textBox_SPLIT.Text = infoEle.GetAttribute("SPLIT");

        }

        internal void cgv_MouseMove(object sender, MouseEventArgs e)
        {
           
        }

        internal void cgv_MouseClick(object sender, MouseEventArgs e)
        {           
            //清空已标记节点
            tabControl1.ContextMenuStrip = null;
            CloneGenealogyViz cgv = (CloneGenealogyViz)sender;
            int ex = (int)((e.X - cgv.AutoScrollPosition.X) / cgv.X);
            int ey = (int)((e.Y - cgv.AutoScrollPosition.Y) / cgv.Y);
            Point newPoint = e.Location;
            newPoint.X = ex;
            newPoint.Y = ey;

            int flag = 0;
            int cgnum = 0;
            foreach (CGNode cgNode in cgv.NodeList)
            {               
                Rectangle rect = new Rectangle((cgNode.rect.X + cgv.AutoScrollPosition.X), (cgNode.rect.Y + cgv.AutoScrollPosition.Y), cgNode.rect.Width, cgNode.rect.Height);
                Region cgRegion = new Region(rect);  //创建表示CGNode区域的对象
                int cfNum = cgNode.cfNum;
                Region[] cfRegions = new Region[cfNum];
                char[] tagArray = new char[cfNum];
                GraphicsPath[] gpForCFNodes = new GraphicsPath[cfNum];               
                for (int i = 0; i < cfNum; i++)
                {                   
                    Rectangle outerRect = new Rectangle((cgNode.cfNodes[i].center.X + cgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[i].center.Y + cgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                    gpForCFNodes[i] = new GraphicsPath();
                    gpForCFNodes[i].AddRectangle(outerRect);
                    cfRegions[i] = new Region(gpForCFNodes[i]);
                    tagArray[i] = cgNode.cfNodes[i].tag;   
                }
                cgv.G = cgv.CreateGraphics();             
                cgv.G.ScaleTransform(cgv.X, cgv.Y);
                cgv.G.FillRegion(CloneGenealogyViz.brush2, cgRegion);
                this.groupBox_NodeInfo.Enabled = false;
                this.textBox_CGID.Text = "";
                this.textBox_Version.Text = "";
                this.textBox_CGSize.Text = "";
                this.textBox_CGMAPID.Text = "";
                for (int j = 0; j < cfNum; j++)
                {
                    Rectangle outerRect = new Rectangle((cgNode.cfNodes[j].center.X + cgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[j].center.Y + cgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                    if (cgNode.PatternFlag() == false)
                        cgv.G.DrawRectangle(CloneGenealogyViz.pen1, outerRect);
                    else
                        cgv.G.DrawRectangle(CloneGenealogyViz.pen4, outerRect);
                    cgv.G.FillRegion(CloneGenealogyViz.brush2, cfRegions[j]);
                    cgv.G.DrawString(tagArray[j].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[j].center.X + cgv.AutoScrollPosition.X) - 8, (cgNode.cfNodes[j].center.Y + cgv.AutoScrollPosition.Y) - 8);
                }
            }
            //鼠标右键相应：右键菜单可提供查看代码片段
            if (e.Button == MouseButtons.Right)
            {
                CloneGenealogyViz curcgv = (CloneGenealogyViz)sender;
               // ex = (int)((e.X - curcgv.AutoScrollPosition.X) / curcgv.X);
               // ey = (int)((e.Y - curcgv.AutoScrollPosition.Y) / curcgv.Y);
                ex = (int)(e.X / curcgv.X);
                ey = (int)(e.Y / curcgv.Y);
                newPoint = e.Location;
                newPoint.X = ex;
                newPoint.Y = ey;
                int k = -1;
                int selectedCGIndex = -1;   //标记选中的CG
                int cfIndex = -1;
                foreach (CGNode cgNode in curcgv.NodeList)
                {
                    k++;
                    cgnum++;
                    Rectangle rect = new Rectangle((cgNode.rect.X + curcgv.AutoScrollPosition.X), (cgNode.rect.Y + curcgv.AutoScrollPosition.Y), cgNode.rect.Width, cgNode.rect.Height);
                    Region cgRegion = new Region(rect);  //创建表示CGNode区域的对象
                    //int cfNum = cgNode.cfNum > CGNode.maxCFNum ? 2 : cgNode.cfNum;
                    int cfNum = cgNode.cfNum;
                    Region[] cfRegions = new Region[cfNum];
                    char[] tagArray = new char[cfNum];
                    GraphicsPath[] gpForCFNodes = new GraphicsPath[cfNum];
                    for (int i = 0; i < cfNum; i++)
                    {
                        Rectangle outerRect = new Rectangle((cgNode.cfNodes[i].center.X + curcgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[i].center.Y + curcgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                        gpForCFNodes[i] = new GraphicsPath();
                        gpForCFNodes[i].AddRectangle(outerRect);
                        cfRegions[i] = new Region(gpForCFNodes[i]);

                        tagArray[i] = cgNode.cfNodes[i].tag;
                    }


                    if (cgRegion.IsVisible(newPoint)) //如果鼠标在CGNode区域
                    {
                        curcgv.G = curcgv.CreateGraphics();
                        curcgv.G.ScaleTransform(cgv.X, cgv.Y);
                        bool mouseInThisNode = false;
                        cfIndex = -1;
                        foreach (Region cfRegion in cfRegions)  //判断鼠标是否在CFNode区域
                        {
                            cfIndex++;
                            if (cfRegion.IsVisible(newPoint))
                            {
                                curcgv.G.FillRegion(CloneGenealogyViz.brush4, cfRegion);
                                curcgv.G.DrawString(tagArray[cfIndex].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[cfIndex].center.X + curcgv.AutoScrollPosition.X - 8), (cgNode.cfNodes[cfIndex].center.Y + curcgv.AutoScrollPosition.Y - 8));
                                // curCgv.G.FillRegion(CloneGenealogyViz.brush3, cgRegion);
                                this.groupBox_NodeInfo.Enabled = true;
                                mouseInThisNode = true;
                                selectedCGIndex = k;
                                if (cgnum == 1)
                                    flag = 1;
                                break;
                            }
                        }

                        if (!mouseInThisNode) //如果鼠标不在CGNode或CFNode区域
                        {
                            curcgv.G.ScaleTransform(curcgv.X, curcgv.Y);
                            curcgv.G.FillRegion(CloneGenealogyViz.brush2, cgRegion);
                            this.groupBox_NodeInfo.Enabled = false;
                            this.textBox_CGID.Text = "";
                            this.textBox_Version.Text = "";
                            this.textBox_CGSize.Text = "";

                            for (int j = 0; j < cfNum; j++)
                            {
                                Rectangle outerRect = new Rectangle((cgNode.cfNodes[j].center.X + curcgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[j].center.Y + curcgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                                if (cgNode.PatternFlag() == false)
                                    curcgv.G.DrawRectangle(CloneGenealogyViz.pen1, outerRect);
                                else
                                    curcgv.G.DrawRectangle(CloneGenealogyViz.pen4, outerRect);
                                curcgv.G.FillRegion(CloneGenealogyViz.brush2, cfRegions[j]);
                                curcgv.G.DrawString(tagArray[j].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[j].center.X + curcgv.AutoScrollPosition.X - 8), (cgNode.cfNodes[j].center.Y + curcgv.AutoScrollPosition.Y - 8));
                            }
                        }
                    }
                }
                if (selectedCGIndex != -1)  //如果有要显示的CGInfo（即鼠标落在某个CGNode或其中的CFNode内）
                {
                    tabControl1.ContextMenuStrip = this.contextMenuStrip2;
                    contextMenuStrip2.Items[0].Enabled = true;    //ViewCloneFragment项
                    contextMenuStrip2.Items[1].Enabled = true;    //ViewFullCode项
                    contextMenuStrip2.Items[2].Enabled = true;   //ViewSourceDiff项
                    this.groupBox_NodeInfo.Enabled = true;
                    this.textBox_CGID.ForeColor = Color.Red;
                    this.textBox_CGID.Text = curcgv.NodeList[selectedCGIndex].Info.id;
                    this.textBox_Version.ForeColor = Color.Red;
                    this.textBox_Version.Text = curcgv.NodeList[selectedCGIndex].Info.version;
                    this.textBox_CGSize.Text = curcgv.NodeList[selectedCGIndex].Info.size.ToString();
                    this.textBox_CFID.Text = (cfIndex + 1).ToString();
                    if (flag != 1)
                        this.textBox_CGMAPID.Text = curcgv.NodeList[selectedCGIndex].CGMAPid().ToString();
                    else
                        this.textBox_CGMAPID.Text = "";
                }
                return;
            }

            //鼠标左键被按下，进行如下操作
            CloneGenealogyViz curCgv = (CloneGenealogyViz)sender;
            ex = (int)(e.X / curCgv.X);
            ey = (int)(e.Y / curCgv.Y);
           
            newPoint = e.Location;
            newPoint.X = ex;
            newPoint.Y = ey;

            int K = -1;
            int SelectedCGIndex = -1;   //标记选中的CG
            int CfIndex = -1;
            foreach (CGNode cgNode in curCgv.NodeList)
            {
                K++;
                cgnum++;
                Rectangle rect = new Rectangle((cgNode.rect.X + curCgv.AutoScrollPosition.X), (cgNode.rect.Y + curCgv.AutoScrollPosition.Y), cgNode.rect.Width, cgNode.rect.Height);
                Region cgRegion = new Region(rect);  //创建表示CGNode区域的对象
                // int cfNum = cgNode.cfNum > CGNode.maxCFNum ? 2 : cgNode.cfNum;
                int cfNum = cgNode.cfNum;
                Region[] cfRegions = new Region[cfNum];
                char[] tagArray = new char[cfNum];
                GraphicsPath[] gpForCFNodes = new GraphicsPath[cfNum];

                for (int i = 0; i < cfNum; i++)
                {
                    Rectangle outerRect = new Rectangle((cgNode.cfNodes[i].center.X + curCgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[i].center.Y + curCgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                    gpForCFNodes[i] = new GraphicsPath();
                    gpForCFNodes[i].AddRectangle(outerRect);
                    cfRegions[i] = new Region(gpForCFNodes[i]);

                    tagArray[i] = cgNode.cfNodes[i].tag;
                }

                if (cgRegion.IsVisible(newPoint)) //如果鼠标在CGNode区域
                {
                    curCgv.G = curCgv.CreateGraphics();
                    curCgv.G.ScaleTransform(curCgv.X, curCgv.Y);
                    bool mouseInThisNode = false;
                    CfIndex = -1;
                    foreach (Region cfRegion in cfRegions)  //判断鼠标是否在CFNode区域
                    {
                        CfIndex++;
                        if (cfRegion.IsVisible(newPoint))
                        {
                            curCgv.G.FillRegion(CloneGenealogyViz.brush4, cfRegion);
                            curCgv.G.DrawString(tagArray[CfIndex].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[CfIndex].center.X + curCgv.AutoScrollPosition.X - 8), (cgNode.cfNodes[CfIndex].center.Y + curCgv.AutoScrollPosition.Y - 8));
                            // curCgv.G.FillRegion(CloneGenealogyViz.brush3, cgRegion);
                            this.groupBox_NodeInfo.Enabled = true;
                            mouseInThisNode = true;
                            SelectedCGIndex = K;
                            if (cgnum == 1)
                                flag = 1;
                            break;
                        }
                    }

                    if (!mouseInThisNode) //如果鼠标不在CGNode或CFNode区域
                    {
                        curCgv.G.ScaleTransform(curCgv.X, curCgv.Y);
                        curCgv.G.FillRegion(CloneGenealogyViz.brush2, cgRegion);
                        this.groupBox_NodeInfo.Enabled = false;
                        this.textBox_CGID.Text = "";
                        this.textBox_Version.Text = "";
                        this.textBox_CGSize.Text = "";

                        for (int j = 0; j < cfNum; j++)
                        {
                            Rectangle outerRect = new Rectangle((cgNode.cfNodes[j].center.X + curCgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[j].center.Y + curCgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                            if (cgNode.PatternFlag() == false)
                                curCgv.G.DrawRectangle(CloneGenealogyViz.pen1, outerRect);
                            else
                                curCgv.G.DrawRectangle(CloneGenealogyViz.pen4, outerRect);
                            curCgv.G.FillRegion(CloneGenealogyViz.brush2, cfRegions[j]);
                            curCgv.G.DrawString(tagArray[j].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[CfIndex].center.X + curCgv.AutoScrollPosition.X - 8), (cgNode.cfNodes[CfIndex].center.Y + curCgv.AutoScrollPosition.Y - 8));
                        }
                    }
                }
            }
            if (SelectedCGIndex != -1)  //如果有要显示的CGInfo（即鼠标落在某个CGNode或其中的CFNode内）
            {
                this.groupBox_NodeInfo.Enabled = true;
                this.textBox_CGID.ForeColor = Color.Red;
                this.textBox_CGID.Text = curCgv.NodeList[SelectedCGIndex].Info.id;
                this.textBox_Version.ForeColor = Color.Red;
                this.textBox_Version.Text = curCgv.NodeList[SelectedCGIndex].Info.version;
                this.textBox_CGSize.Text = curCgv.NodeList[SelectedCGIndex].Info.size.ToString();
                this.textBox_CFID.Text = (CfIndex + 1).ToString();
                if (flag != 1)
                    this.textBox_CGMAPID.Text = curCgv.NodeList[SelectedCGIndex].CGMAPid().ToString();
                else
                    this.textBox_CGMAPID.Text = "";
                SelectedCGIndex = -1;
            }
        }

        internal void cgv_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            { return; }
            //双击鼠标左键，进行如下操作
            CloneGenealogyViz curCgv = (CloneGenealogyViz)sender;
            int ex = (int)(e.X  / curCgv.X);
            int ey = (int)(e.Y  / curCgv.Y);
            Point newPoint = e.Location;
            newPoint.X = ex;
            newPoint.Y = ey;
            int k = -1;
            int selectedCGIndex = -1;   //标记选中的CG
            int cfIndex = -1;
            foreach (CGNode cgNode in curCgv.NodeList)
            {
                k++;
                Rectangle rect = new Rectangle((cgNode.rect.X + curCgv.AutoScrollPosition.X), (cgNode.rect.Y + curCgv.AutoScrollPosition.Y), cgNode.rect.Width, cgNode.rect.Height);
                Region cgRegion = new Region(rect);  //创建表示CGNode区域的对象
                // int cfNum = cgNode.cfNum > CGNode.maxCFNum ? 2 : cgNode.cfNum;
                int cfNum = cgNode.cfNum;
                Region[] cfRegions = new Region[cfNum];
                char[] tagArray = new char[cfNum];
                GraphicsPath[] gpForCFNodes = new GraphicsPath[cfNum];
                for (int i = 0; i < cfNum; i++)
                {
                    Rectangle outerRect = new Rectangle((cgNode.cfNodes[i].center.X + curCgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[i].center.Y + curCgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                    gpForCFNodes[i] = new GraphicsPath();
                    gpForCFNodes[i].AddRectangle(outerRect);
                    cfRegions[i] = new Region(gpForCFNodes[i]);
                    // cgRegion.Xor(cfRegions[i]); //去掉与CFNode重合的部分
                    tagArray[i] = cgNode.cfNodes[i].tag;
                }
                if (cgRegion.IsVisible(newPoint)) //如果鼠标在CGNode区域
                {
                    curCgv.G = curCgv.CreateGraphics();
                    bool mouseInThisNode = false;
                    cfIndex = -1;
                    foreach (Region cfRegion in cfRegions)  //判断鼠标是否在CFNode区域
                    {
                        cfIndex++;
                        if (cfRegion.IsVisible(newPoint))
                        {
                            mouseInThisNode = true;
                            selectedCGIndex = k;
                            break;
                        }
                    }

                    if (!mouseInThisNode) //如果鼠标不在CGNode或CFNode区域
                    {
                        curCgv.G.ScaleTransform(curCgv.X, curCgv.Y);
                        curCgv.G.FillRegion(CloneGenealogyViz.brush2, cgRegion);
                        this.groupBox_NodeInfo.Enabled = false;
                        this.textBox_CGID.Text = "";
                        this.textBox_Version.Text = "";
                        this.textBox_CGSize.Text = "";

                        for (int j = 0; j < cfNum; j++)
                        {
                            Rectangle outerRect = new Rectangle((cgNode.cfNodes[j].center.X + curCgv.AutoScrollPosition.X) - (int)(0.5 * 30), (cgNode.cfNodes[j].center.Y + curCgv.AutoScrollPosition.Y) - (int)(0.5 * 20), 30, 20);
                            if (cgNode.PatternFlag() == false)
                                curCgv.G.DrawRectangle(CloneGenealogyViz.pen1, outerRect);
                            else
                                curCgv.G.DrawRectangle(CloneGenealogyViz.pen4, outerRect);
                            curCgv.G.FillRegion(CloneGenealogyViz.brush2, cfRegions[j]);
                            curCgv.G.DrawString(tagArray[j].ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, (cgNode.cfNodes[j].center.X + curCgv.AutoScrollPosition.X - 8), (cgNode.cfNodes[j].center.Y + curCgv.AutoScrollPosition.Y - 8));
                        }
                    }
                }
            }
            if (selectedCGIndex != -1)  //如果有要显示的CGInfo（即鼠标落在某个CGNode或其中的CFNode内）
            {
                //根据CGInfo构造文件名，打开文件并定位至指定CG
                string fileName = curCgv.NodeList[selectedCGIndex].Info.version + "_blocks-clones-0.3-classes-withCRD.xml";
                GetCRDDirFromTreeView1();
                string filePath = CloneRegionDescriptor.CrdDir + @"\blocks\" + fileName;
                int index = Int32.Parse(curCgv.NodeList[selectedCGIndex].Info.id) - 1 + 4;  //获取选中CG的索引
                #region 在tabPage中显示
                try
                {
                    //Load XML
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(filePath);
                    tabControl1.TabPages.Add(fileName);    //创建新的tabPage
                    tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                    //创建TreeView控件
                    TreeView newTreeView = new TreeView();
                    tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
                    newTreeView.Dock = DockStyle.Fill;
                    ShowXMLTree(xdoc, ref newTreeView);
                    newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.newTreeView_NodeMouseClick);
                    //添加预定义的上下文菜单
                    newTreeView.ContextMenuStrip = this.contextMenuStrip1;

                    //newTreeView.HideSelection = false;
                    newTreeView.SelectedNode = newTreeView.Nodes[0].Nodes[index].Nodes[cfIndex + 1];
                    newTreeView.SelectedNode.BackColor = Color.LightSkyBlue;
                    selectedCGIndex = -1;

                }
                catch (XmlException xe)
                {
                    MessageBox.Show("Load XML file failed! " + xe.Message);
                    return;
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                    return;
                }
                #endregion
            }
        }
      
        internal void cgv_MouseDown(object sender, MouseEventArgs e)
        {          
           ((CloneGenealogyViz)sender).Focus();          
        }
        
        private void cgv_MouseWheel(object sender, MouseEventArgs e)
        {            
            CloneGenealogyViz cgv = (CloneGenealogyViz)sender;         
            if (Control.ModifierKeys == Keys.Control)
            {
                if (e.Delta < 0)//鼠标滚轮向下滚动
                { cgv.X -= 0.1f; cgv.Y -= 0.1f; }
                else//鼠标滚轮向上滚动
                { cgv.X += 0.1f; cgv.Y += 0.1f; }
                cgv.Invalidate();               
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //提取度量的CRD文件的生成
            CloneRegionDescriptorforMetric crd = new CloneRegionDescriptorforMetric();
            GetAllLeafNodes(treeView1.Nodes[0]);    //获取treeView1所有叶子节点，保存在this.treeView1_LeafNodes中

            foreach (TreeNode node in this.treeView1_LeafNodes)
            {
                if (node.Parent.Text == "blocks" || node.Parent.Text == "functions")    //判断xml所在文件夹
                {
                    string path = node.Tag.ToString();
                    XmlDocument xmlFile = new XmlDocument();
                    if (path.IndexOf(@"-classes.xml") != -1 && path.IndexOf("-emCRD.xml") == -1)
                    {
                        xmlFile.Load(path);
                        crd.GenerateForSys(xmlFile, path.Substring(path.LastIndexOf(@"\") + 1));
                    }
                }
            }
            //已删除ShowCRD选项卡，改在ShowXMLTree中显示
            //tabControl1.SelectedTab = tabControl1.TabPages[2];
            //treeView3.Nodes[0].Text = "Click on a file in the filetree to show its CRD.";
            Global.CrdGenerationState = 2;  //置生成crd标志位2（生成所有文件crd）
            RefreshTreeView1();

            ////提取度量的MAP文件的生成
            //Global.mappingVersionRange = "ALLVERSIONS"; //置映射范围为全部版本
            //Global.mainForm.GetCRDDirFromTreeView1();   //获取CRD文件所在文件夹路径           
            //DirectoryInfo dir = null;

            //try
            //{
            //    dir = new DirectoryInfo(CloneRegionDescriptorforMetric.CrdDir + @"\blocks");
            //}
            //catch (Exception ee)
            //{
            //    MessageBox.Show("Get CRD directory of blocks failed! " + ee.Message);
            //}
            //FileInfo[] crdFiles = null;
            //try
            //{
            //    crdFiles = dir.GetFiles();
            //}
            //catch (Exception ee)
            //{
            //    MessageBox.Show("Get CRD files on blocks failed! " + ee.Message);
            //}
            //List<string> fileNames = new List<string>();
            //foreach (FileInfo info in crdFiles)
            //{
            //    if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
            //    { continue; }
            //    else
            //    {
            //        //选出所有带有"_blocks-"标签的-withCRD.xml文件，记录它们的文件名（不包含路径）
            //        if (info.Name.IndexOf("-emCRD.xml") != -1 && info.Name.IndexOf("_blocks-") != -1)
            //        { fileNames.Add(info.Name); }
            //    }
            //}
            //if (fileNames.Count > 1)
            //{
            //    for (int i = 0; i < fileNames.Count - 1; i++)
            //    {
            //        CGMapEventArgs ee = new CGMapEventArgs();
            //        ee.srcStr = fileNames[i];
            //        ee.destStr = fileNames[i + 1];
            //        //OnMapSettingFinished(ee); //不采取触发事件的方式，原因：并发进程的处理问题？？
            //        AdjacentVersionMappingforMetric adjMap = new AdjacentVersionMappingforMetric();
            //        adjMap.OnStartMapping(this, ee);    //直接调用事件的响应函数OnStartMapping
            //    }
            //    MessageBox.Show("Mapping All Versions on Blocks level Finished!");
            //}
            //else
            //{
            //    MessageBox.Show("No withCRD.xml files at Blocks level for mapping!");
            //}
            //if (Global.MapState != 2)
            //{ Global.MapState = 2; }                   
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {           
            ClearText(splitContainer1.Panel2);            
            tabControl1.TabPages.Add("CloneGenealogyViz");
            tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
           
            GenealogyAllViz gav = new GenealogyAllViz();
            gav.MouseDown += new MouseEventHandler(this.gav_MouseDown);
            gav.MouseWheel += new MouseEventHandler(this.gav_MouseWheel);
            gav.Init();           
            tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(gav);
        }

        internal void gav_MouseMove(object sender, MouseEventArgs e)
        {
        }

        internal void gav_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                return;
            }
            int K = -1;
            int SelectedCGIndex = -1;
            
            //双击鼠标左键，进行如下操作
            GenealogyAllViz Gav = (GenealogyAllViz)sender;
            
            int ex = (int)((e.X - Gav.AutoScrollPosition.X) / Gav.X);
            int ey = (int)((e.Y - Gav.AutoScrollPosition.Y) / Gav.Y);
            Point newPoint = e.Location;
            newPoint.X = ex;
            newPoint.Y = ey;
            foreach (Node node in Gav.NodeList)
            {
                K++;
                Region cgRegion = new Region(node.rect);  //创建表示CGNode区域的对象
                if (cgRegion.IsVisible(newPoint)) //如果鼠标在CGNode区域
                {
                    SelectedCGIndex = K;
                    break;
                }
            }
            if (SelectedCGIndex != -1)
            {
                string fileName = Gav.NodeList[SelectedCGIndex].FileName();
                string filePath = Global.mainForm._folderPath + @"\GenealogyFiles\blocks\" + fileName;
                XmlDocument genealogyXml = new XmlDocument();
                genealogyXml.Load(filePath);
                ShowInfo(genealogyXml);
            }
        }

        internal void gav_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {                  
                return;               
            }
            int K = -1;
            int SelectedCGIndex = -1; 
            //双击鼠标左键，进行如下操作
            GenealogyAllViz Gav = (GenealogyAllViz)sender;
            int ex = (int)((e.X - Gav.AutoScrollPosition.X) / Gav.X);
            int ey = (int)((e.Y - Gav.AutoScrollPosition.Y) / Gav.Y);

            Point newPoint = e.Location;
            newPoint.X = ex;
            newPoint.Y = ey;
            foreach(Node node in Gav.NodeList)
            {
                K++;
                Region cgRegion = new Region(node.rect);  //创建表示CGNode区域的对象
                if (cgRegion.IsVisible(newPoint)) //如果鼠标在CGNode区域
                {
                    SelectedCGIndex = K;
                    break;
                }
            }
            if (SelectedCGIndex != -1)
            {
                string fileName = Gav.NodeList[SelectedCGIndex].FileName();              
                string filePath = Global.mainForm._folderPath + @"\GenealogyFiles\blocks\" + fileName;
                XmlDocument genealogyXml = new XmlDocument();
                genealogyXml.Load(filePath);
                //tabControl1.Dock = DockStyle.Top;
                //tabControl1.Size = new Size(636, 546);
                tabControl1.TabPages.Add(filePath.Substring(filePath.LastIndexOf("\\") + 1));//创建新的tabPage，显示文件名
                tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                //创建CloneGenealogyViz对象
                CloneGenealogyViz cgv = new CloneGenealogyViz();
                cgv.MouseDown += new MouseEventHandler(this.cgv_MouseDown);
                cgv.MouseWheel += new MouseEventHandler(this.cgv_MouseWheel);
                cgv.Init();
                cgv.SetGenealogyXml(genealogyXml);
                tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(cgv);                
                ShowInfo(genealogyXml);
            }
        }

        private void gav_MouseDown(object sender, MouseEventArgs e)
        {
            ((GenealogyAllViz)sender).Focus();
        }

        private void gav_MouseWheel(object sender, MouseEventArgs e)
        {
            GenealogyAllViz cgv = (GenealogyAllViz)sender;
            if (Control.ModifierKeys == Keys.Control)
            {
                if (e.Delta < 0)//鼠标滚轮向下滚动
                { cgv.X -= 0.1f; cgv.Y -= 0.1f; }
                else//鼠标滚轮向上滚动
                { cgv.X += 0.1f; cgv.Y += 0.1f; }
                cgv.Invalidate();              
            }
        }
        
        private void myContextMenuItem1_Click(TreeView treeView, object sender, EventArgs e)
        {
            string fileName = this.textBox_Version.Text + "_blocks-clones-0.3-classes-withCRD.xml";
            GetCRDDirFromTreeView1();
            string filePath = CloneRegionDescriptor.CrdDir + @"\blocks\" + fileName;
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(filePath);
            tabControl1.TabPages.Add(fileName);    //创建新的tabPage
            //tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;
            ShowXMLTree(xdoc, ref newTreeView);
             newTreeView.SelectedNode = newTreeView.Nodes[0].Nodes[Int32.Parse(this.textBox_CGID.Text) - 1 + 4].Nodes[Int32.Parse(this.textBox_CFID.Text)];

            CloneSourceInfo sourceInfo = (CloneSourceInfo)newTreeView.SelectedNode.Tag;
            ShowCodeForm showCodeForm = new ShowCodeForm(); 
            showCodeForm.SetCloneSourceInfo(sourceInfo);
            showCodeForm.IsShowFullCode = false;
            showCodeForm.Show();       
        }

        private void myContextMenuItem2_Click(TreeView treeView, object sender, EventArgs e)
        {
            string fileName = this.textBox_Version.Text + "_blocks-clones-0.3-classes-withCRD.xml";
            GetCRDDirFromTreeView1();
            string filePath = CloneRegionDescriptor.CrdDir + @"\blocks\" + fileName;
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(filePath);
            tabControl1.TabPages.Add(fileName);    //创建新的tabPage
            //tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;
            ShowXMLTree(xdoc, ref newTreeView);
            newTreeView.SelectedNode = newTreeView.Nodes[0].Nodes[Int32.Parse(this.textBox_CGID.Text) - 1 + 4].Nodes[Int32.Parse(this.textBox_CFID.Text)];

            CloneSourceInfo sourceInfo = (CloneSourceInfo)newTreeView.SelectedNode.Tag;
            ShowCodeForm showCodeForm = new ShowCodeForm();
            showCodeForm.SetCloneSourceInfo(sourceInfo);
            showCodeForm.IsShowFullCode = true;
            showCodeForm.Show();
        }

        private void myContextMenuItem3_Click(TreeView treeView, object sender, EventArgs e)
        {
            if (this.textBox_CGMAPID.Text == "")
            {
                MessageBox.Show("No Matched Nodes");
                return;
            }
            string subfileName = this.textBox_Version.Text + "-MapOnBlocks";
            GetMAPDirFromTreeView1();          
            string[] fileList = System.IO.Directory.GetFileSystemEntries(Global.mainForm._folderPath + @"\MAPFiles\blocks\");
            int filenum = -1;
            foreach (string file in fileList)
            {
                filenum ++;
                if (file.Contains(subfileName))
                {
                   break;
                }
            }
            string filePath= fileList[filenum];
            string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);              

            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(filePath);
            tabControl1.TabPages.Add(fileName);    //创建新的tabPage
            //tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;
            ShowXMLTree(xdoc, ref newTreeView);

            XmlElement CGXml = null;
            List<XmlElement> cfMAPList = new List<XmlElement>();  //存放所有元素的列表
            int cgnum = -1;
            foreach (XmlElement cgMapNode in  xdoc.DocumentElement.SelectNodes("CGMap"))
            {
                cgnum++;
                if (cgMapNode.GetAttribute("cgMapid") == this.textBox_CGMAPID.Text)
                {
                    CGXml = cgMapNode;
                    break;
                }
            }
            int cfnum = -1;
            foreach (XmlElement cfMapNode in  CGXml.SelectNodes("CFMap"))
            {
                cfnum++;
                if (cfMapNode.GetAttribute("srcCFid") == this.textBox_CFID.Text)
                {                   
                    break;
                }
            }
            if (cfnum == -1)
            {
                MessageBox.Show("No Matched Nodes");
            }
            else
            {
                newTreeView.SelectedNode = newTreeView.Nodes[0].Nodes[cgnum + 2].Nodes[cfnum + 3];

                string text = newTreeView.SelectedNode.Text;
                XmlElement node = (XmlElement)newTreeView.SelectedNode.Tag;
                List<string> srcCF = new List<string>();
                List<string> destCF = new List<string>();
                CloneSourceInfo srcCFInfo = new CloneSourceInfo();
                CloneSourceInfo destCFInfo = new CloneSourceInfo();
                srcCF = GetCFSourceFromCFMapNode(node, true, out srcCFInfo);
                destCF = GetCFSourceFromCFMapNode(node, false, out destCFInfo);
                if (srcCF != null && destCF != null)
                {
                    //计算Diff
                    Diff.UseDefaultStrSimTh();  //注：此语句不能少。否则Diff.StrSimThreshold值为0
                    Diff.DiffInfo diffInfo = Diff.DiffFiles(srcCF, destCF);
                    //显示Diff
                    ShowDiffForm showDiffForm = new ShowDiffForm();
                    showDiffForm.ShowCloneFragmentDiff(srcCFInfo, destCFInfo, diffInfo);
                }
                else
                    MessageBox.Show("No Matched Nodes");
            }
        }

        private void Cluster_Click(object sender, EventArgs e)
        {
            //Clustering test = new Clustering();
            //GenealogyAllViz gav = new GenealogyAllViz();
            //test.adjustxy(gav);
            ClusteringFCM cluster = new ClusteringFCM();
            cluster.FCM();
            //int cnum = cluster.Hcluster();
            //cluster.FCM(cnum);
            
        }
        private void Statistics_Click(object sender, EventArgs e)
        {
            Statistic data = new Statistic();
            data.result();           

        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.metricSettingForm = new MetricSettingForm();
            this.metricSettingForm.Show();
        }

        private void loadAFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.fragmentSettingForm = new FragmentSettingForm();
            this.fragmentSettingForm.Show();
        }

        private void setCloneClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ccSetForm = new CCSetForm();
            this.ccSetForm.Show();
            this.ccSetForm.Init();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            KFCM cluster = new KFCM();
            cluster.kFCM();
        }

        private void toolStripStart_Click(object sender, EventArgs e)
        {

        }












      



        
    }
}