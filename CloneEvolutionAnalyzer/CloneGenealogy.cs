using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace CloneEvolutionAnalyzer
{
    internal struct InfoStruct  //保存进化关系中源和目标信息的结构
    {
        public string version; //CG所在版本（crd文件的文件名，不含路径）
        public string cgid;    //CG的ID
        public int size;    //CG含有的CF数量
        public static bool operator !=(InfoStruct info1, InfoStruct info2)
        { return info1.version != info2.version || info1.cgid != info2.cgid; }
        public static bool operator ==(InfoStruct info1, InfoStruct info2)
        { return info1.version == info2.version && info1.cgid == info2.cgid; }
    }
    //定义进化关系（克隆家系的基本组成元素）
    class Evolution
    {
        public string ID;
        //进化关系的源信息
        private InfoStruct srcInfo;
        public InfoStruct SrcInfo
        {
            get { return srcInfo; }
        }
        //进化关系的目标信息
        private InfoStruct destInfo;
        public InfoStruct DestInfo
        {
            get { return destInfo; }
        }
        //CGMap信息
        private CGMapInfo cgMapInfo;
        public CGMapInfo CGMapInfo
        {
            get { return cgMapInfo; }
        }
        //保存继承关系
        private string parentID;    //保存本进化的父亲的id
        public string ParentID
        { get { return parentID; } }
        private string childID;   //保存本进化的后代的id
        public string ChildID
        { get { return childID; } }

        //根据CGMap对象构造Evolution对象。源和目标文件的信息要传入。使用并更新当前的evolutionList（确定父子关系）
        public void BuildFromCGMap(CloneGroupMapping cgMap, string srcVersionFile, string destVersionFile, ref List<Evolution> evolutionList, ref int id, bool atBlocks)
        {
            if (atBlocks)
            {
                this.srcInfo.version = srcVersionFile.Substring(0, srcVersionFile.IndexOf("_blocks-"));
                this.destInfo.version = destVersionFile.Substring(0, destVersionFile.IndexOf("_blocks-"));
            }
            else
            {
                this.srcInfo.version = srcVersionFile.Substring(0, srcVersionFile.IndexOf("_functions-"));
                this.destInfo.version = destVersionFile.Substring(0, destVersionFile.IndexOf("_functions-"));
            }
            this.srcInfo.cgid = cgMap.srcCGInfo.id;
            this.srcInfo.size = cgMap.srcCGInfo.size;
            this.destInfo.cgid = cgMap.destCGInfo.id;
            this.destInfo.size = cgMap.destCGInfo.size;
            this.cgMapInfo.id = cgMap.ID;
            //构造pattern成员，多个进化模式之间以空格分隔
            this.cgMapInfo.pattern = "";
            if (cgMap.EvoPattern.STATIC)
            { this.cgMapInfo.pattern += "STATIC"; }
            if (cgMap.EvoPattern.SAME)
            { this.cgMapInfo.pattern += "+SAME"; }
            if (cgMap.EvoPattern.ADD)
            { this.cgMapInfo.pattern += "+ADD"; }
            if (cgMap.EvoPattern.SUBSTRACT)
            { this.cgMapInfo.pattern += "+DELETE"; }
            if (cgMap.EvoPattern.CONSISTENTCHANGE)
            { this.cgMapInfo.pattern += "+CONSISTENTCHANGE"; }
            if (cgMap.EvoPattern.INCONSISTENTCHANGE)
            { this.cgMapInfo.pattern += "+INCONSISTENTCHANGE"; }
            if (cgMap.EvoPattern.SPLIT)
            { this.cgMapInfo.pattern += "+SPLIT"; }
            //构造父子成员
            this.parentID = null;   //若有parent，则在下面被置值，否则此项为null
            this.childID = null;
            this.ID = (++id).ToString();
            //在当前的evolutionList中查找destInfo与当前对象的srcInfo相同的对象，确定父子继承关系
            for (int i = 0; i < evolutionList.Count; i++)
            {
                if (evolutionList[i].destInfo == this.srcInfo)
                {
                    if (evolutionList[i].childID == null)
                    {
                        evolutionList[i].childID = this.ID;
                    }
                    else
                    { evolutionList[i].childID += "+" + this.ID; }    //多个ID之间以+分隔
                    this.parentID = evolutionList[i].ID;
                    //break;
                }
            }

        }

        /// <summary>
        /// 根据xml文件中的Evolution元素构造Evolution对象
        /// </summary>
        /// <param name="evolutionNode"></param>
        public void GetFromXmlElement(XmlElement evolutionNode)
        {
            this.ID = evolutionNode.GetAttribute("id");
            this.srcInfo.version = ((XmlElement)evolutionNode.SelectSingleNode("srcInfo")).GetAttribute("filename");
            this.srcInfo.cgid = ((XmlElement)evolutionNode.SelectSingleNode("srcInfo")).GetAttribute("cgid");
            this.destInfo.version = ((XmlElement)evolutionNode.SelectSingleNode("destInfo")).GetAttribute("filename");
            this.destInfo.cgid = ((XmlElement)evolutionNode.SelectSingleNode("destInfo")).GetAttribute("cgid");
            this.cgMapInfo.id = ((XmlElement)evolutionNode.SelectSingleNode("CGMapInfo")).GetAttribute("id");
            this.cgMapInfo.pattern = ((XmlElement)evolutionNode.SelectSingleNode("CGMapInfo")).GetAttribute("pattern");
        }
    }
    //定义克隆家系中保存的CGMap信息
    struct CGMapInfo
    {
        public string id;   //id其实是访问map文件中CGMap元素的“指针”
        public string pattern;
    }
    //定义只包含一个克隆群的克隆家系
    struct SingleCgGenealogy
    {
        public string version;
        public CGInfo cgInfo;
    }
    //定义克隆家系
    class CloneGenealogy
    {
        public string ID;   //克隆家系编号
        public static GranularityType granularity;
        private XmlDocument genealogyXml;   //存放本克隆家系的XmlDocument
        public XmlDocument GenealogyXml
        { get { return genealogyXml; } }
        //起始版本（名称+版本号）
        private string startVersion;
        public string StartVerion
        {
            get { return startVersion; }
        }
        //终止版本
        private string endVersion;
        public string EndVersion
        {
            get { return endVersion; }
        }
        //寿命（或当前年龄）
        private int age;
        public int Age
        {
            get { return age; }
        }
        //根克隆群id
        private string rootCGid;
        public string RootCGid
        {
            get { return rootCGid; }
        }
        //各种进化模式出现次数统计
        private int[] evoPatternCount;
        public int[] EvoPatternCount
        {
            get { return evoPatternCount; }
        }
        //保存进化关系列表
        private List<Evolution> evolutionList;
        public List<Evolution> EvolutionList
        { get { return evolutionList; } }

        public static string GenealogyFileDir;  //保存Genealogy文件的文件夹路径
        public static List<SingleCgGenealogy> SingleCgGenealogyList;    //保存单克隆家系的列表
        public static XmlDocument SingleCgGenealogyXml; //保存单克隆群家系的Xml对象

        //保存映射文件集合（保存文件名）。不保存xml文档的原因：过于庞大
        public static List<string> mapFileCollection;

        //要使用递归么？

        /// <summary>
        /// 生成以指定CG为根的克隆家系
        /// </summary>
        /// <param name="crdFileName">克隆群所在版本的CRD文件名名称（不含路径）</param>
        /// <param name="cgInfo">克隆群信息（id及size）</param>
        /// <param name="mapXmlCollection">所用表示相邻版本版本映射的xml文档集合</param>
        public void BuildForCG(string crdFileName, CGInfo cgInfo, List<XmlDocument> mapXmlCollection, ref int id)
        {
            List<XmlDocument> mapXmlDocs = new List<XmlDocument>();

            #region 获得从crdFileName版本开始的所有映射文档，保存在mapXmlDocs中
            bool flag = false;
            foreach (XmlDocument mapXml in mapXmlCollection)
            {
                if (!flag)
                {
                    if (mapXml.DocumentElement.ChildNodes[0].InnerText == crdFileName)
                    {
                        mapXmlDocs.Add(mapXml);
                        flag = true;
                    }
                }
                else//将从这一版本开始的所有mapXml加入mapXmlDocs中
                { mapXmlDocs.Add(mapXml); }
            }
            #endregion

            if (mapXmlDocs.Count == 0)
            {
                return;
            }
            else
            {
                #region 构造克隆家系信息
                string startFile = mapXmlDocs[0].DocumentElement.ChildNodes[0].InnerText;
                bool atBlocks;  //粒度标识，在函数调用时传递粒度信息
                if (startFile.IndexOf("_blocks-") != -1)
                {
                    CloneGenealogy.granularity = GranularityType.BLOCKS;
                    atBlocks = true;
                    this.startVersion = startFile.Substring(0, startFile.IndexOf("_blocks-"));
                }
                else
                {
                    CloneGenealogy.granularity = GranularityType.FUNCTIONS;
                    atBlocks = false;
                    this.startVersion = startFile.Substring(0, startFile.IndexOf("_functions-"));
                }
                this.rootCGid = cgInfo.id;  //保存根克隆群在该版本中的id
                this.evolutionList = new List<Evolution>();
                #endregion

                #region 构造进化关系列表
                int index = -1;
                int evoID = 0;
                int endIndex = -1;  //记录克隆家系停止生长的版本的位置 
                List<CGInfo> cgInfoList = new List<CGInfo>();   //cgInfoList用来存放当前所关注的克隆群，可能是多个，因此用列表
                cgInfoList.Add(cgInfo);
                foreach (XmlDocument mapXml in mapXmlDocs)
                {
                    index++;
                    List<CloneGroupMapping> cgMapList = new List<CloneGroupMapping>(); //存放当前两个版本间相关映射集
                    foreach (CGInfo info in cgInfoList) //这个循环是考虑分裂的情况而设
                    {
                        //找出mapXml中以info代表的CG为源的所有映射,多数情况下为1个，少数情况多于一个
                        foreach (XmlElement cgMapNode in mapXml.DocumentElement.SelectNodes("CGMap"))
                        {
                            CloneGroupMapping cgMap = new CloneGroupMapping();
                            cgMap.CreateCGMapppingFromCGMapNode(cgMapNode);
                            if (cgMap.srcCGInfo.id == info.id)
                            { cgMapList.Add(cgMap); }
                        }
                    }
                    string srcFileName = mapXml.DocumentElement.ChildNodes[0].InnerText;
                    string destFileName = mapXml.DocumentElement.ChildNodes[1].InnerText;
                    cgInfoList = new List<CGInfo>();
                    if (cgMapList.Count != 0)
                    {
                        //为选出的每一个CGMap构造Evolution对象。同时更新cgInfoList
                        foreach (CloneGroupMapping cgMap in cgMapList)
                        {
                            Evolution evolution = new Evolution();
                            evolution.BuildFromCGMap(cgMap, srcFileName, destFileName, ref this.evolutionList, ref evoID, atBlocks);
                            this.evolutionList.Add(evolution);
                            cgInfoList.Add(cgMap.destCGInfo);
                            endIndex = index;   //更新endIndex的值
                        }
                    }
                    else//当cgMapList.Count为0时，说明此克隆家系停止生长
                    { break; }
                }
                #endregion

                #region 补全克隆家系信息
                this.age = endIndex + 2;    //计算年龄（年龄指克隆群在几个版本中存在，等于它所发生的进化次数+1）
                if (this.age > 1) //如果该克隆群寿命大于1（即不是只在单个版本出现）
                {
                    string endFile = mapXmlDocs[endIndex].DocumentElement.ChildNodes[1].InnerText;
                    if (atBlocks)
                    { this.endVersion = endFile.Substring(0, endFile.IndexOf("_blocks-")); }
                    else
                    { this.endVersion = endFile.Substring(0, endFile.IndexOf("_functions-")); }

                    if (this.evolutionList != null && this.evolutionList.Count != 0)
                    {
                        id++;
                        this.ID = id.ToString();
                    }

                    #region 统计各种进化模式出现的次数，保存在evoPatternCount数组中
                    this.evoPatternCount = new int[7] { 0, 0, 0, 0, 0, 0, 0 };
                    foreach (Evolution evolution in this.evolutionList)
                    {
                        if (evolution.CGMapInfo.pattern.IndexOf("STATIC") != -1)
                        { this.evoPatternCount[0]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("SAME") != -1)
                        { this.evoPatternCount[1]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("ADD") != -1)
                        { this.evoPatternCount[2]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("DELETE") != -1)
                        { this.evoPatternCount[3]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("CONSISTENTCHANGE") != -1)
                        { this.evoPatternCount[4]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("INCONSISTENTCHANGE") != -1)
                        { this.evoPatternCount[5]++; }
                        if (evolution.CGMapInfo.pattern.IndexOf("SPLIT") != -1)
                        { this.evoPatternCount[6]++; }
                    }
                    #endregion
                }

                #endregion
            }
        }

        //静态方法，根据指定的映射文件集生成所有克隆家系
        public static void BuildAndSaveAll(List<string> mapFileCollection)
        {
            //CloneGenealogy.genealogyList=new List<CloneGenealogy>();
            if (!IsMapFileCollectionSuccessive(mapFileCollection))  //判断版本集是否连续
            {
                MessageBox.Show("Map Files NOT Successive! Fix it and Try again!");
                return;
            }

            List<XmlDocument> mapXmlCollection = new List<XmlDocument>();
            //获得存放相邻版本映射信息的XmlDocument对象集合
            foreach (string fileName in mapFileCollection)
            { mapXmlCollection.Add(AdjacentVersionMapping.GetXmlDocument(fileName)); }
            //寻找每个版本中新产生的CG，以其为根建立克隆家系
            int index = -1;
            int id = 0;
            CloneGenealogy.SingleCgGenealogyList = new List<SingleCgGenealogy>();   //初始化单克隆群家系列表
            foreach (XmlDocument mapXml in mapXmlCollection)
            {
                index++;
                string srcFileName, destFileName;   //保存源和目标版本的CRD文件名（不含路径）
                srcFileName = mapXml.DocumentElement.ChildNodes[0].InnerText;
                destFileName = mapXml.DocumentElement.ChildNodes[1].InnerText;
                string prev = "0";    //记录前一个被构造的CG的id，避免重复构造（针对分裂的情况）

                #region 如果是第一个mapXml，为源版本中所有克隆群构建以其为根的克隆家系
                if (index == 0)
                {
                    #region 对每个CGMap中的源CG，以其为根构建克隆家系
                    foreach (XmlElement cgMapNode in mapXml.DocumentElement.SelectNodes("CGMap"))
                    {
                        CloneGroupMapping cgMapping = new CloneGroupMapping();
                        cgMapping.CreateCGMapppingFromCGMapNode(cgMapNode);  //根据CGMap元素构造CloneGroupMapping对象
                        if (cgMapping.srcCGInfo.id != prev)
                        {
                            CloneGenealogy cloneGenealogy = new CloneGenealogy();
                            cloneGenealogy.BuildForCG(srcFileName, cgMapping.srcCGInfo, mapXmlCollection, ref id);
                            cloneGenealogy.SaveGenealogyToXml();
                            //CloneGenealogy.genealogyList.Add(cloneGenealogy);
                            prev = cgMapping.srcCGInfo.id;
                        }
                    }
                    #endregion

                    #region 为UnMappedSrcCG中每个CG构建克隆家系
                    if (mapXml.DocumentElement.SelectSingleNode("UnMappedSrcCG") != null)
                    {
                        foreach (XmlElement unMappedSrcCGNode in mapXml.DocumentElement.SelectSingleNode("UnMappedSrcCG").ChildNodes)
                        {
                            CGInfo cgInfo = new CGInfo();
                            cgInfo.id = unMappedSrcCGNode.GetAttribute("id");
                            cgInfo.size = int.Parse(unMappedSrcCGNode.GetAttribute("size"));
                            CloneGenealogy cloneGenealogy = new CloneGenealogy();
                            //cloneGenealogy.BuildForCG(srcFileName, cgInfo, mapXmlCollection, ref id); //对于第一个版本中的UnMappedSrcCG，不需要此语句
                            SingleCgGenealogy sGenealogy = new CloneEvolutionAnalyzer.SingleCgGenealogy();
                            if (CloneGenealogy.granularity == GranularityType.BLOCKS)
                            { sGenealogy.version = srcFileName.Substring(0, srcFileName.IndexOf("_blocks-")); }
                            else
                            { sGenealogy.version = srcFileName.Substring(0, srcFileName.IndexOf("_functions-")); }
                            sGenealogy.cgInfo = cgInfo;
                            CloneGenealogy.SingleCgGenealogyList.Add(sGenealogy);
                            //cloneGenealogy.SaveGenealogyToXml();
                            //CloneGenealogy.genealogyList.Add(cloneGenealogy);
                        }
                    }
                    #endregion
                }
                #endregion

                #region 对于所有mapXml，为UnMappedDestCG中每个CG构建克隆家系
                if (mapXml.DocumentElement.SelectSingleNode("UnMappedDestCG") != null)
                {
                    foreach (XmlElement unMappedDestCGNode in mapXml.DocumentElement.SelectSingleNode("UnMappedDestCG").ChildNodes)
                    {
                        CGInfo cgInfo = new CGInfo();
                        cgInfo.id = unMappedDestCGNode.GetAttribute("id");
                        cgInfo.size = int.Parse(unMappedDestCGNode.GetAttribute("size"));
                        CloneGenealogy cloneGenealogy = new CloneGenealogy();
                        cloneGenealogy.BuildForCG(destFileName, cgInfo, mapXmlCollection, ref id);
                        if (cloneGenealogy.evolutionList != null && cloneGenealogy.evolutionList.Count != 0)
                        {
                            cloneGenealogy.SaveGenealogyToXml();
                        }
                        else
                        {
                            SingleCgGenealogy sGenealogy = new CloneEvolutionAnalyzer.SingleCgGenealogy();
                            if (CloneGenealogy.granularity == GranularityType.BLOCKS)
                            { sGenealogy.version = destFileName.Substring(0, destFileName.IndexOf("_blocks-")); }
                            else
                            { sGenealogy.version = destFileName.Substring(0, destFileName.IndexOf("_functions-")); }
                            sGenealogy.cgInfo = cgInfo;
                            CloneGenealogy.SingleCgGenealogyList.Add(sGenealogy);
                        }
                        //CloneGenealogy.genealogyList.Add(cloneGenealogy);
                    }
                }
                #endregion
            }
            SaveSingleCgGenealogiesToXml(); //保存SingleCgGenealogies.xml文件
        }

        /// <summary>
        /// 将克隆家系保存至XML文件，存放在GenealogyFiles文件夹的blocks/functions子文件夹下
        /// </summary>
        public void SaveGenealogyToXml()
        {
            this.genealogyXml = new XmlDocument();
            //创建根节点
            XmlElement root = genealogyXml.CreateElement("CloneGenealogy");
            genealogyXml.AppendChild(root);

            #region 添加GenealogyInfo元素
            XmlElement node = genealogyXml.CreateElement("GenealogyInfo");
            XmlAttribute attr = genealogyXml.CreateAttribute("id");
            attr.InnerXml = this.ID;
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("startversion");
            attr.InnerXml = this.startVersion;
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("endversion");
            attr.InnerXml = this.endVersion;
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("age");
            attr.InnerXml = this.age.ToString();
            node.Attributes.Append(attr);
            root.AppendChild(node);
            #endregion

            #region 添加EvolutionPatternCount元素
            node = genealogyXml.CreateElement("EvolutionPatternCount");
            attr = genealogyXml.CreateAttribute("STATIC");
            attr.InnerXml = this.evoPatternCount[0].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("SAME");
            attr.InnerXml = this.evoPatternCount[1].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("ADD");
            attr.InnerXml = this.evoPatternCount[2].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("DELETE");
            attr.InnerXml = this.evoPatternCount[3].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("CONSISTENTCHANGE");
            attr.InnerXml = this.evoPatternCount[4].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("INCONSISTENTCHANGE");
            attr.InnerXml = this.evoPatternCount[5].ToString();
            node.Attributes.Append(attr);
            attr = genealogyXml.CreateAttribute("SPLIT");
            attr.InnerXml = this.evoPatternCount[6].ToString();
            node.Attributes.Append(attr);
            root.AppendChild(node);
            #endregion

            foreach (Evolution evolution in this.evolutionList)
            {
                #region 添加Evolution元素
                node = genealogyXml.CreateElement("Evolution");
                attr = genealogyXml.CreateAttribute("id");  //添加id属性
                attr.InnerXml = evolution.ID;
                node.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("parentID");  //添加parentID属性
                if (evolution.ParentID != null)
                { attr.InnerXml = evolution.ParentID; }
                else
                { attr.InnerXml = "null"; }
                node.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("childID");  //添加childID属性，多个ID之间以"，"分隔
                if (evolution.ChildID != null)
                { attr.InnerXml = evolution.ChildID; }
                else
                { attr.InnerXml = "null"; }
                node.Attributes.Append(attr);
                //添加srcInfo子元素
                XmlElement subNode = genealogyXml.CreateElement("srcInfo");
                attr = genealogyXml.CreateAttribute("filename");  //添加filename属性
                attr.InnerXml = evolution.SrcInfo.version;
                subNode.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("cgid");  //添加cgid属性
                attr.InnerXml = evolution.SrcInfo.cgid;
                subNode.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("size");  //添加size属性
                attr.InnerXml = evolution.SrcInfo.size.ToString();
                subNode.Attributes.Append(attr);
                node.AppendChild(subNode);
                //添加destInfo子元素
                subNode = genealogyXml.CreateElement("destInfo");
                attr = genealogyXml.CreateAttribute("filename");  //添加属性
                attr.InnerXml = evolution.DestInfo.version;
                subNode.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("cgid");  //添加属性
                attr.InnerXml = evolution.DestInfo.cgid;
                subNode.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("size");  //添加属性
                attr.InnerXml = evolution.DestInfo.size.ToString();
                subNode.Attributes.Append(attr);
                node.AppendChild(subNode);
                //添加CGMapInfo子元素
                subNode = genealogyXml.CreateElement("CGMapInfo");
                attr = genealogyXml.CreateAttribute("id");
                attr.InnerXml = evolution.CGMapInfo.id;
                subNode.Attributes.Append(attr);
                attr = genealogyXml.CreateAttribute("pattern");
                attr.InnerXml = evolution.CGMapInfo.pattern;    //多个进化模式之间以空格分隔
                subNode.Attributes.Append(attr);
                node.AppendChild(subNode);
                root.AppendChild(node);
                #endregion
            }

            #region 创建GenealogyFiles文件夹，并保存.xml文件
            Global.mainForm.GetCRDDirFromTreeView1();
            CloneGenealogy.GenealogyFileDir = CloneRegionDescriptor.CrdDir.Replace("CRDFiles", "GenealogyFiles");
            DirectoryInfo dir = new DirectoryInfo(CloneGenealogy.GenealogyFileDir); //创建GenealogyFiles文件夹
            dir.Create();
            string fileName;
            DirectoryInfo genealogySubDir;
            if (CloneGenealogy.granularity == GranularityType.FUNCTIONS)
            {
                genealogySubDir = new DirectoryInfo(CloneGenealogy.GenealogyFileDir + @"\functions");   //建立functions子文件夹
                genealogySubDir.Create();
                fileName = "Genealogy-" + this.ID + "_" + this.startVersion + "_" + this.endVersion + "__" + this.rootCGid + "_functions.xml";
            }
            else
            {
                genealogySubDir = new DirectoryInfo(CloneGenealogy.GenealogyFileDir + @"\blocks");
                genealogySubDir.Create();
                fileName = "Genealogy-" + this.ID + "_" + this.startVersion + "_" + this.endVersion + "__" + this.rootCGid + "_blocks.xml";
            }
            XmlTextWriter writer = new XmlTextWriter(genealogySubDir + "\\" + fileName, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            try
            { this.genealogyXml.Save(writer); }
            catch (Exception ee)
            { MessageBox.Show("Save Genealogy files Failed! " + ee.Message); }

            writer.Close();
            #endregion
        }

        /// <summary>
        /// 保存SingleCgGenealogiesXml对象至xml文件，与Genealogy文件在同一文件夹下
        /// </summary>
        public static void SaveSingleCgGenealogiesToXml()
        {
            CloneGenealogy.SingleCgGenealogyXml = new XmlDocument();

            #region 创建SingleCgGenealogyXml对象
            XmlElement root = CloneGenealogy.SingleCgGenealogyXml.CreateElement("SingleCgGenealogies");
            CloneGenealogy.SingleCgGenealogyXml.AppendChild(root);
            foreach (SingleCgGenealogy sGenealogy in SingleCgGenealogyList)
            {
                //添加SingleCgGenealogy元素
                XmlElement node = CloneGenealogy.SingleCgGenealogyXml.CreateElement("SingleCgGenealogy");
                //添加version,cgid,cgsize属性
                XmlAttribute attr = CloneGenealogy.SingleCgGenealogyXml.CreateAttribute("version");
                attr.InnerXml = sGenealogy.version;
                node.Attributes.Append(attr);
                attr = CloneGenealogy.SingleCgGenealogyXml.CreateAttribute("cgid");
                attr.InnerXml = sGenealogy.cgInfo.id;
                node.Attributes.Append(attr);
                attr = CloneGenealogy.SingleCgGenealogyXml.CreateAttribute("cgsize");
                attr.InnerXml = sGenealogy.cgInfo.size.ToString();
                node.Attributes.Append(attr);
                root.AppendChild(node);
            }
            #endregion

            #region 保存SingleCgGenealogies.xml文件
            DirectoryInfo genealogySubDir;
            if (CloneGenealogy.granularity == GranularityType.FUNCTIONS)
            {
                genealogySubDir = new DirectoryInfo(CloneGenealogy.GenealogyFileDir + @"\functions");//实际上，经过SaveGenealogyToXml后，该文件夹已经存在，不需要创建
            }
            else
            { genealogySubDir = new DirectoryInfo(CloneGenealogy.GenealogyFileDir + @"\blocks"); }
            genealogySubDir.Create();

            string fileName = "SingleCgGenealogies.xml";

            XmlTextWriter writer = new XmlTextWriter(genealogySubDir + "\\" + fileName, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            try
            { CloneGenealogy.SingleCgGenealogyXml.Save(writer); }
            catch (Exception ee)
            { MessageBox.Show("Save SingleCgGenealogies file Failed! " + ee.Message); }
            #endregion
        }

        //判断版本集是否连续
        private static bool IsMapFileCollectionSuccessive(List<string> mapFileCollection)
        {
            string[,] prevAndNextVersion = new string[mapFileCollection.Count, 2];  //存放版本链信息
            int index = 0;
            int index1, index2;
            //获取版本链
            foreach (string mapFile in mapFileCollection)
            {
                index1 = mapFile.IndexOf("_");
                prevAndNextVersion[index, 0] = mapFile.Substring(0, index1);   //数组第二维第一个元素存放源系统名称
                //index1 = mapFile.IndexOf("_");
                index2 = mapFile.IndexOf("-MapOn");
                prevAndNextVersion[index, 1] = mapFile.Substring(index1 + 1, index2 - 1 - index1);  //获取目标系统名称
                index++;
            }
            //检查版本链是否连续
            for (index = 0; index < mapFileCollection.Count - 1; index++)
            {
                if (prevAndNextVersion[index, 1] != prevAndNextVersion[index + 1, 0])
                { return false; }
            }
            return true;
        }
    }
}
