using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.IO;

namespace CloneEvolutionAnalyzer
{
    public partial class CCSetForm : Form
    {
        public string path = "";
        public static int flag;
        public static List<string> SrcFragmentCC = new List<string>();
        public static string SrcPathCC;
        public CCSetForm()
        {
            InitializeComponent();
        }
        private void ComboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            string version = comboBox1.Text;
            DirectoryInfo dir = new DirectoryInfo(Global.mainForm._folderPath + @"\blocks");
            FileInfo[] genFiles = dir.GetFiles();
            foreach (FileInfo info in genFiles)
            {
                if (info.Name.Contains(version))
                {
                    version = info.Name; 
                    break;
                }
            }
            path = Global.mainForm._folderPath + @"\blocks\" + version;
            XmlDocument crdXml = new XmlDocument();
            crdXml.Load(path);
            int classnum = int.Parse(crdXml.DocumentElement.SelectSingleNode("classinfo").Attributes[0].Value);
            DataTable CClass = new DataTable();
            DataColumn C = new DataColumn("ID", typeof(int));
            CClass.Columns.Add(C);
            for (int i = 1; i <= classnum; i++)
            {
                DataRow R = CClass.NewRow();
                R[0] = i;
                CClass.Rows.Add(R);
            }
            //进行绑定   
            comboBox2.DisplayMember = "ID";//控件显示的列名   
            comboBox2.ValueMember = "ID";//控件值的列名   
            comboBox2.DataSource = CClass;    

        } 
        public void Init()
        {
            DataTable version = new DataTable();           
            DataColumn C = new DataColumn("Name", typeof(string));
            version.Columns.Add(C);
            DirectoryInfo dir = new DirectoryInfo(Global.mainForm.subSysDirectory);
            DirectoryInfo[] genFiles = dir.GetDirectories();
            foreach (DirectoryInfo info in genFiles)
            {                
                    DataRow R = version.NewRow();
                    R[0] = info.Name;                  
                    version.Rows.Add(R);                   
            }            
            //进行绑定   
            comboBox1.DisplayMember = "Name";//控件显示的列名   
            comboBox1.ValueMember = "Name";//控件值的列名   
            comboBox1.DataSource = version;
            comboBox1.SelectedIndexChanged += new System.EventHandler(ComboBox1_SelectedIndexChanged);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SrcFragmentCC.Clear();
            XmlDocument crdXml = new XmlDocument();
            crdXml.Load(path);
            string ccid = comboBox2.Text;
            foreach (XmlElement classNode in crdXml.DocumentElement.SelectNodes("class"))
            {
                if (classNode.GetAttribute("classid") == ccid)
                {
                    List<string> fullSource = new List<string>();
                    string sourcePath = ((XmlElement)classNode.ChildNodes[0]).GetAttribute("file");
                    //获取目标系统文件夹起始路径
                    string subSysStartPath = Global.mainForm.subSysDirectory;
                    //绝对路径=起始路径+相对路径
                    string subSysPath = subSysStartPath + "\\" + sourcePath;
                    //获得源文件代码
                    SrcPathCC = subSysPath;
                    fullSource = Global.GetFileContent(subSysPath);
                    //提取对应行号的代码片段
                    
                    int startLine = int.Parse(((XmlElement)classNode.ChildNodes[0]).GetAttribute("startline"));
                    int endLine = int.Parse(((XmlElement)classNode.ChildNodes[0]).GetAttribute("endline"));
                    for (int j = startLine - 1; j < endLine; j++) //注意，索引从0起算，而行号从1起算
                        SrcFragmentCC.Add(fullSource[j]);
                    break; 
                }
            }

            FragmentSettingForm.flag = 0;
            FragmentSettingForm test = new FragmentSettingForm();           
            flag = 1;
            test.Confirm(SrcFragmentCC);
            this.Close();


        }

    }
}
