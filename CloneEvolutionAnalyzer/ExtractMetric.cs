using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Text;

namespace CloneEvolutionAnalyzer
{
    class ExtractMetric
    {
        public void GetExtractProcess()
        {

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";//调用dos命令
            string sArguments0 = "cd " + Global.GetProjectDirectory();
            string sArguments1 = @"utf8.py " + Global.mainForm._folderPath + @"\emCRDFiles\blocks\";//cd + 工程目录
            string sArguments11 = @"utf8.py " + Global.mainForm._folderPath + @"\MAPFiles\blocks\";//cd + 工程目录
            string sArguments2 = @"extract1.py " + Global.mainForm._folderPath + @"\emCRDFiles\blocks\ " + Global.mainForm._folderPath + @"\MAPFiles\blocks\";//未添加进化度量，SVM使用
            string sArguments22 = @"extract2.py " + Global.mainForm._folderPath + @"\emCRDFiles\blocks\ " + Global.mainForm._folderPath + @"\MAPFiles\blocks\";//添加进化度量，聚类使用
            //string sArguments3 = "easy.py train test";
            //string sArguments4 = "result+.py";
           
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine(sArguments0);
            p.StandardInput.WriteLine(sArguments1);
            p.StandardInput.WriteLine(sArguments11);
            
            MessageBox.Show("数据预处理完毕");
            //p.StandardInput.WriteLine(sArguments2);
            p.StandardInput.WriteLine(sArguments22);


           // MessageBox.Show("特征提取完毕");
            //p.StandardInput.WriteLine(sArguments3);
            //MessageBox.Show("训练数据完毕");
            //p.StandardInput.WriteLine(sArguments4);
            //MessageBox.Show("结果分析完毕");
            p.Close();
                       
        }

        public void GetTrainProcess()
        {
            
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";//调用dos命令
            string sArguments0 = "cd " + Global.GetProjectDirectory();
            string sArguments3 = "easy.py train test";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine(sArguments0);
            p.StandardInput.WriteLine(sArguments3);
           
            p.Close();
            //MessageBox.Show("训练数据完毕");

        }
        public void GetResultProcess()
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";//调用dos命令
            string sArguments0 = "cd " + Global.GetProjectDirectory();
            string sArguments4 = "result+.py";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine(sArguments0);
            p.StandardInput.WriteLine(sArguments4);
            
            //MessageBox.Show("结果分析完毕");
            p.Close();

        }
    }
}
