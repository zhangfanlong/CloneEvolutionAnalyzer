using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;   //使用绘图功能
using System.Windows.Forms;
using System.IO;

namespace CloneEvolutionAnalyzer
{
    public class Node
    {
        public static int width;//节点宽
        public static int height;//节点高
        public static int RowY;//标记最后一行节点纵坐标
        public static int RowX;//标记最后一列节点横坐标
        public static int NodeCenterDis;//节点中心垂直距离
        public static int RowDis;//每行距离
        public static void SetBase()
        {
            width = 10;
            height = 10;
            RowX = 0;
            RowY = 0;
            NodeCenterDis = 50;
            RowDis = 25;
        }
        public Point center;
        public Rectangle rect;
        public int id;
        public string GenFile;

        public Node() { }
        public Node(Node node)
        {
            this.center = new Point(node.center.X, node.center.Y);
            this.rect = new Rectangle(node.rect.X, node.rect.Y, node.rect.Width, node.rect.Height);
            this.id = node.id;
            this.GenFile = node.GenFile;
        }
        public int ID()
        {
            return id;
        }
        public string FileName()
        {
            return GenFile;
        }
        public void Set(string GenFileName, XmlElement curEle, Node preNode, Node baseNode, int roundNum, int top, int centX)
        {
            #region 设置位置

            id = Int32.Parse(curEle.GetAttribute("id"));
            GenFile = GenFileName;
            if (preNode == null && roundNum == 1)   //如果是第一个节点
            {
                //center = baseCenter;  
                center.X = centX;
                center.Y = top;
            }
            else if (preNode == null)
            {
                center.X = baseNode.center.X + NodeCenterDis;   //横向右移
                center.Y = baseNode.center.Y + top;  //每增加一轮，纵向下移
            }
            else
            {
                center.X = preNode.center.X + NodeCenterDis;   //横向右移
                center.Y = preNode.center.Y + top;  //每增加一轮，纵向下移
            }

            rect = new Rectangle(center.X - (int)(0.5 * width), center.Y - (int)(0.5 * width), width, height);
            #endregion
        }

        public void Draw1(GenealogyAllViz cgv)//一致变化时
        {
            cgv.Gra.DrawRectangle(GenealogyAllViz.pen1, this.rect);
            cgv.Gra.FillRectangle(GenealogyAllViz.brush1, this.rect);
        }
        public void Draw2(GenealogyAllViz cgv)//不一致变化时
        {
            cgv.Gra.DrawRectangle(GenealogyAllViz.pen5, this.rect);
            cgv.Gra.FillRectangle(GenealogyAllViz.brush1, this.rect);
        }

    }
    /// <summary>
    /// 定义接口，封装与可视化相关的概念及功能
    /// </summary>
    public interface ALLVisual
    {
        Graphics Gra { get; set; }
       // XmlDocument GenealogyXml { get; }
        List<Node> NodeList { get; }
        void Init();
       // void SetGenealogyXml(XmlDocument genealgoyXml);
        void SetScaling(float x, float y);
        void DrawAllGraphics();
    }
    /// <summary>
    /// CloneGenealogyViz类继承自Panel类及ALLVisual接口。其中Panel类对应绘图功能，并可在事件响应中作为参数传递
    /// IVisual类实现与可视化相关的操作
    /// </summary>
    public class GenealogyAllViz : Panel, ALLVisual
    {
        public static Pen pen1; //用于画Node边缘的画笔(一致变化的)
        public static Pen pen2; //用户画连接线的画笔（无变化）
        public static Pen pen3; //用户画连接线的画笔（有变化）
        public static Pen pen4; //用于画版本号和家系标号
        public static Pen pen5; //用于画Node边缘的画笔（发生不一致变化的）
        public static SolidBrush brush1;    //用于填充Node区域的画刷
        public float X = 1.0f;//用于缩放功能
        public float Y = 1.0f;//用于缩放功能
        public static int tagSet;    //标签集（数字）：表示克隆群个数
        public static int vernum;   //版本号
        //对接口中的属性成员进行定义
        private Graphics gra;
        public Graphics Gra { get { return gra; } set { gra = value; } }
        private XmlDocument genealogyXml;
        public XmlDocument GenealogyXml { get { return genealogyXml; } }
        public List<Node> nodeList;//记录所有节点信息
        public List<Node> NodeList { get { return nodeList; } } 
      
        public new event PaintEventHandler Paint;   //重新定义Paint事件
        public delegate void PaintEventHandler(object sender, PaintEventArgs e);
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.gra = e.Graphics;
            this.AutoScroll = true;           
            gra.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
            gra.ScaleTransform(X, Y);//缩放功能
            DrawAllGraphics();//绘图
            this.AutoScrollMinSize = new Size((int)(Node.RowX * X), (int)(Node.RowY * Y)); //自动添加滚动条
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            //Global.mainForm.gav_MouseMove(this, e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Global.mainForm.gav_MouseClick(this, e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Global.mainForm.gav_MouseDoubleClick(this, e);
        }

