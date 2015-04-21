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
    public partial class MapSettingForm : Form
    {
        public MapSettingForm()
        {
            InitializeComponent();
        }
        
        //定义MapSettingFinished事件，用于向主窗口发送消息
        public event CGMapEventHandler MapSettingFinished;
        protected virtual void OnMapSettingFinished(CGMapEventArgs e)
        {
            if (this.MapSettingFinished != null)
            {
                this.MapSettingFinished(this,e);
            }
        }
        
        //显示状态：MapBetweenVersions部分激活，MapAll部分未激活
        public void InitShowStatus1()
        {
            this.groupBox1.Enabled = true;
            this.groupBox2.Enabled = false;
        }
        //显示状态：MapBetweenVersions部分未激活，MapAll部分激活
        public void InitShowStatus2()
        {
            this.groupBox1.Enabled = false;
            this.groupBox2.Enabled = true;
        }

        private void button_MapBetweenVersions_Click(object sender, EventArgs e)
        {
            Global.mappingVersionRange = "ADJACENTVERSIONS";    //置映射范围为相邻版本
            //创建CGMapEventArgs对象，并赋予映射系统的信息
            CGMapEventArgs ee = new CGMapEventArgs();
            ee.srcStr = this.textBox1.Text.ToString();
            ee.destStr = this.textBox2.Text.ToString();
            this.Close();
            OnMapSettingFinished(ee);    //引发MapSettingFinished事件
            //if (this.MapSettingFinished != null)//可以用这两行替换上一条语句和OnMapSettingFinished方法
            //{ this.MapSettingFinished(sender, new EventArgs()); }
            
        }

        private void button_MapAll_Click(object sender, EventArgs e)
        {
            if (!this.checkBox_functions.Checked && !this.checkBox_blocks.Checked)
            {
                this.label_Warning2.ForeColor = Color.Red;
                this.label_Warning2.Text = "Please check on at least one granularity type!";
                return;
            }
            this.Close();
            Global.mappingVersionRange = "ALLVERSIONS"; //置映射范围为全部版本
            Global.mainForm.GetCRDDirFromTreeView1();   //获取CRD文件所在文件夹路径           
            DirectoryInfo dir = null; 

            #region 在funtions粒度上映射所有版本
            if (this.checkBox_functions.Checked)
            {
                try
                {
                    dir = new DirectoryInfo(CloneRegionDescriptor.CrdDir + @"\functions");
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Get CRD directory of functions failed! " + ee.Message);
                }
                FileInfo[] crdFiles = null;
                try
                {
                    crdFiles = dir.GetFiles();
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Get CRD files on functions failed! " + ee.Message);
                }
                List<string> fileNames = new List<string>();
                foreach (FileInfo info in crdFiles)
                {
                    if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    { continue; }
                    else
                    {
                        //选出所有带有"_functions-"标签的-withCRD.xml文件，记录它们的文件名（不包含路径）
                        if (info.Name.IndexOf("-withCRD.xml") != -1 && info.Name.IndexOf("_functions-") != -1)
                        { fileNames.Add(info.Name); }
                    }
                }
                if (fileNames.Count > 1)
                {
                    for (int i = 0; i < fileNames.Count - 1; i++)
                    {
                        CGMapEventArgs ee = new CGMapEventArgs();
                        ee.srcStr = fileNames[i];
                        ee.destStr = fileNames[i + 1];
                        //OnMapSettingFinished(ee); //不采取触发事件的方式，原因：并发进程的处理问题？？
                        AdjacentVersionMapping adjMap = new AdjacentVersionMapping();
                        adjMap.OnStartMapping(this, ee);    //直接调用事件的响应函数OnStartMapping
                    }
                    MessageBox.Show("Mapping All Versions on Functions level Finished!");
                }
                else
                {
                    MessageBox.Show("No withCRD.xml file at Functions level for mapping!");
                }
                if (Global.MapState != 2)       //置映射状态为完成某粒度下所有相邻版本的映射
                { Global.MapState = 2; }
            }
            #endregion

            #region 在blocks粒度上映射所有版本
            if (this.checkBox_blocks.Checked)
            {
                try
                {
                    dir = new DirectoryInfo(CloneRegionDescriptor.CrdDir + @"\blocks");
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Get CRD directory of blocks failed! " + ee.Message);
                }
                FileInfo[] crdFiles = null;
                try
                {
                    crdFiles = dir.GetFiles();
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Get CRD files on blocks failed! " + ee.Message);
                }
                List<string> fileNames = new List<string>();
                foreach (FileInfo info in crdFiles)
                {
                    if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    { continue; }
                    else
                    {
                        //选出所有带有"_blocks-"标签的-withCRD.xml文件，记录它们的文件名（不包含路径）
                        if (info.Name.IndexOf("-withCRD.xml") != -1 && info.Name.IndexOf("_blocks-") != -1)
                        { fileNames.Add(info.Name); }
                    }
                }
                if (fileNames.Count > 1)
                {
                    for (int i = 0; i < fileNames.Count - 1; i++)
                    {
                        CGMapEventArgs ee = new CGMapEventArgs();
                        ee.srcStr = fileNames[i];
                        ee.destStr = fileNames[i + 1];
                        //OnMapSettingFinished(ee); //不采取触发事件的方式，原因：并发进程的处理问题？？
                        AdjacentVersionMapping adjMap = new AdjacentVersionMapping();
                        adjMap.OnStartMapping(this, ee);    //直接调用事件的响应函数OnStartMapping
                    }
                    MessageBox.Show("Mapping All Versions on Blocks level Finished!");
                }
                else
                {
                    MessageBox.Show("No withCRD.xml files at Blocks level for mapping!");
                }
                if (Global.MapState != 2)
                { Global.MapState = 2; }
            }
            #endregion
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Global.mainForm.treeView1.CheckBoxes = false;
            Global.mainForm.treeView1.ExpandAll();
        }

        private void button_Clear_Click(object sender, EventArgs e)
        {
            this.textBox1.Text = "";
            this.textBox2.Text = "";
            foreach (TreeNode node in Global.mainForm.treeView1.Nodes[0].Nodes)
            {
                if (node.Nodes.Count == 0)
                {
                    node.Checked = false;   //注：修改Checked属性会引发AfterCheck事件！！
                }
                else
                {
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        if (childNode.Checked)
                        { childNode.Checked = false; }
                    }
                }
            }
            Global.treeView1NodeCheckedNum = 0;
        }
        //用户点击"关闭"按钮关闭窗口时，执行与Cancel相同的操作
        private void MappingSettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            button_Cancel_Click(sender, e);
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }        
    }
}
