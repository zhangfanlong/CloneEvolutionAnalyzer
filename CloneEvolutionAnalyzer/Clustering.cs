﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Text;

//添加层次聚类来确定聚类个数
namespace CloneEvolutionAnalyzer
{
    class Cluster//层次聚类计算相似度、存储类别
    {
        /// <summary>
        /// 存放代码片段
        /// </summary>
        private List<List<string>> _lsCodes = new List<List<string>>();
        public int metricno = 16;
        public List<List<string>> Codes
        {
            get
            {
                return _lsCodes;
            }
        }
        /// <summary>
        /// 计算两个簇之间的相似度,采用Group Average
        /// </summary>
        /// <param name="cluOther"></param>
        /// <returns></returns>
        public double Simalarty(Cluster cluOther)
        {
            double groupAve = 0;
            for (int i = 0; i < _lsCodes.Count; i++)
            {
                for (int j = 0; j < cluOther._lsCodes.Count; j++)
                {
                    double tmpDou = 0;

                    List<double> v1 = new List<double>();
                    List<double> v2 = new List<double>();           
                    for (int t = 0; t < metricno; t++)
                    {
                        v1.Add(double.Parse(_lsCodes[i][t+1]));
                        v2.Add(double.Parse(cluOther._lsCodes[j][t+1]));
                    } 
                    Clustering dis = new Clustering();
                    tmpDou += dis.distance(v1, v2, metricno);                  

                    groupAve += tmpDou;

                }
            }
            groupAve = groupAve / (_lsCodes.Count * cluOther.Codes.Count);
            return groupAve;
        }
    }
    public class Clustering
    {
        public int mnum = 29;//所有度量维数，对应xtemp_matrix
        public int metricno = 16;//实际选择的度量维数，对应x_matrix
        public int cnum;//聚类个数，从层次聚类获取传递给FCM
        public int m = 2;
        public double objval = 0.0;
        public List<List<string>> xtemp_matrix = new List<List<string>>();//（含29维度量的样本）
        public List<List<string>> x_matrix = new List<List<string>>();//样本矩阵，行表示样本，列表示度量(筛选度量后的样本)
        public List<XmlElement> CFinfo = new List<XmlElement>();//存放x _matrix第0列标签所对应的代码片段的CRD-Xml信息，标签相差1，从0开始有效
        public List<List<double>> utemp_matrix = new List<List<double>>();
        public List<List<double>> u_matrix = new List<List<double>>();
        public List<List<double>> center = new List<List<double>>();
        public List<List<XmlElement>> result = new List<List<XmlElement>>();
        public double ThresholdValue = 200;// 聚类的阈值 

