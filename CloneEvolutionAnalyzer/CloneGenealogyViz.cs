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
    /// <summary>
    /// 定义接口，封装与可视化相关的概念及功能
    /// </summary>
    public interface IVisual
    {
        Graphics G { get; set; }
        XmlDocument GenealogyXml { get; }
        List<CGNode> NodeList { get; }
        void Init();
        void SetGenealogyXml(XmlDocument genealgoyXml);
        void SetScaling(float x, float y);
        void DrawGenealogy();
    }

    /// <summary>
    /// CloneGenealogyViz类继承自Panel类及IVisual接口。其中Panel类对应绘图功能，并可在事件响应中作为参数传递
    /// IVisual类实现与可视化相关的操作
    /// </summary>
    public class CloneGenealogyViz:Panel,IVisual
    {
        public static Pen pen1; //用于画Node边缘的画笔
        public static Pen pen2; //用户画连接线的画笔（无变化）
        public static Pen pen3; //用户画连接线的画笔（有变化）
        public static Pen pen4;//用于画Node边缘的画笔(不一致变换)
        public static SolidBrush brush1;    //用于填充CGNode区域的画刷
        public static SolidBrush brush2;    //用于填充CFNode区域的画刷
        public static SolidBrush brush3;    //用于鼠标移过时填充CGNode区域的画刷
        public static SolidBrush brush4;    //用户鼠标移过时填充CFNode区域的画刷
        public static Font font1;   //克隆代码标签字体
        public static SolidBrush brush5;    //克隆代码标签画刷
        public static string tagSet;    //标签集（26个字母A-Z）
        //对接口中的属性成员进行定义
        private Graphics g;
        public Graphics G { get { return g; } set { g = value; } }
        private XmlDocument genealogyXml;
        public XmlDocument GenealogyXml { get { return genealogyXml; } }
        private List<CGNode> nodeList;
        public List<CGNode> NodeList { get { return nodeList; } }
        public float X = 1.0f;//缩放功能
        public float Y = 1.0f;//缩放功能
        //public CanvasType canvas;

        public new event PaintEventHandler Paint;   //重新定义Paint事件
        public delegate void PaintEventHandler(object sender, PaintEventArgs e);
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.g = e.Graphics;
           // this.AutoScrollMinSize = new Size(636,516);    //估计的绘图大小。当显示窗口小于此大小时，自动添加滚动条
            G.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);

           
            G.ScaleTransform(X,Y);//缩放功能
            
            DrawGenealogy();//绘图
            this.AutoScrollMinSize = new Size((int)((CGNode.rowX+50)*X), (int)((CGNode.rowY+50)*Y)); //自动添加滚动条，适用于缩放
           // this.VerticalScroll.Value = this.VerticalScroll.Minimum;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Global.mainForm.cgv_MouseMove(this, e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Global.mainForm.cgv_MouseClick(this, e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Global.mainForm.cgv_MouseDoubleClick(this, e);
        }     

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);           
        }
     
        public void Init()
        {
            //初始化画笔画刷
            pen1 = new Pen(Color.CornflowerBlue, 2);
            pen2 = new Pen(Color.CornflowerBlue, 1);
            pen3 = new Pen(Color.LightSalmon, 1);
            pen4 = new Pen(Color.HotPink, 2);
            brush1 = new SolidBrush(Color.Aqua);
            brush2 = new SolidBrush(Color.LightSkyBlue);
            brush3 = new SolidBrush(Color.Aqua);
            brush4 = new SolidBrush(Color.PaleVioletRed);
            font1 = new Font("Arial",10);
            brush5 = new SolidBrush(Color.Black);
            //定义标签集
            tagSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            //初始化画布
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Visible = true;
            //设置CGNode和CFNode的基本属性
            CGNode.SetBase();
            CFNode.SetBase();
        }

        public void SetGenealogyXml(XmlDocument geneXml)
        { 
            genealogyXml = geneXml;
        }

        //缩放功能设置缩放大小
        public void SetScaling(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public void DrawGenealogy()
        {
            //首先显示克隆家系整体简单视图
            GenealogyAllViz gav = new GenealogyAllViz();
            gav.Gra = this.g;
            Node.SetBase();
            gav.nodeList = new List<Node>();
            gav.Init();
            gav.SetGenealogyXml(GenealogyXml);
            Node.NodeCenterDis = 60;
            int y = gav.DrawGenGraphics(80, 25, GenealogyXml.Name);//80为首节点横坐标，25为首节点纵坐标,获取已绘图纵坐标
            
            //描绘该克隆家系详细视图
            int genealogyNo = Int32.Parse(((XmlElement)(GenealogyXml.DocumentElement.SelectSingleNode("GenealogyInfo"))).GetAttribute("id"));
            List<XmlElement> evoList = new List<XmlElement>();  //存放所有元素的列表
            foreach (XmlElement evoNode in GenealogyXml.DocumentElement.SelectNodes("Evolution"))
            {
                evoList.Add(evoNode);
            }
            List<XmlElement> leftEvoList = new List<XmlElement>(evoList);  //存放未处理元素的列表
            int roundNum = 0;   //记录处理到第一轮
            //定义用到的对象
            XmlElement curNode;
            XmlElement srcInfoEle;  //Evolution的子元素srcInfo
            XmlElement destInfoEle;
            XmlElement evoInfoEle;  //Evolution的子元素CGMapInfo
            CGNode srcCGNode = new CGNode();    //src元素在家系图中对应的节点
            CGNode preNode = new CGNode();  //定义前驱节点         
            CGNode baseNode = new CGNode();
            //CGNode destCGNode = new CGNode();            
            EvolutionLine evoLine = new EvolutionLine();    //CGMap对应的连接线
            //初始化nodeList
            this.nodeList = new List<CGNode>();
            //用于保存所有克隆群中的克隆代码的标签集（每个克隆代码用A,B,C,等表示）
            List<string> tagList = new List<string>();
            string preTags = "";
            string baseTags = "";
            string curTags;
            int tagSetIndex = 0;
            int rowHeightMax = 0;   //保存每一行CGNode高度的最大值，用于确定下一行的rowY
            int rowheight = 0;
            int flag = 0;
            List<int> visit = new List<int>();//深度优先遍历，采用栈，标记访问
            Stack<CGNode> s = new Stack<CGNode>();
            Stack<string> stag = new Stack<string>();
            for (int i = 0; i <= evoList.Count; i++)
            {
                visit.Add(0);
            }
            //处理每个Evolution元素，绘制CGNode
            while (leftEvoList.Count != 0)
            {
                roundNum++;
                curNode = leftEvoList[0];

                srcInfoEle = (XmlElement)curNode.SelectSingleNode("srcInfo");

                if (roundNum == 1)//基节点
                {
                    CGNode.rowY =  y + 20;
                    int cgsize = Int32.Parse(srcInfoEle.GetAttribute("size"));
                    curTags = "";
                    while (tagSetIndex < cgsize)
                    {
                        curTags += tagSet[tagSetIndex].ToString();    //构造第一个CGNode的标签集
                        tagSetIndex++;
                    }
                    //只有第一个Evo需要画src
                    srcCGNode.Set(curNode, srcInfoEle, null, 1, curTags);
                    srcCGNode.Draw1(this, curTags);
                    srcCGNode.patternflag = false;
                    this.nodeList.Add(srcCGNode);
                    baseNode = new CGNode(srcCGNode); //初始化前驱节点，不能直接用"="赋值!!
                    tagList.Add(curTags);
                    baseTags = curTags;               
                }
                else
                {
                    if (roundNum == 2)
                        CGNode.rowY = y + 20;                                   
                    //判断节点是否为克隆直系的首节点
                    if (curNode.GetAttribute("parentID") == "null")
                    {
                        #region 根据克隆群映射确定curTags
                        //获取当前Evolution元素对应的克隆群映射
                        string srcVersion = ((XmlElement)curNode.SelectSingleNode("srcInfo")).GetAttribute("filename");
                        string destVersion = ((XmlElement)curNode.SelectSingleNode("destInfo")).GetAttribute("filename");
                        Global.mainForm.GetMAPDirFromTreeView1();
                        string mapFileName = AdjacentVersionMapping.MapFileDir + @"\blocks\" + srcVersion + "_" + destVersion + "-MapOnBlocks.xml";
                        XmlDocument mapXml = new XmlDocument();
                        mapXml.Load(mapFileName);   //加载MAP文件
                        int cgMapID = Int32.Parse(((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("id"));
                        XmlElement mapEle = (XmlElement)mapXml.DocumentElement.ChildNodes[cgMapID + 1]; //此赋值没有实际意义（不初始化会出错）
                        foreach (XmlElement ele in mapXml.DocumentElement.SelectNodes("CGMap"))
                        {
                            if (ele.GetAttribute("cgMapid") == cgMapID.ToString())
                            { mapEle = ele; break; }
                        }
                        int destcgsize = Int32.Parse(mapEle.GetAttribute("destCGsize"));
                        char[] tempTags = new char[destcgsize]; //为destCG的标签分配空间
                        bool[] tagFlag = new bool[destcgsize];  //标记destCG中每个CF的标签是否由源继承而来
                        foreach (XmlElement cfMap in mapEle.SelectNodes("CFMap"))
                        {
                            int srccfid = Int32.Parse(cfMap.GetAttribute("srcCFid"));
                            int destcfid = Int32.Parse(cfMap.GetAttribute("destCFid"));
                            tempTags[destcfid - 1] = baseTags[srccfid - 1];
                            tagFlag[destcfid - 1] = true;
                        }
                        for (int i = 0; i < destcgsize; i++)
                        {
                            if (!tagFlag[i])
                            {
                                if (tagSetIndex == 52)
                                { tagSetIndex = 0; }
                                tempTags[i] = tagSet[tagSetIndex++];    //按字母顺序使用下一个字母
                            }
                        }
                        //tempTags每个tag确定后，保存到curTags中
                        curTags = "";
                        for (int i = 0; i < destcgsize; i++)
                        {
                            curTags += tempTags[i].ToString();
                        }
                        #endregion
                        destInfoEle = (XmlElement)curNode.SelectSingleNode("destInfo");
                        CGNode destCGNode = new CGNode();
                        destCGNode.Set(curNode, destInfoEle, baseNode, roundNum, curTags);
                        string pattern = ((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
                        if (pattern.Contains("INCONSISTENTCHANGE"))
                        { destCGNode.Draw2(this, curTags); destCGNode.patternflag = true; }
                        else
                        { destCGNode.Draw1(this, curTags); destCGNode.patternflag = false; }  

                        this.nodeList.Add(destCGNode);
                        tagList.Add(curTags);

                        //画进化连接线
                        evoInfoEle = (XmlElement)curNode.SelectSingleNode("CGMapInfo");
                        evoLine.Set(baseNode, destCGNode, mapEle);
                        evoLine.Draw(this, mapEle);

                        preNode = new CGNode(destCGNode);   //更新前驱节点（上一元素的dest）
                        leftEvoList.Remove(curNode);    //处理完毕，从未处理列表中删除
                        preTags = curTags;
                 
                        s.Push(preNode);
                        stag.Push(preTags);
                        visit[Int32.Parse(curNode.GetAttribute("id"))] = 1;
                        #region 深度优先遍历绘图
                        while (s.Count != 0)
                        {
                            curNode = evoList[s.Peek().ID() - 1];//取栈顶元素节点
                            preNode = new CGNode(s.Peek());
                            preTags = stag.Peek();
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
                                        rowheight = 1;
                                        leftChild.Remove(leftChild[0]);
                                    }
                                    if (flag == 1)//描绘未被访问的节点
                                    {
                                        #region 根据克隆群映射确定destCG标签
                                        //获取当前Evolution元素对应的克隆群映射
                                        string SrcVersion = ((XmlElement)curNode.SelectSingleNode("srcInfo")).GetAttribute("filename");
                                        string DestVersion = ((XmlElement)curNode.SelectSingleNode("destInfo")).GetAttribute("filename");
                                        Global.mainForm.GetMAPDirFromTreeView1();
                                        string MapFileName = AdjacentVersionMapping.MapFileDir + @"\blocks\" + SrcVersion + "_" + DestVersion + "-MapOnBlocks.xml";
                                        XmlDocument MapXml = new XmlDocument();
                                        MapXml.Load(MapFileName);   //加载MAP文件
                                        int CgMapID = Int32.Parse(((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("id"));
                                        XmlElement MapEle = (XmlElement)MapXml.DocumentElement.ChildNodes[CgMapID + 1]; //此赋值没有实际意义（不初始化会出错）
                                        foreach (XmlElement ele in MapXml.DocumentElement.SelectNodes("CGMap"))
                                        {
                                            if (ele.GetAttribute("cgMapid") == CgMapID.ToString())
                                            { MapEle = ele; break; }
                                        }

                                        int Destcgsize = Int32.Parse(MapEle.GetAttribute("destCGsize"));
                                        char[] TempTags = new char[Destcgsize]; //为destCG的标签分配空间
                                        bool[] TagFlag = new bool[Destcgsize];  //标记destCG中每个CF的标签是否由源继承而来

                                        foreach (XmlElement cfMap in MapEle.SelectNodes("CFMap"))
                                        {
                                            int srccfid = Int32.Parse(cfMap.GetAttribute("srcCFid"));
                                            int destcfid = Int32.Parse(cfMap.GetAttribute("destCFid"));
                                            TempTags[destcfid - 1] = preTags[srccfid - 1];
                                            TagFlag[destcfid - 1] = true;
                                        }
                                        for (int i = 0; i < Destcgsize; i++)
                                        {
                                            if (!TagFlag[i])
                                            {
                                                if (tagSetIndex == 52)
                                                { tagSetIndex = 0; }
                                                TempTags[i] = tagSet[tagSetIndex++];    //按字母顺序使用下一个字母
                                            }
                                        }
                                        //tempTags每个tag确定后，保存到curTags中
                                        curTags = "";
                                        for (int i = 0; i < Destcgsize; i++)
                                        {
                                            curTags += TempTags[i].ToString();
                                        }
                                        #endregion
                                        destInfoEle = (XmlElement)curNode.SelectSingleNode("destInfo");
                                        CGNode newdestCGNode = new CGNode();
                                        // CGNode.rowY = CGNode.rowY + rowheight * CGNode.rowDis;
                                        newdestCGNode.Set(curNode, destInfoEle, preNode, roundNum, curTags);
                                        pattern = ((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
                                        if (pattern.Contains("INCONSISTENTCHANGE"))
                                        { newdestCGNode.Draw2(this, curTags); newdestCGNode.patternflag = true; }
                                        else
                                        { newdestCGNode.Draw1(this, curTags); newdestCGNode.patternflag = false; }  
                                       
                                        this.nodeList.Add(newdestCGNode);
                                        tagList.Add(curTags);

                                        //画连接线
                                        evoInfoEle = (XmlElement)curNode.SelectSingleNode("CGMapInfo");
                                        evoLine.Set(preNode, newdestCGNode, MapEle);
                                        evoLine.Draw(this, MapEle);

                                        preTags = curTags;
                                        preNode = new CGNode(newdestCGNode);   //更新前驱节点（上一元素的dest）
                                        leftEvoList.Remove(curNode);
                                        if (rowHeightMax < newdestCGNode.height)
                                        { rowHeightMax = newdestCGNode.height; }

                                        //this.nodeList.Add(preNode);
                                        visit[Int32.Parse(curNode.GetAttribute("id"))] = 1;
                                        s.Push(preNode);
                                        stag.Push(preTags);
                                    }
                                }
                                else
                                {
                                    CGNode.rowY = CGNode.rowY + CGNode.rowDis + rowHeightMax;//遍历一层过后更新当前绘图深度
                                    rowHeightMax = 0;
                                }
                                if (flag == 0)
                                {
                                    s.Pop();
                                    stag.Pop();
                                    rowheight = 0;
                                    rowHeightMax = 0;
                                }

                            } while (flag == 1);
                        }
                        #endregion
                    }

                }
            }
        }
    }
    class Line
    {
        public Point Start { get; set; }
        public Point End { get; set; }
    }

    public class CFNode
    {
        public static int width;
        public static int height;
        public static void SetBase()
        {
            width = 30;
            height = 20;
        }
        public Point center;
        public Rectangle outerRect;
        public char tag;

        /*CFNode坐标的设置在CGNode的Set中完成，只设置center*/
        /// <summary>
        /// 画克隆代码节点（小矩形），带标签
        /// </summary>
        /// <param name="cgv"></param>
        /// <param name="tag">标签字符</param>
        public void Draw1(CloneGenealogyViz cgv, char tag)
        {
            outerRect = new Rectangle(center.X - (int)(0.5 * width), center.Y - (int)(0.5 * height), width, height);
            cgv.G.DrawRectangle(CloneGenealogyViz.pen1, outerRect);              
            cgv.G.FillRectangle(CloneGenealogyViz.brush2, outerRect);
            //添加克隆代码标签
            cgv.G.DrawString(tag.ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, center.X - 8, center.Y - 8);
            this.tag = tag;
        }
        public void Draw2(CloneGenealogyViz cgv, char tag)
        {
            outerRect = new Rectangle(center.X - (int)(0.5 * width), center.Y - (int)(0.5 * height), width, height);
            cgv.G.DrawRectangle(CloneGenealogyViz.pen4, outerRect);
            cgv.G.FillRectangle(CloneGenealogyViz.brush2, outerRect);
            //添加克隆代码标签
            cgv.G.DrawString(tag.ToString(), CloneGenealogyViz.font1, CloneGenealogyViz.brush5, center.X - 8, center.Y - 8);
            this.tag = tag;
        }
    }

    public class CGNode
    {
        //public static Point baseCenter; //第1个CGNode的中心作为基准点
        public static int width;    //node的宽度（CFNode宽度固定，高度由CF数量决定）
        //public static int height;   //node的高度
        public static int distanceHor;  //两个node中心的垂直距离
        //public static int distanceVer;  //两个node中心的垂直距离
        //public static int maxCFNum; //CF显示最大数量，设为12
        public static int rowY;   //存放行水平基准线（初值为0，每一行绘制结束后更新，其值为最大的CGNode的下边沿Y值）
        public static int rowX;  
        public static int rowDis;
        public static int cfNodeCenterDis;  //两个CFNode中心的垂直距离

        public Point center;
        public int height;
        public Rectangle rect;
        public int evoid;
        public int cgMapid;
        public bool patternflag;

        private CGInfo info;
        public CGInfo Info { get { return info; } }

        public CFNode[] cfNodes;
        public int cfNum;   //所含CF数量
        public string cftag;

        //private CGNode parent;
        //public CGNode Parent { get { return parent; } }

        public CGNode() { }
        public CGNode(CGNode node)
        {
            this.center = new Point(node.center.X, node.center.Y);
            this.rect = new Rectangle(node.rect.X, node.rect.Y, node.rect.Width, node.rect.Height);
            this.info.version = node.info.version;
            this.info.id = node.info.id;
            this.info.size = node.info.size;
            this.cfNodes = node.cfNodes;
            this.cfNum = node.cfNum;
            this.cftag = node.cftag;
            this.evoid = node.evoid;
            this.cgMapid = node.cgMapid;
            this.patternflag = node.patternflag;
        }

        
        public static void SetBase()
        {           
            width = 30;           
            distanceHor = 60;           
            rowY = 0;
            rowDis = 10;
            cfNodeCenterDis = 21;
        }
         public int ID()
        {
            return evoid;
        }
         public int CGMAPid()
         {
             return cgMapid;
         }
         public bool PatternFlag()
         {
             return patternflag;
         }

        /// <summary>
        /// 根据Evolution节点信息，设置CGNode属性（用到前驱节点的位置信息及轮次信息）
        /// </summary>
        /// <param name="curEle"></param>
        /// <param name="preNode">前驱节点决定横向位置</param>
        /// <param name="roundNum">轮次决定位置</param>
        public void Set(XmlElement curNode, XmlElement curEle, CGNode preNode, int roundNum, string curTags)
        {
            #region 设置内容
            evoid = Int32.Parse(curNode.GetAttribute("id"));
            cgMapid = Int32.Parse(((XmlElement)curNode.SelectSingleNode("CGMapInfo")).GetAttribute("id"));
            info.version = curEle.GetAttribute("filename");
            info.id = curEle.GetAttribute("cgid");
            info.size = Int32.Parse(curEle.GetAttribute("size"));
            #endregion

            #region 设置位置
            int top, bottom;
            top = rowY + rowDis;
            if (preNode == null && roundNum == 1)   //如果是第一个节点
            {
                center.X = 80;
            }
            else
            {
                center.X = preNode.center.X + distanceHor;   //横向右移
                if (center.X > rowX)
                    rowX = center.X ;               
            }

            cfNum = Int32.Parse(curEle.GetAttribute("size"));
            cftag = curTags;           
            cfNodes = new CFNode[cfNum];   //建立CFNode数组
            cfNodes[0] = new CFNode();
            cfNodes[0].center = new Point(center.X, top + 11);
            for (int i = 1; i < cfNum; i++)
            {
                cfNodes[i] = new CFNode();
                //CF中心下移15
                cfNodes[i].center = new Point(center.X, top + 11 + i * cfNodeCenterDis);
             }
             bottom = cfNodes[cfNum - 1].center.Y + 11;  //bottom为最后一个CFNode中心下移11               

            height = bottom - top - 2;
            center.Y = top + 1 + (int)(0.5 * height);
            rect = new Rectangle(center.X - (int)(0.5 * width), top + 1, width, height);
            #endregion
        }

        /// <summary>
        /// 画克隆群节点（包含克隆代码节点的矩形），标签集作为参数传递
        /// </summary>
        /// <param name="cgv"></param>
        /// <param name="tags"></param>
        public void Draw1(CloneGenealogyViz cgv, string tags)
        {            
            cgv.G.DrawRectangle(CloneGenealogyViz.pen1, this.rect);            
            int i = 0;
            foreach (CFNode cfNode in cfNodes)
            { cfNode.Draw1(cgv, tags[i++]); }    //标签作为参数传递
        }
        public void Draw2(CloneGenealogyViz cgv, string tags)
        {  
            cgv.G.DrawRectangle(CloneGenealogyViz.pen4, this.rect);          
            int i = 0;
            foreach (CFNode cfNode in cfNodes)
            { cfNode.Draw2(cgv, tags[i++]); }    //标签作为参数传递
        }
    }

    class EvolutionLine
    {
        private Line _line;
        public Line _Line { get { return _line; } }

        private CGNode srcNode;
        public CGNode SrcNode { get { return srcNode; } }
        private CGNode destNode;
        public CGNode DestNode { get { return destNode; } }

        private Evolution evo;
        public Evolution Evo { get { return evo; } }

        public void Set(CGNode srcNode, CGNode destNode, XmlElement evoInfoNode)
        {
            this.srcNode = srcNode;
            this.destNode = destNode;
        }

        public void Draw(CloneGenealogyViz cgv, XmlElement mapElement)
        {
            int flag = 0;
            foreach (XmlElement cfMap in mapElement.SelectNodes("CFMap"))
            {
                flag = 1;
                int srccfid = Int32.Parse(cfMap.GetAttribute("srcCFid"));
                int destcfid = Int32.Parse(cfMap.GetAttribute("destCFid"));
                Point p1 = new Point(this.srcNode.cfNodes[srccfid - 1].center.X + (int)(0.5 * CFNode.width) + 1, this.srcNode.cfNodes[srccfid - 1].center.Y);
                Point p2 = new Point(this.destNode.cfNodes[destcfid - 1].center.X - (int)(0.5 * CFNode.width) -1, this.destNode.cfNodes[destcfid - 1].center.Y);
                float textSim = float.Parse(cfMap.GetAttribute("textSim"));
                if (textSim == 1.0)
                { cgv.G.DrawLine(CloneGenealogyViz.pen2, p1, p2); }
                else
                { cgv.G.DrawLine(CloneGenealogyViz.pen3, p1, p2); }
            }
            if (flag == 0)
            {
                Point p1 = new Point(this.srcNode.center.X + (int)(0.5 * CFNode.width) + 1, this.srcNode.center.Y);
                Point p2 = new Point(this.destNode.center.X - (int)(0.5 * CFNode.width) - 1, this.destNode.center.Y);
                cgv.G.DrawLine(CloneGenealogyViz.pen3, p1, p2);
            }
        }
    }
}