        //缩放功能设置缩放大小
        public void SetScaling(float x, float y)
        {
            X = x;
            Y = y;
        }
        public void Init()
        {            
            //初始化画笔画刷
            pen1 = new Pen(Color.LightSkyBlue, 2);
            pen2 = new Pen(Color.LightSkyBlue, 2);
            pen3 = new Pen(Color.LightSalmon, 2);
            pen4 = new Pen(Color.LightSteelBlue,2);
            pen5 = new Pen(Color.HotPink, 2);
            brush1 = new SolidBrush(Color.CornflowerBlue);
          
            //初始化画布
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Visible = true;
        }

        public void SetGenealogyXml(XmlDocument geneXml)
        {
            genealogyXml = geneXml;
        }

        public void DrawAllGraphics()
        {            
            string[] fileList = System.IO.Directory.GetFileSystemEntries(Global.mainForm._folderPath + @"\blocks\");
            int fileNum = 0;//记录文件数，即进化版本个数
            int num = 1;
            int flag = 0;
            int y = 0;
            foreach (string file in fileList)
            {
                fileNum++;
            }         
            for (int count = 0; count < fileNum; count++)//描绘版本号信息
            {
                this.gra.DrawString(Convert.ToString(count + 1), new Font("Verdana", 10), new SolidBrush(Color.DimGray), 73 + count * 50, 40);
            }
            Node.SetBase();
            Node.RowX = 73 + (fileNum-1) * 50 + 50;//横坐标边界值设定
            //初始化nodeList
            this.nodeList = new List<Node>();
            List<String> genList = new List<String>();//存储克隆家系文件名
            List<String> Adjust_genList = new List<String>(); //按照标号顺序调整后的克隆家系文件名
            DirectoryInfo dir = new DirectoryInfo(Global.mainForm._folderPath + @"\GenealogyFiles\blocks");
            FileInfo[] genFiles = dir.GetFiles();
            foreach (FileInfo info in genFiles)
            {
                if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    continue;
                else                                 
                    genList.Add(info.Name);
            }

            #region 调整文件名顺序
            //按照标号顺序调整后的克隆家系文件名，不处理只含单个节点的克隆家系
            while (genList.Count != 0)
            {
                flag = 0;
                foreach (string genXml in genList)
                {
                    if (genXml.Contains("Genealogy-" + Convert.ToString(num) + "_"))
                    {
                        flag = 1;                        
                        genList.Remove(genXml);
                        Adjust_genList.Add(genXml);
                        num++;
                        break;
                    }
                }
                if (flag == 0)
                    break;
            }
            num = 0;
            #endregion

            //对于每个克隆家系文件，执行DrawGenGraphics
            for (int i = 0; i < Adjust_genList.Count; i++ )
            {
                num++;
                XmlDocument gen_Xml = new XmlDocument();
                gen_Xml.Load(Global.mainForm._folderPath + @"\GenealogyFiles\blocks\" + Adjust_genList[i]);
                this.SetGenealogyXml(gen_Xml);
                if (genealogyXml.DocumentElement.Name == "CloneGenealogy")
                {                    
                    string startversion = ((XmlElement)(GenealogyXml.DocumentElement.SelectSingleNode("GenealogyInfo"))).GetAttribute("startversion");
                    string GenFileName = Adjust_genList[i];
                    int j;
                    for(j = 0; j < fileList.Length; j++ )//确定初始节点位置
                    {
                        if (fileList[j].Contains(startversion))
                            break;
                    }
                    if (num == 1)
                    {
                        this.gra.DrawString(Convert.ToString(num), new Font("Verdana", 10), new SolidBrush(Color.DimGray), 35, 73);
                        y = DrawGenGraphics(80 + j * 50, 80, GenFileName);
                    }
                    else
                    {
                        this.gra.DrawString(Convert.ToString(num), new Font("Verdana", 10), new SolidBrush(Color.DimGray), 35, y-7);
                        y = DrawGenGraphics(80 + j * 50, y, GenFileName);
                    }

                }
            }
            Node.RowY = y + 50; //设定纵坐标边界        
        }

        public int DrawGenGraphics(int x, int y, string GenFileName)//x为首节点横坐标，y为首节点纵坐标
        {          
            List<XmlElement> evoList = new List<XmlElement>();  //存放所有元素的列表
            foreach (XmlElement evoNode in GenealogyXml.DocumentElement.SelectNodes("Evolution"))
            {
                evoList.Add(evoNode);
            }           
            List<XmlElement> leftEvoList = new List<XmlElement>(evoList);  //存放未处理元素的列表
            List<int> visit = new List<int>();//标记是否访问过
            Stack<Node> s = new Stack<Node>();//深度优先遍历使用的栈
            Node srcNode = new Node();
            Node preNode = new Node();
            Node baseNode = new Node();//基节点
            XmlElement curNode;
            MAPLine evoLine = new MAPLine();
            int flag = 0;
            int rowheight = 0;
            int roundNum = 0;
            int top = 0;//记录已描绘节点的高度
            for (int i = 0; i <= evoList.Count;i++ )
            {
                visit.Add(0);
            }
            while (leftEvoList.Count != 0)
            {
                roundNum++;
                curNode = leftEvoList[0];
                if (roundNum == 1)//描绘基节点
                {
                    srcNode.Set(GenFileName, curNode, null, baseNode, 1, y, x);
                    srcNode.Draw1(this);                    
                    baseNode = new Node(srcNode);
                    this.nodeList.Add(baseNode);
                }
                else
                {
                    //判断节点是否为克隆直系的首节点
                    if (curNode.GetAttribute("parentID") == "null")
                    {
                        //描绘首节点，并与基节点连线
                        srcNode.Set(GenFileName, curNode, null, baseNode, roundNum, top, x);
                        string pattern = ((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
                        if (pattern.Contains("INCONSISTENTCHANGE"))
                        { srcNode.Draw2(this); }
                        else
                        { srcNode.Draw1(this); }                         
                        evoLine.Draw(this, baseNode, srcNode, curNode);
                        leftEvoList.Remove(curNode);
                        preNode = new Node(srcNode);
                        this.nodeList.Add(preNode);
                        s.Push(preNode);
                        visit[Int32.Parse(curNode.GetAttribute("id"))] = 1;
                        #region 深度优先遍历绘图
                        //深度优先遍历，采用栈。如果栈不为空
                        while (s.Count != 0)
                        {
                            curNode = evoList[s.Peek().ID() - 1];//取栈顶元素节点
                            preNode = new Node(s.Peek());
                            //栈顶元素顶点是否存在未被访问过的孩子节点
                            //先将孩子节点提取出来，因为存在“5+6+7”情况
                            do
                            {
                                flag = 0;
                                rowheight = 0;
                                List<int> leftChild = new List<int>();
                                if (curNode.GetAttribute("childID") != "null")
                                {
                                    string childID = curNode.GetAttribute("childID");
                                    while (childID.Contains("+"))
                                    {
                                        int indexofadd = childID.IndexOf("+");
                                        int cid = Int32.Parse(childID.Substring(0, indexofadd));
                                        leftChild.Add(cid);
                                        childID = childID.Substring(indexofadd + 1);
                                    }
                                    leftChild.Add(Int32.Parse(childID));
                                    //判断孩子节点是否都被访问过,存在没访问过的节点则flag=1，否则flag为0
                                    while (leftChild.Count != 0)
                                    {
                                        if (visit[leftChild[0]] != 1)
                                        {
                                            curNode = evoList[leftChild[0] - 1];
                                            flag = 1;
                                            break;
                                        }
                                        rowheight += Node.RowDis;
                                        leftChild.Remove(leftChild[0]);
                                    }
                                    if (flag == 1)//描绘未被访问的节点
                                    {
                                        if (rowheight == 0)//对于同一层的节点
                                            srcNode.Set(GenFileName, curNode, preNode, baseNode, roundNum, 0, x);
                                        else//发生分裂，自动下移一层
                                            srcNode.Set(GenFileName, curNode, preNode, baseNode, roundNum, top, x);
                                        pattern = ((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
                                        if (pattern.Contains("INCONSISTENTCHANGE"))
                                        { srcNode.Draw2(this); }
                                        else
                                        { srcNode.Draw1(this); }                                         
                                        evoLine.Draw(this, preNode, srcNode, curNode);
                                        leftEvoList.Remove(curNode);
                                        preNode = new Node(srcNode);
                                        this.nodeList.Add(preNode);
                                        visit[Int32.Parse(curNode.GetAttribute("id"))] = 1;
                                        s.Push(preNode);
                                    }
                                }
                                else
                                    top += Node.RowDis;//遍历一层过后更新当前绘图深度
                                if (flag == 0)
                                {
                                    s.Pop();
                                    rowheight = 0;
                                }
                            } while (flag == 1);

                        }
                        #endregion
                    }
                }
            }           
            return (y + top);
        }
        class TheLine
        {
            public Point Start { get; set; }
            public Point End { get; set; }
        }
        class MAPLine
        {       
            public void Draw(GenealogyAllViz cgv, Node srcNode, Node destNode, XmlElement evoXml)
            {               
                Point p1 = new Point(srcNode.center.X + (int)(0.5 * Node.width) + 1, srcNode.center.Y);
                Point p2 = new Point(destNode.center.X - (int)(0.5 * Node.width) - 1, destNode.center.Y);
                string pattern = ((XmlElement)evoXml.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
                    if (pattern.Contains("STATIC"))
                    { cgv.Gra.DrawLine(GenealogyAllViz.pen2, p1, p2); }
                    else
                    { cgv.Gra.DrawLine(GenealogyAllViz.pen3, p1, p2); }               
            }
        } 
    }
}