        public void FCM(int fcmnum)
        {
           // CreatMatrix();
            cnum = fcmnum;
            Init();//随机初始化隶属度矩阵
            ComputeCenter(); //计算聚类中心
            objval = objectfun(u_matrix, center, x_matrix, cnum, (x_matrix.Count - 1), metricno, m);//计算优化的目标函数
            //迭代重置隶属矩阵与聚类中心
            int n_maxcycle=1000;//最大的跌代次数
            float d_threshold = (float) 0.1;//误差限
            int flagtemp;
            int count;
            int n_cycle;
            double lastv,delta;
            double min_dis=0.001;//距离的最小值
            double temp;
            List<double> v1 = new List<double>();
            List<double> v2 = new List<double>();
            List<double> v3 = new List<double>();
            for (int i = 0; i < metricno; i++)
            {
                v1.Add(0.0);
                v2.Add(0.0);
                v3.Add(0.0);
            }
            n_cycle = 0;
            lastv = 0;
            do
            {
                for (int i = 0;i < (x_matrix.Count - 1);i++)
                {
                    flagtemp = 0;
                    count = 0;
                    for (int j = 0; j < cnum; j++)
                    {
                        temp = 0.0;
                        for (int t = 0; t < cnum; t++)
                        {
                            for (int k = 0; k < metricno; k++)
                            {
                                v1[k]=double.Parse(x_matrix[i+1][k+1]);
                                v2[k]=center[t][k];
                            }
                            if(distance (v1,v2,metricno) > min_dis)
                                temp += System .Math.Pow(distance(v1,v2,metricno),-2/(m-1));
                            else
                                flagtemp = 1;
                        }
                        for (int k = 0; k < metricno; k++)
                        {
                            v1[k]=double.Parse(x_matrix[i+1][k+1]);
                            v2[k]=center[j][k];
                        }
                        if(flagtemp == 1)
                        {
                            u_matrix[j][i] = 0;
                            flagtemp = 0;
                        }
                        //如果存在使得||Xi-Cj||=0的点置标志-1
                        if (distance(v1,v2,metricno) > min_dis)
                        {                        
                             double value_ji = System.Math.Pow(distance(v1,v2,metricno),-2/(m-1))/temp;
                             u_matrix[j][i] = value_ji;
                        }
                        else
                        {
                            count++;
                            u_matrix[j][i]=-1;
                        }
                    }//end for j
                    // 如果存在使得||Xi-Cj||=0就让所有的与Xi不为零的类的隶属度为零。
                     if (count > 0)
                    {
                        for (int j = 0; j < cnum; j++)
                        {
                            if (u_matrix[j][i] == -1)
                            {
                                u_matrix[j][i] = 1/(double)count;
                            }
                            else
                            {
                                u_matrix[j][i]=0;
                            }
                        }
                    }
                }//end for i

                temp=objectfun(u_matrix,center,x_matrix,cnum,(x_matrix.Count - 1),metricno,m);
                delta=System.Math.Abs(temp-lastv);
                lastv=temp;

                //计算聚类中心的坐标
                for (int i = 0; i < cnum; i++)
                {
                    for (int j = 0; j < metricno; j++)
                    {
                        temp=0;
                        for (int k = 0;k < (x_matrix.Count - 1); k++)
                        {
                            temp += System.Math.Pow(u_matrix[i][k],m)*double.Parse(x_matrix[k+1][j+1]);
                        }
                        center[i][j]=temp;
                        temp=0;
                        for (int k = 0; k < (x_matrix.Count - 1); k++)
                        {
                            temp += System.Math.Pow(u_matrix[i][k],m);
                        }
                        center[i][j]/=temp;
                    }
                }
                n_cycle++;            

            }while((n_cycle < n_maxcycle) && (delta > d_threshold));
            
           
            //根据最大隶属度原则将样本分类
            for (int i = 0; i < cnum; i++)
            {
                List<XmlElement> newlist = new List<XmlElement>();                                           
                    
                result.Add(newlist);               
            }
            double max = 0.0;
            int maxnum = 0;
            for (int i = 0; i < (x_matrix.Count - 1); i++)
            {
                max = 0.0;              
                maxnum = 0;               
                for (int j = 0; j < cnum; j++)//找到隶属度最大的类别
                {
                    if (u_matrix[j][i] > max)
                    {
                        max = u_matrix[j][i];
                        maxnum = j;
                    }
                }
                int resultnum = i + 1;
                result[maxnum].Add(CFinfo[i]);
            }
            //~~~~~~~~~~~~~~for test~~~~~~~~~~~~~~~~~~~~~~~~~~~
            StreamWriter sw = new StreamWriter(@"E:\\1", false);
            List<String> genList = new List<String>();//存储克隆家系文件名
            DirectoryInfo dir = new DirectoryInfo(Global.mainForm._folderPath + @"\GenealogyFiles\blocks");
            FileInfo[] genFiles = dir.GetFiles();
            foreach (FileInfo info in genFiles)
            {
                if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    continue;
                else
                    genList.Add(info.Name);
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~视图显示聚类结果~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            Global.mainForm.tabControl1.TabPages.Add(" ");//创建新的tabPage，显示文件名
            Global.mainForm.tabControl1.SelectedTab = Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1];
            //创建TreeView控件
            TreeView newTreeView = new TreeView();
            Global.mainForm.tabControl1.TabPages[Global.mainForm.tabControl1.TabPages.Count - 1].Controls.Add(newTreeView);
            newTreeView.Dock = DockStyle.Fill;
            newTreeView.Nodes.Clear();
            TreeNode treeRoot = new TreeNode("ShowClusteringResult");
            newTreeView.Nodes.Add(treeRoot);

            for (int i = 0; i < cnum; i++)
            {
                TreeNode treeNode = new TreeNode("group" + (i + 1).ToString());             
                for (int j = 0; j < result[i].Count; j++)
                {
                    
                    sw.Write('v' + result[i][j].ToString());
                    sw.Write(",");
                    sw.Write("group" + (i+1).ToString());
                    sw.Write(",");
                    string genid = null;
                    int k = 0;
                    for (k = 0; k < genList.Count; k++)
                    {
                        XmlDocument gen_Xml = new XmlDocument();
                        gen_Xml.Load(Global.mainForm._folderPath + @"\GenealogyFiles\blocks\" + genList[k]);
                        string sourcefile = result[i][j].Attributes[0].Value;
                        string file = sourcefile.Substring(0, sourcefile.IndexOf(@"/"));
                        string classid = result[i][j].ParentNode.Attributes[0].Value.ToString();
                        if (genList[k] != "SingleCgGenealogies.xml")
                        {
                            int round = 0;
                            foreach (XmlElement evoNode in gen_Xml.DocumentElement.SelectNodes("Evolution"))
                            {
                                round++;
                                
                                if (round == 1)
                                {
                                    string filename = ((XmlElement)(evoNode.SelectSingleNode("srcInfo"))).GetAttribute("filename");
                                    string cgid = ((XmlElement)(evoNode.SelectSingleNode("srcInfo"))).GetAttribute("cgid");
                                    if ((file == filename) && (classid == cgid))
                                    {
                                        genid = ((XmlElement)(gen_Xml.DocumentElement.SelectSingleNode("GenealogyInfo"))).GetAttribute("id");
                                        k = genList.Count;
                                        break;
                                    }
                                }
                                string filename0 = ((XmlElement)(evoNode.SelectSingleNode("destInfo"))).GetAttribute("filename");
                                string cgid0 = ((XmlElement)(evoNode.SelectSingleNode("destInfo"))).GetAttribute("cgid");
                                if ((file == filename0) && (classid == cgid0))
                                {
                                    genid = ((XmlElement)(gen_Xml.DocumentElement.SelectSingleNode("GenealogyInfo"))).GetAttribute("id");
                                    k = genList.Count;
                                    break;

                                }

                            }
                            
                        }
                        else
                        {
                            int round = 0;
                            foreach (XmlElement evoNode in gen_Xml.DocumentElement.SelectNodes("SingleCgGenealogy"))
                            {
                                round++;
                                string filename = ((XmlElement)(evoNode)).GetAttribute("version");
                                string cgid = ((XmlElement)(evoNode)).GetAttribute("cgid");

                                if ((file == filename) && (classid == cgid))
                                {
                                    genid = round.ToString() + 's';
                                    k = genList.Count;
                                    break;
                                }
                            }
                        }
                        
                    }
                    //sw.Write("class" + CFinfo[result[i][j]-1].ParentNode.Attributes[0].Value.ToString());
                    sw.Write("genid" + genid);
                    sw.Write(",");
                    //CloneGenealogyViz Cgv = new CloneGenealogyViz();
                    //foreach (CGNode cgNode in Cgv.NodeList)
                    //{
                    //    string filename = cgNode.Info.version + "_blocks-clones-0.3-classes-withCRD.xml";
                        
                    //    int cfNum = cgNode.cfNum;
                    //    for (int ii = 0; ii < cfNum; ii++)
                    //    {


                    //    }
                    //}

                    string cgnum = result[i][j].ParentNode.Attributes[0].Value.ToString();
                    sw.Write(cgnum);
                    sw.Write(",");
                    string versionname =  result[i][j].Attributes[0].Value.Substring(0, (result[i][j].Attributes[0].Value.IndexOf(@"/")));
                    sw.Write(versionname);
                    sw.Write("\n");
                    
                    //string attr = versionname + ", genid=" + genid + ", cgid=" + cgnum + ", cfid=" + result[i][j].ToString();
                    //TreeNode attr1 = new TreeNode(attr);
 
                }
               
                ShowXml(result[i], ref treeNode);
                treeNode.Expand();
                newTreeView.Nodes[0].Nodes.Add(treeNode);
                newTreeView.Nodes[0].Expand();
            }
            sw.Close();

            MessageBox.Show("Done");
            
            newTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(Global.mainForm.newTreeView_NodeMouseClick);
            //添加预定义的上下文菜单
            newTreeView.ContextMenuStrip = Global.mainForm.contextMenuStrip1;
            newTreeView.ExpandAll();

        }


        public int Hcluster()//层次聚类 确定分类个数
        {
            CreatMatrix();
            SelectMatrix();
            // 开始对代码片段进行分类,下面链表当中每个list是一个类别
            List<Cluster> codeCluster = new List<Cluster>();
            int i = 0;
            for (; i < (x_matrix.Count - 1); i++) // 初试话每个函数为一个类
            {
                Cluster tmpCluster = new Cluster();
                tmpCluster.Codes.Add(x_matrix[i+1]);
                codeCluster.Add(tmpCluster);
            }
            List<List<double>> simMetric = new List<List<double>>();// 相似度矩阵
            ///初试化相似度矩阵
            int j;
            for (i = 0; i < codeCluster.Count; i++)
            {
                List<double> lsRow = new List<double>();
                for (j = 0; j < codeCluster.Count; j++)
                {
                    if (i != j)
                    {
                        lsRow.Add(codeCluster[i].Simalarty(codeCluster[j])); // 计算每个簇之间的相似度
                    }
                    else
                    {
                        lsRow.Add(double.MaxValue);  // 行列相等，对角线上 相似度设置为最大
                    }
                }
                simMetric.Add(lsRow);
            }

            while (true)// 在达到阈值之间不断循环
            {
                double min = double.MaxValue;
                int indexA = -1; // 记录最小的两个簇的索引
                int indexB = -1;
                for(i=0;i<simMetric.Count;i++)
                    for (j = 0; j < simMetric.Count; j++)
                    {
                        if (simMetric[i][j] < min)
                        {
                            indexA = i;
                            indexB = j;
                            min = simMetric[i][j];
                        }
                    }

                //更新将两个簇合并为一个簇，簇的个数通过合并被更新；
               //同时更新相似矩阵，将两个簇的两行（两列）距离用1行（1列）距离替换反映合并操作。
                if (indexA == indexB)
                    break;
                if (min > ThresholdValue)
                {
                    break;
                }
                if (indexA > indexB) // 是indexA为最小值
                {
                    int tmpInt = indexA;
                    indexA = indexB;
                    indexB = tmpInt;
                }

                codeCluster[indexA].Codes.AddRange(codeCluster[indexB].Codes); // 把indexB对应的簇添加的indexA对应的簇当中
                codeCluster.RemoveAt(indexB);  // 删除indexB对应的簇
                //更新相似度矩阵
                simMetric.RemoveAt(indexB);// 删除行
                // 删除列
                for (i = 0; i < simMetric.Count; i++)
                {
                    simMetric[i].RemoveAt(indexB);
                }
                //从新计算indexA行的相似度
                for (j = 0; j < simMetric[indexA].Count; j++)
                {
                    if (j != indexA)
                    {
                        simMetric[indexA][j] = codeCluster[indexA].Simalarty(codeCluster[j]);
                    }
                    else
                    {
                        simMetric[indexA][j] = double.MaxValue;
                    }
                    
                }
                //重新计算indexA列的相似矩阵
                for (i = 0; i < simMetric.Count; i++)
                {
                    if (i != indexA)
                    {
                        simMetric[i][indexA] = codeCluster[i].Simalarty(codeCluster[indexA]);
                    }
                    else
                    {
                        simMetric[i][indexA] = double.MaxValue;
                    }
                }

            }
            MessageBox.Show("The cluster num is " + codeCluster.Count.ToString());
            return codeCluster.Count;      
           
        }



        public void ShowXml(List<XmlElement> xmlNode, ref TreeNode treeRoot)
        {            
            //由根节点的孩子节点名称，判断要显示的文件内容
            for (int i = 0; i < xmlNode.Count; i++)
            {
                if (xmlNode[i].Name == "source")
                {
                    TreeNode tNode = new TreeNode(((XmlNode)xmlNode[i]).OuterXml.Substring(0, ((XmlNode)xmlNode[i]).OuterXml.IndexOf(">")));
                    tNode.Name = xmlNode[i].Name;    //保存节点名称（注：名称与Text不同）                        

                    CloneSourceInfo info = new CloneSourceInfo();
                    info.sourcePath = xmlNode[i].Attributes[0].Value;
                    Int32.TryParse(xmlNode[i].Attributes[1].Value, out info.startLine);
                    Int32.TryParse(xmlNode[i].Attributes[2].Value, out info.endLine);
                    tNode.Tag = info;

                    treeRoot.Nodes.Add(tNode);  //添加新的树节点并获取其索引号
                }
            }
        }

        //从提取的代码片段度量信息中提取样本矩阵(0行0列都是标签)~~构建xtemp_matrix
        public void CreatMatrix()
        {
            string []data = File.ReadAllLines(Global.GetProjectDirectory() + @"\\matrix");
            int num = 1;
            string crdpath = null;
            string classid = null;
            int sourceid = 0;
            //第0行存储度量标签0~29
            List<string> list = new List<string>();
            for(int i = 0; i <= mnum; i++)
            {
                list.Add(Convert.ToString(i));
            }
            xtemp_matrix.Add(list);
            for (int i = 0; i < data.Length; i++)
            {
                //提取每一行有意义的度量信息，加入x_matrix
                List<string> newlist = new List<string>();              
                newlist.Add(Convert.ToString(num++));   
                int k = 0;
                while (k < data[i].Length)
                {
                    if (k == 3)//获取代码片段相关信息以此定位确定xml文件
                    {
                        int t = k + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ' ')
                            {
                                crdpath = data[i].Substring(k, t - k);                                
                                break;
                            }
                            t++;
                        }
                        k = t + 1; t = k + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ' ')
                            {
                                classid = data[i].Substring(k, t - k);
                                break;
                            }
                            t++;
                        }
                        k = t + 1; t = k + 1;
                        while (t < data[i].Length)
                        {
                            if (data[i][t] == ' ')
                            {
                                sourceid = int.Parse(data[i].Substring(k, t - k));
                                break;
                            }
                            t++;
                        }
                        k = t + 1; 
                    }
                    if (data[i][k] == ':')//获取属性信息
                    {
                        int t = k + 1;
                        while ( t < data[i].Length)
                        {
                            if (data[i][t] == ' ')
                            {
                                string item = data[i].Substring(k + 1, t - k - 1);
                                newlist.Add(item);
                                break;
                            }
                            else if (t == (data[i].Length - 1))
                            {
                                string item = data[i].Substring(k + 1, t - k);
                                newlist.Add(item);
                                break;
                            }
                            t++;
                        }
                        k = t + 1;
                    }
                    else
                        k++;
                }
                xtemp_matrix.Add(newlist);
                XmlDocument xdoc = new XmlDocument();  
                XmlElement CFcrd = null;
                xdoc.Load(crdpath);
                foreach (XmlElement classnode in  xdoc.DocumentElement.SelectNodes("class"))           
                {
                    if(classnode.GetAttribute("classid") == classid)
                    {
                        CFcrd = classnode;
                        break;
                    }

                }
                CFinfo.Add((XmlElement)CFcrd.ChildNodes[sourceid - 1]);

            }          
        }
        //筛选度量，构建x_matrix
        public void SelectMatrix()
        {
            List<string> list = new List<string>();
            for (int i = 0; i <= metricno; i++)
            {
                list.Add(Convert.ToString(i));
            }
            x_matrix.Add(list);
            int num = 1;
            for (int i = 0; i < (xtemp_matrix.Count - 1); i++)
            {               
                List<string> newlist = new List<string>();
                newlist.Add(Convert.ToString(num++));
                newlist.Add(xtemp_matrix[i][1]);
                newlist.Add(xtemp_matrix[i][7]);
                newlist.Add(xtemp_matrix[i][8]);
                newlist.Add(xtemp_matrix[i][9]);
                newlist.Add(xtemp_matrix[i][10]);
                newlist.Add(xtemp_matrix[i][11]);
                newlist.Add(xtemp_matrix[i][12]);
                newlist.Add(xtemp_matrix[i][13]);
                newlist.Add(xtemp_matrix[i][14]);
                newlist.Add(xtemp_matrix[i][23]);
                newlist.Add(xtemp_matrix[i][24]);
                newlist.Add(xtemp_matrix[i][25]);
                newlist.Add(xtemp_matrix[i][26]);
                newlist.Add(xtemp_matrix[i][27]);
                newlist.Add(xtemp_matrix[i][28]);
                newlist.Add(xtemp_matrix[i][29]);
                x_matrix.Add(newlist);
            }

        }

        //随机初始化隶属度矩阵
        public void Init()
        {
            //创建u_matrix
            for (int i = 0; i < cnum; i++)
            {
                List<double> newlist = new List<double>();
                for (int j = 0; j < (x_matrix.Count-1); j++)                                   
                    newlist.Add(0.0);              
                u_matrix.Add(newlist);
                utemp_matrix.Add(newlist);
            }
            //随机初始化
            Random ra = new Random();
            for (int i = 0; i < cnum; i++ )
            {
                for (int j = 0; j < (x_matrix.Count - 1); j++)
                {
                    utemp_matrix[i][j] = ra.NextDouble();
                }
            }
            //归一化处理（每列之和为1）            
            List<double> sumj = new List<double>();//计算各列之和
            for (int j = 0; j < (x_matrix.Count - 1); j++)
            {
                double sj = 0;
                for (int i = 0; i < cnum; i++)
                {
                    sj = sj + utemp_matrix[i][j];                    
                }
                sumj.Add(sj);
            }
            for (int i = 0; i < cnum; i++)//归一化
            {
                for (int j = 0; j < (x_matrix.Count - 1); j++)
                {
                    u_matrix[i][j] = utemp_matrix[i][j] / sumj[j];
                }
            }        
        }

        //计算聚类中心
        public void ComputeCenter()
        {
            //创建center矩阵 行表示类别 列表示度量属性
            for (int i = 0; i < cnum; i++)
            {
                List<double> newlist = new List<double>();
                for (int k = 0; k < metricno; k++)
                    newlist.Add(0.0);
                center.Add(newlist);               
            }
            //计算聚类中心
            for (int i = 0; i < cnum; i++)
            {               
                for (int j = 0; j < metricno; j++)
                {
                    double temp = 0.0;
                    for (int k = 0; k < (x_matrix.Count - 1); k++)
                        temp += System.Math.Pow(u_matrix[i][j], m) * double.Parse(x_matrix[k + 1][j + 1]);
                    center[i][j] = temp;
                    temp = 0.0;
                    for (int t = 0; t < (x_matrix.Count - 1); t++)                    
                        temp += System.Math.Pow(u_matrix[i][t], m);
                    center[i][j] /= temp;                    
                }
            }
        }

        //计算优化的目标函数
        double objectfun(List<List<double>> u_matrix, List<List<double>>center, List<List<string>>x_matrix, int cnum, int n, int metricno, int m)
        {
            List<double> v1 = new List<double>();
            List<double> v2 = new List<double>();
            for (int i = 0; i < n; i++)
            {
                v1.Add(0.0);
                v2.Add(0.0);
            }
            double value = 0.0;
            for (int i = 0; i < cnum; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < metricno; k++)
                    {
                        v1[k] = center[i][k];
                        v2[k] = double.Parse(x_matrix[j+1][k+1]);
                    }
                    value += System.Math.Pow(u_matrix[i][j], m) * distance(v1,v2,metricno);
                }
            }
            return value;
        }

        //计算距离函数：暂时采用欧式距离
        public double distance(List<double> v1, List<double> v2, int metricno)
        {          
            double result = 0.0;
            for (int i = 0; i < metricno; i++)
            {
                result += (v1[i] - v2[i]) * (v1[i] - v2[i]);
            }
            result = System.Math.Sqrt(result);
            return result;
        }

        //分析用~~~~~~
        public void adjustxy(GenealogyAllViz v)
        {
            
            List<Node> cglist = new List<Node>();
            for (int i = 0; i < v.nodeList.Count; i++)
            {
                cglist.Add(v.NodeList[i]);
            }
             StreamWriter sw = new StreamWriter(@"E:\\4", false);
             for (int i = 0; i < cglist.Count; i++)
             {

                 sw.Write(cglist[i].center.X);

                 sw.Write(",");
                 sw.Write(cglist[i].center.Y);
                 sw.Write("\n");
             }
             sw.Close();
        }

    }
}
