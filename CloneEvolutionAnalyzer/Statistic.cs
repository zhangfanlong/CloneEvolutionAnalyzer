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
    public class Statistic
    {
        
        //处理gdf文件时先去除第一行，处理中间文件"1"时直接存储信息
        //功能：统计聚类结果中，同一类里各个度量值为1时出现的次数,其他条件需要自行设置,更改判断条件和数据类型即可
        public string SrcPath;
        public int group_sum = 0;
        public int k1_num = 0;
        public int k2_num = 0;
        public int k3_num = 0;       
        public int k4_num = 0;
        public int k5_num = 0;
        public int k6_num = 0;
        public int k7_num = 0;
        public int k8_num = 0;
        public int k9_num = 0;
        public int k10_num = 0;

        public void result()
        {
            OpenFileDialog Op = new OpenFileDialog();            
            if (Op.ShowDialog() == DialogResult.OK)
            {
                SrcPath = Op.FileName;
            }
            else return;
            string[] data = File.ReadAllLines(SrcPath);
            string group = "group1";
            string groupnum;
            StreamWriter sw = new StreamWriter(@"E:\\statistic", false);
            for (int i = 0; i < data.Length; i++)
            {
                int c = 0;
                int j = 0;
                while (j < data[i].Length)
                {
                    if ((c < 7) && (data[i][j] == ','))
                    {
                        c++; 
                        j++;
                        if (c == 3)
                        {
                            int t = j;
                            while (t < data[i].Length)
                            {
                                if (data[i][t] == ',')
                                {
                                    groupnum = data[i].Substring(j, t - j);
                                    if (groupnum != group)
                                    {
                                        sw.Write(group + ' ' + group_sum + ' ' + k1_num + ' ' + k2_num + ' ' + k3_num + ' ' + k4_num + ' ' + k5_num + ' ' + k6_num + ' ' + k7_num + ' ' + k8_num + ' ' + k9_num + ' ' + k10_num);
                                        sw.Write("\n");
                                        group = groupnum;
                                        group_sum = 0;
                                        k1_num = 0;
                                        k2_num = 0;
                                        k3_num = 0;       
                                        k4_num = 0;
                                        k5_num = 0;
                                        k6_num = 0;
                                        k7_num = 0;
                                        k8_num = 0;
                                        k9_num = 0;
                                        k10_num = 0;
                                    }
                                    group_sum++;
                                    break;
                                }
                                t++; 
                            }
                        }
                        continue; 
                    }
                    else if (c < 7)
                    {
                        j++;
                        continue;
                    }
                    else
                    {
                        int t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k1 = int.Parse(data[i].Substring(j, t - j));//数据类型
                                if (k1 == 1)//判断条件
                                    k1_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k2 = int.Parse(data[i].Substring(j, t - j));
                                if (k2 == 1)//判断条件
                                    k2_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                double k3 = double.Parse(data[i].Substring(j, t - j));
                                if (k3 == 1.0)
                                    k3_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k4 = int.Parse(data[i].Substring(j, t - j));
                                if (k4 == 1)
                                    k4_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k5 = int.Parse(data[i].Substring(j, t - j));
                                if (k5 == 1)
                                    k5_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k6 = int.Parse(data[i].Substring(j, t - j));
                                if (k6 == 1)
                                    k6_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k7 = int.Parse(data[i].Substring(j, t - j));
                                if (k7 == 1)
                                    k7_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k8 = int.Parse(data[i].Substring(j, t - j));
                                if (k8 == 1)
                                    k8_num++;
                                break;
                            }
                            t++;
                        }
                        j = t + 1; t = j + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k9 = int.Parse(data[i].Substring(j, t - j));
                                if (k9 == 1)
                                    k9_num++;
                                break;
                            }
                            t++;
                        }
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ',')
                            {
                                int k10 = int.Parse(data[i].Substring(j, t - j));
                                if (k10 == 1)
                                    k10_num++;
                                break;
                            }
                            t++;
                        }
                       
                    }
                }
                    

            }
            sw.Write(group + ' ' + group_sum + ' ' + k1_num + ' ' + k2_num + ' ' + k3_num + ' ' + k4_num + ' ' + k5_num + ' ' + k6_num + ' ' + k7_num + ' ' + k8_num + ' ' + k9_num + ' ' + k10_num);
            sw.Write("\n");
            sw.Close();

            MessageBox.Show("Done");
            
        }
    }
}
