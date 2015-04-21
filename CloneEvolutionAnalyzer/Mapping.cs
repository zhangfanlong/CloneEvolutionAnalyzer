using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Xml;
using System.Diagnostics;   //使用计时功能

namespace CloneEvolutionAnalyzer
{
    //定义CGMapEventArgs事件类，用于携带映射信息
    public class CGMapEventArgs : EventArgs
    {
        public string srcStr;   //当前选择的源系统CRD文件名
        public string destStr;  //当前选择的目标系统CRD文件名
    }
    //定义CGMapEventHandler委托，用以匹配CGMapEventArgs事件的处理函数
    public delegate void CGMapEventHandler(object sender, CGMapEventArgs e);
    //定义MappingList类，作为ArrayList类的别名
    public class MappingList : ArrayList { }
    //定义抽象类Mapping，为其他映射类的基类
    abstract class Mapping
    {
        public string ID { get; set; }  //映射关系编号
        //public string Src { get; set; } //映射关系中的源
        //public string Dest { get; set; }//映射关系中的目标
    }
    class CloneFragmentMapping : Mapping
    {
        public string SrcCFID { get; set; }
        public string DestCFID { get; set; }
        //保存映射的两个CF的CRD，是为了在识别进化模式时，计算textSim（因为在MapCF中不计算）
        public CloneRegionDescriptor srcCrd { get; set; }
        public CloneRegionDescriptor destCrd { get; set; }

        internal CRDMatchLevel crdMatchLevel;
        public CRDMatchLevel CrdMatchLevel //新增属性，用来保存两个CF的CRD匹配级别
        {
            get { return crdMatchLevel; }
        }

        //public float overlap; //新增成员，用来保存两个CF的位置覆盖率

        public float textSim;  //两段源代码的文本相似度，从diff信息中获得
        //保存两段克隆代码的位置信息，用于计算diff时读取。sourceInfos[0]为源信息，sourceInfos[1]为目标信息
        public CloneSourceInfo[] sourceInfos = new CloneSourceInfo[2];
        //public Diff.DiffInfo diffInfo;  //保存两段源代码diff信息的对象（考虑不保存diff信息，而是在需要时再计算，否则过于浪费空间）
    }

    public struct CGInfo   //CG信息结构体，包含CG的ID和大小（包含克隆片段的数量）
    {
        public string version;
        public string id;
        public int size;
    }

    //定义进化模式
    struct EvolutionPattern
    {
        public bool STATIC;    //定义7种进化模式
        public bool SAME;
        public bool ADD;
        public bool SUBSTRACT; //原来叫DELETE模式，后改为SUBSTRACT
        public bool CONSISTENTCHANGE;
        public bool INCONSISTENTCHANGE;
        public bool SPLIT;
        public string MapGroupIDs;   //若映射为一对多（即分裂）的情况，记录多个目标的ID（各ID之间以"，"分隔）。否则为null。
    }

    //??考虑增加CG编号的相关内容在，在CloneGenealogy构建阶段
    /// <summary>
    /// 克隆群映射类。在一对多情况的处理上，采用创建多个独立映射关系的方式
    /// </summary>
    class CloneGroupMapping : Mapping
    {
        public EvolutionPattern EvoPattern;    //保存进化模式信息
        public MappingList CFMapList { get; set; }  //保存CF映射的列表
        public CGInfo srcCGInfo;    //源克隆群信息
        public CGInfo destCGInfo;   //目标克隆群信息

        /// <summary>
        /// 为以确定匹配的两个CG创建一个映射关系对象
        /// </summary>
        /// <param name="srcClassEle">表示源克隆群的XmlElement对象</param>
        /// <param name="destClassEle">表示目标克隆群的XmlElement对象</param>
        /// <param name="mapCount">引用参数，传入及传入当前映射的序号</param>
        public void CreateCGMapping(XmlElement srcClassEle, XmlElement destClassEle, ref int mapCount)
        {
            
            this.srcCGInfo.id = srcClassEle.GetAttribute("classid");
            this.srcCGInfo.size = srcClassEle.ChildNodes.Count;
            //构造目标CG信息结构，加入destCGInfoList成员
            this.destCGInfo.id = destClassEle.GetAttribute("classid");
            this.destCGInfo.size = destClassEle.ChildNodes.Count;
            //映射克隆群内的克隆片段映射，结果保存到CFMapList成员中
            this.CFMapList = MapCF(srcClassEle, destClassEle);
            this.ID = (++mapCount).ToString();
        }

        /// <summary>
        /// 将已匹配的克隆群(CG)中的克隆片段(CF)进行匹配（修改的MapCF算法，不计算textSim）
        /// </summary>
        /// <param name="srcCG"></param>
        /// <param name="destCG"></param>
        /// <returns></returns>

        #region 计算textSim的MapCF方法
        internal MappingList MapCF(XmlElement srcCG, XmlElement destCG)
        {
            MappingList cfMappingList = new MappingList();
            //分别获取两个克隆群中克隆片段的CRD元素列表
            XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptor");
            XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptor");
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            int i, j;
            //建立两个标记数组保存srcCF和destCF的映射情况
            bool[] srcCFMapped = new bool[srcCGCrdList.Count];
            for (i = 0; i < srcCGCrdList.Count; i++)
            { srcCFMapped[i] = false; }
            bool[] destCFMapped = new bool[destCGCrdList.Count];
            for (j = 0; j < destCGCrdList.Count; j++)
            { destCFMapped[j] = false; }

            #region 开始映射
            if (srcCGCrdList != null && destCGCrdList != null && srcCGCrdList.Count != 0 && destCGCrdList.Count != 0)
            {
                //建立矩阵保存CRDMatch结果
                CRDMatchLevel[,] crdMatchMatrix = new CRDMatchLevel[srcCGCrdList.Count, destCGCrdList.Count];
                //建立矩阵保存textSim结果
                float[,] textSimMatrix = new float[srcCGCrdList.Count, destCGCrdList.Count];

                i = -1;
                int mapCount = 0;

                #region 第一步，为每对srcCF与destCF计算textSim及CRDMatchLevel
                foreach (XmlElement srcNode in srcCGCrdList)
                {
                    srcCrd.CreateCRDFromCRDNode(srcNode);
                    i++;
                    j = -1;
                    foreach (XmlElement destNode in destCGCrdList)
                    {
                        j++;
                        if (!destCFMapped[j])
                        {
                            destCrd.CreateCRDFromCRDNode(destNode);
                            CRDMatchLevel matchLevel = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);
                            crdMatchMatrix[i, j] = matchLevel;
                            textSimMatrix[i, j] = CloneRegionDescriptor.GetTextSimilarity(srcCrd, destCrd, true); //最后一个参数指定是否忽略空行
                        }
                    }
                }
                #endregion

                #region 第二步，根据textSim及CRDMatchLevel，共同确定CF映射
                for (i = 0; i < srcCGCrdList.Count; i++)
                {
                    //跳过已映射的srcCF
                    if (!srcCFMapped[i])
                    {
                        int maxTextSimIndex = -1;   //用于记录最大的textSim对应的destCF索引
                        int maxMatchLevelIndex = -1;    //用于记录最高的CRDMatchLevel对应的destCF的索引
                        float maxTextSim = CloneRegionDescriptor.defaultTextSimTh;  //用于记录最大的textSim值，下限为阈值
                       // float maxTextSim = (float) 0.1; 
                        CRDMatchLevel maxMatchLevel = CRDMatchLevel.DIFFERENT;
                        for (j = 0; j < destCGCrdList.Count; j++)
                        {
                            if (!destCFMapped[j])
                            {
                                if (textSimMatrix[i, j] > maxTextSim)   //获取最大的textSim及索引
                                { maxTextSim = textSimMatrix[i, j]; maxTextSimIndex = j; }
                                if (crdMatchMatrix[i, j] > maxMatchLevel)   //获取最高的CRDMatchLevel及索引
                                { maxMatchLevel = crdMatchMatrix[i, j]; maxMatchLevelIndex = j; }
                            }
                            else
                            { continue; }
                        }
                        if (maxTextSimIndex > -1 && maxMatchLevelIndex > -1)  //如果找到
                        {
                            int finalIndex;
                            if (maxTextSimIndex == maxMatchLevelIndex)  //两个最大在同一个destCF上，则创建映射
                            {
                                finalIndex = maxTextSimIndex;
                            }
                            else if (crdMatchMatrix[i, maxTextSimIndex] < maxMatchLevel)
                            {
                                finalIndex = maxMatchLevelIndex;
                            }
                            else if (textSimMatrix[i, maxMatchLevelIndex] < maxTextSim)
                            {
                                finalIndex = maxTextSimIndex;
                            }
                            else//当根据textSim和CRDMatchLevel仍无法确定时，选index最接近i的一个
                            {
                                if (Math.Abs(maxTextSimIndex - i) < Math.Abs(maxMatchLevelIndex - i))
                                { finalIndex = maxTextSimIndex; }
                                else
                                { finalIndex = maxMatchLevelIndex; }
                            }
                            CloneFragmentMapping cfMapping = new CloneFragmentMapping();
                            cfMapping.ID = (++mapCount).ToString();
                            cfMapping.SrcCFID = (i + 1).ToString();
                            cfMapping.DestCFID = (finalIndex + 1).ToString();
                            cfMapping.textSim = textSimMatrix[i, finalIndex];
                            cfMapping.crdMatchLevel = crdMatchMatrix[i, finalIndex];
                            cfMappingList.Add(cfMapping);
                            srcCFMapped[i] = true;
                            destCFMapped[finalIndex] = true;
                        }
                    }
                }
                #endregion
            }
            #endregion

            return cfMappingList;
        }
        #endregion

        //根据映射文件中的CGMap节点创建CloneGroupMapping对象
        public void CreateCGMapppingFromCGMapNode(XmlElement cgMapElement)
        {
            if (cgMapElement.Name != "CGMap")
            {
                MessageBox.Show("Create CGMapping failed!");
                return;
            }
            //获取源CG及目标CG信息
            this.ID = cgMapElement.GetAttribute("cgMapid");
            this.srcCGInfo.id = cgMapElement.GetAttribute("srcCGid");
            Int32.TryParse(cgMapElement.GetAttribute("srcCGsize"), out this.srcCGInfo.size);
            this.destCGInfo.id = cgMapElement.GetAttribute("destCGid");
            Int32.TryParse(cgMapElement.GetAttribute("destCGsize"), out this.destCGInfo.size);
            //为EvoPattern成员赋值
            XmlElement evoPatternEle = (XmlElement)cgMapElement.SelectSingleNode("EvolutionPattern");
            this.EvoPattern.STATIC = evoPatternEle.GetAttribute("STATIC") == "True" ? true : false;
            this.EvoPattern.SAME = evoPatternEle.GetAttribute("SAME") == "True" ? true : false;
            this.EvoPattern.ADD = evoPatternEle.GetAttribute("ADD") == "True" ? true : false;
            this.EvoPattern.SUBSTRACT = evoPatternEle.GetAttribute("DELETE") == "True" ? true : false;
            this.EvoPattern.CONSISTENTCHANGE = evoPatternEle.GetAttribute("CONSISTENTCHANGE") == "True" ? true : false;
            this.EvoPattern.INCONSISTENTCHANGE = evoPatternEle.GetAttribute("INCONSISTENTCHANGE") == "True" ? true : false;
            this.EvoPattern.SPLIT = evoPatternEle.GetAttribute("SPLIT") == "True" ? true : false;
            this.EvoPattern.MapGroupIDs = evoPatternEle.GetAttribute("MapGroupIDs");
            //为CFMapList成员赋值
            CloneFragmentMapping cfMap = new CloneFragmentMapping();
            this.CFMapList = new MappingList();
            foreach (XmlElement cfMapNode in cgMapElement.SelectNodes("CFMap"))
            {
                cfMap.ID = cfMapNode.GetAttribute("cfMapid");
                cfMap.SrcCFID = cfMapNode.GetAttribute("srcCFid");
                cfMap.DestCFID = cfMapNode.GetAttribute("destCFid");
                cfMap.textSim = float.Parse(cfMapNode.GetAttribute("textSim"));
                cfMap.sourceInfos = null;   //源代码信息从map文件中无法获取
                this.CFMapList.Add(cfMap);
            }
        }
    }

    //定义相邻版本间映射类
    class AdjacentVersionMapping : Mapping
    {
        public static string MapFileDir { get; set; }  //保存存放map文件的文件夹的路径
        public string FilePath { get; set; }    //保存映射文件(.xml)的完整路径
        public XmlDocument MapXml { get; set; } //存放完整映射关系的Xml文档对象
        public GranularityType mapGranularity;  //映射的粒度functions或blocks（取决于CRD文件是functions还是blocks）

        //三个列表，存储已映射克隆群对和未映射克隆群信息
        public MappingList CGMapList { get; set; }  //保存CG映射的列表
        public MappingList UnMappedSrcCGList { get; set; } //保存源系统中未找到目标的CG
        public MappingList UnMappedDestCGList { get; set; } //保存目标系统中未找到源的CG（新产生的CG）
        //源系统和目标系统的信息（名称及crd文件）
        public string srcFileName;  //用于映射的源文件（-withCRD.xml文件）名
        public string destFileName;
        public string srcSysName;  //包括系统名称+版本信息，在OnStartMapping方法中得到值
        public string destSysName;

        public XmlDocument SrcCrdFile { get; set; }
        public XmlDocument DestCrdFile { get; set; }

        /// <summary>
        /// MapSettingFinished事件的响应函数，MapSettingFinished事件在点击"Map"按钮时被触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnStartMapping(object sender, CGMapEventArgs e)
        {
            if (e.srcStr.IndexOf("_blocks-") != -1 && e.destStr.IndexOf("_blocks-") != -1)
            {
                //保存CRD文件名称（带相对路径）
                this.srcFileName = e.srcStr;
                this.destFileName = e.destStr;
                //从crd文件名中提取系统名称
                this.srcSysName = e.srcStr.Substring(0, e.srcStr.IndexOf("_blocks-"));
                this.destSysName = e.destStr.Substring(0, e.destStr.IndexOf("_blocks-"));
                //MapOnBlocks(e.srcStr, e.destStr);
                this.mapGranularity = GranularityType.BLOCKS;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                MapBetweenVersions(e.srcStr, e.destStr);
                sw.Stop();
                RecognizeEvolutionPattern();    //识别进化模式
                SaveMappingToXml();
                Global.mainForm.RefreshTreeView1(); //因为生成了新的文件，因此刷新主窗口treeview1
                if (Global.mappingVersionRange == "ADJACENTVERSIONS")   //当只进行相邻版本映射时，弹出此窗口（表示映射已完成）
                {
                    MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Blocks level Finished! Time Cost:" + sw.ElapsedMilliseconds.ToString() + "ms.");
                    if (Global.MapState == 0)   //置映射状态
                    { Global.MapState = 1; }
                }
            }
            else if (e.srcStr.IndexOf("_functions-") != -1 && e.destStr.IndexOf("_functions-") != -1)
            {
                this.srcFileName = e.srcStr;
                this.destFileName = e.destStr;
                this.srcSysName = e.srcStr.Substring(0, e.srcStr.IndexOf("_functions-"));
                this.destSysName = e.destStr.Substring(0, e.destStr.IndexOf("_functions-"));
                this.mapGranularity = GranularityType.FUNCTIONS;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                MapBetweenVersions(e.srcStr, e.destStr);
                RecognizeEvolutionPattern();
                SaveMappingToXml();
                sw.Stop();
                Global.mainForm.RefreshTreeView1();
                if (Global.mappingVersionRange == "ADJACENTVERSIONS")
                {
                    MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Functions level Finished! Time Cost:" + sw.ElapsedMilliseconds.ToString() + "ms.");
                    if (Global.MapState == 0)
                    { Global.MapState = 1; }
                }
            }
            else
            {
                MessageBox.Show("Granularity unmatched!Choose versions that are both on block level or function level!");
            }
        }

        #region 采用CRD+LocationOverlap的Multi-Round算法
        public void MapBetweenVersions(string srcVersion, string destVersion)
        {
            this.LoadCrdFile(srcVersion, destVersion);

            XmlElement srcRoot = this.SrcCrdFile.DocumentElement;
            XmlElement destRoot = this.DestCrdFile.DocumentElement;
            //获取两个xml文件中的class元素列表
            XmlNodeList srcCGList, destCGList;
            if (srcRoot.SelectNodes("class").Count != 0)
            { srcCGList = srcRoot.SelectNodes("class"); }
            else { srcCGList = null; }
            if (destRoot.SelectNodes("class").Count != 0)
            { destCGList = destRoot.SelectNodes("class"); }
            else { destCGList = null; }
            //如果srcCGList和destCGList中有一个为空，返回
            if (srcCGList == null || destCGList == null)
            {
                this.CGMapList = null;
                MessageBox.Show(@"No Mappping At All:The Src/Dest CG Collection is EMPTY!");
                return;
            }
            //否则，开始。
            #region 置初始状态
            this.CGMapList = new MappingList();
            this.UnMappedSrcCGList = null;
            this.UnMappedDestCGList = null;
            int mapCount = 0; //用于统计CG映射的数量
            int srcIndex, destIndex; //用作CG在列表中的索引及循环变量
            int[,] cgMatchLevelMatrix = new int[srcCGList.Count, destCGList.Count];
            //float[,] cgTextSimMatrix = new float[srcCGList.Count, destCGList.Count];
            float[,] cgLocationOverlapMatrix = new float[srcCGList.Count, destCGList.Count];
            for (int i = 0; i < srcCGList.Count; i++)
            {
                for (int j = 0; j < destCGList.Count; j++)
                {
                    cgMatchLevelMatrix[i, j] = -1;   //-1表示未计算matchLevel
                    cgLocationOverlapMatrix[i, j] = -1;  //-1表示未计算
                }
            }

            bool[] isSrcCGMapped = new bool[srcCGList.Count];   //源克隆群映射标记数组
            bool[] isDestCGMapped = new bool[destCGList.Count];   //目标克隆群映射标记数组

            for (srcIndex = 0; srcIndex < srcCGList.Count; srcIndex++)
            { isSrcCGMapped[srcIndex] = false; }
            for (destIndex = 0; destIndex < destCGList.Count; destIndex++)
            { isDestCGMapped[destIndex] = false; }
            #endregion

            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw3 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            Stopwatch sw4 = new Stopwatch();

            int counter = 0;    //统计计算文本相似度的次数

            sw1.Start();
            #region 第一轮映射（在METHODINFOMATCH级别上映射）
            srcIndex = -1;
            foreach (XmlElement srcClassEle in srcCGList)
            {
                srcIndex++;  //用于获得当前srcClassEle在srcCGList中的序号
                destIndex = -1;
                foreach (XmlElement destClassEle in destCGList)
                {
                    destIndex++;
                    if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODINFOMATCH, ref counter))
                    {
                        //如果两个CG匹配，则构造一个CG映射关系对象，加入映射列表中
                        CloneGroupMapping cgMapping = new CloneGroupMapping();
                        cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
                        //将此CG映射关系加入CGMapList中
                        this.CGMapList.Add(cgMapping);
                        isSrcCGMapped[srcIndex] = true;
                        isDestCGMapped[destIndex] = true; //置已映射标记为真
                        break;  //源克隆群找到对应的（一个）CG后，不再继续寻找
                    }
                    else
                    { continue; }
                }
            }
            #endregion
            sw1.Stop();

            sw2.Start();
            #region 第二轮映射（在METHODNAMEMATCH级别上映射，与第一轮基本相似）
            srcIndex = -1;
            foreach (XmlElement srcClassEle in srcCGList)
            {
                srcIndex++;
                //只考察第一轮映射中未被映射的源克隆群
                if (!isSrcCGMapped[srcIndex])
                {
                    destIndex = -1;
                    foreach (XmlElement destClassEle in destCGList)
                    {
                        destIndex++;
                        //只考察未被映射的目标克隆群，计算matchlevel，必要时计算textSim
                        if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODNAMEMATCH, ref counter))
                        {
                            CloneGroupMapping cgMapping = new CloneGroupMapping();
                            cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
                            this.CGMapList.Add(cgMapping);
                            isSrcCGMapped[srcIndex] = true;
                            isDestCGMapped[destIndex] = true;
                            break;
                        }
                        else
                        { continue; }
                    }
                }
                else
                { continue; }
            }
            #endregion
            sw2.Stop();

            sw3.Start();
            #region 第三轮映射（在FILECLASSMATCH级别上映射，与第一轮基本相似）
            srcIndex = -1;
            foreach (XmlElement srcClassEle in srcCGList)
            {
                srcIndex++;
                //只考察未被映射的源克隆群
                if (!isSrcCGMapped[srcIndex])
                {
                    destIndex = -1;
                    foreach (XmlElement destClassEle in destCGList)
                    {
                        destIndex++;
                        //只考察未被映射的目标克隆群，计算matchlevel，必要时计算textSim
                        if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.FILECLASSMATCH, ref counter))
                        {
                            CloneGroupMapping cgMapping = new CloneGroupMapping();
                            cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
                            this.CGMapList.Add(cgMapping);
                            isSrcCGMapped[srcIndex] = true;
                            isDestCGMapped[destIndex] = true;
                            break;
                        }
                        else
                        { continue; }
                    }
                }
                else
                { continue; }
            }
            #endregion
            sw3.Stop();

            #region 将前三轮后未被映射的源克隆群加入UnMappedSrcCGList
            srcIndex = -1;
            foreach (XmlElement srcClassEle in srcCGList)
            {
                srcIndex++;
                if (!isSrcCGMapped[srcIndex])
                {
                    if (this.UnMappedSrcCGList == null)
                    { this.UnMappedSrcCGList = new MappingList(); }
                    CGInfo info = new CGInfo();
                    info.id = srcClassEle.GetAttribute("classid");
                    info.size = srcClassEle.ChildNodes.Count;
                    this.UnMappedSrcCGList.Add(info);

                }
            }
            #endregion

            sw4.Start();
            #region 第四轮映射（处理一对多的情况，在METHODNAMEMATCH级别上考查，与前几轮类似，只是从dest到src方向）
            destIndex = -1;
            foreach (XmlElement destClassEle in destCGList)
            {
                destIndex++;
                //只考察未被映射的destCG
                if (!isDestCGMapped[destIndex])
                {
                    srcIndex = -1;
                    foreach (XmlElement srcClassEle in srcCGList)
                    {
                        srcIndex++;
                        //只考察已映射的源克隆群（若不满足，则不进行后面比较）
                        if (isSrcCGMapped[srcIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODNAMEMATCH, ref counter))
                        {
                            //在现存的映射列表中查找srcClassEle所在的映射，创建一个映射插入它后面的位置，使具有统一源的两个映射位置上相邻
                            int mapIndex = -1;
                            foreach (CloneGroupMapping cgMap in this.CGMapList)
                            {
                                mapIndex++;
                                if (cgMap.srcCGInfo.id == srcClassEle.GetAttribute("classid"))
                                {
                                    CloneGroupMapping cgMapping = new CloneGroupMapping();
                                    cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
                                    //将此CG映射关系加入CGMapList中mapIndex+1的位置
                                    this.CGMapList.Insert(mapIndex + 1, cgMapping);
                                    isDestCGMapped[destIndex] = true; //置已映射标记为真                                       
                                    break;
                                }
                            }
                            break;
                        }
                        else
                        { continue; }
                    }
                }
            }
            #endregion
            sw4.Stop();

            #region 将未找到源克隆群的目标克隆群加入UnMappedDestCGList
            for (destIndex = 0; destIndex < destCGList.Count; destIndex++)
            {
                if (!isDestCGMapped[destIndex])
                {
                    if (this.UnMappedDestCGList == null)
                    { this.UnMappedDestCGList = new MappingList(); }
                    CGInfo info = new CGInfo();
                    info.id = ((XmlElement)destCGList[destIndex]).GetAttribute("classid");
                    info.size = ((XmlElement)destCGList[destIndex]).ChildNodes.Count;
                    this.UnMappedDestCGList.Add(info);
                }
            }
            #endregion

        }
        #endregion

        /// <summary>
        /// 识别克隆群的进化模式
        /// </summary>
        private void RecognizeEvolutionPattern()
        {
            bool[] atGroupFlag;
            if (this.CGMapList.Count > 0)
            {
                atGroupFlag = new bool[this.CGMapList.Count];
                foreach (CloneGroupMapping cgMap in this.CGMapList)
                {
                    #region 原来的进化模式识别方法（原来的理解，不在用）
                    ////识别SAME模式
                    //if (cgMap.srcCGInfo.size == cgMap.destCGInfo.size)
                    //{ cgMap.EvoPattern.SAME = true; }
                    ////识别ADD模式
                    //if (cgMap.CFMapList.Count < cgMap.destCGInfo.size)
                    //{ cgMap.EvoPattern.ADD = true; }
                    ////识别DELETE模式
                    //if (cgMap.CFMapList.Count < cgMap.srcCGInfo.size)
                    //{ cgMap.EvoPattern.DELETE = true; }

                    //#region 识别STATIC模式，进而识别CONSISTENTCHANGE模式
                    //if (cgMap.EvoPattern.SAME)
                    //{
                    //    bool textSameFlag = true;    //标记各个CFMap的文本是否完全相同
                    //    foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
                    //    {
                    //        if (cfMap.textSim < 1)
                    //        { textSameFlag = false; break; }
                    //    }
                    //    if (textSameFlag)
                    //    { cgMap.EvoPattern.STATIC = true; }
                    //    else
                    //    {
                    //        //判断是否是CONSISTENTCHANGE
                    //        bool textSimSameFlag = true;    //此时flag标记所有CFMap的textSim是否相同
                    //        foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
                    //        {
                    //            if (cfMap.textSim != ((CloneFragmentMapping)cgMap.CFMapList[0]).textSim)
                    //            { textSimSameFlag = false; break; }
                    //        }
                    //        if (textSimSameFlag)
                    //        { cgMap.EvoPattern.CONSISTENTCHANGE = true; }
                    //        else
                    //        { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }
                    //    }
                    //}
                    //#endregion

                    ////若发生了DELETE模式，则必发生INCONSISTENTCHANGE模式
                    //if (cgMap.EvoPattern.DELETE)
                    //{ cgMap.EvoPattern.INCONSISTENTCHANGE = true; }

                    //#region 识别SPLIT模式
                    ////在进行克隆群映射时，已将具有相同源的映射放在相邻的位置，因此只需要检查紧邻当前映射的几个映射即可
                    //int i = this.CGMapList.IndexOf(cgMap);
                    //if (!atGroupFlag[i] && i < this.CGMapList.Count - 1)
                    //{
                    //    //当后面CGMap与当前cgMap同源的时候，继续检查
                    //    while (((CloneGroupMapping)this.CGMapList[i + 1]).srcCGInfo.id == cgMap.srcCGInfo.id)
                    //    {
                    //        if (cgMap.EvoPattern.MapGroupIDs == null)   //默认值（未赋值）为null
                    //        {
                    //            cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
                    //            cgMap.EvoPattern.SPLIT = true;
                    //            atGroupFlag[i] = true;
                    //        }
                    //        cgMap.EvoPattern.MapGroupIDs += ",";
                    //        cgMap.EvoPattern.MapGroupIDs += ((CloneGroupMapping)this.CGMapList[i + 1]).ID;
                    //        i++;
                    //    }
                    //    //将与当前cgMap同源的几个CGMap置为SPLIT状态，并置MapGroupIDs的值
                    //    for (int j = this.CGMapList.IndexOf(cgMap) + 1; j <= i; j++)
                    //    {
                    //        ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.SPLIT = true;
                    //        ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
                    //        atGroupFlag[j] = true;
                    //    }
                    //}
                    //#endregion 
                    #endregion


                    #region 新的进化模式识别方法（按Kim的定义）
                    //识别SAME模式
                    if (cgMap.srcCGInfo.size == cgMap.destCGInfo.size)
                    { cgMap.EvoPattern.SAME = true; }
                    //识别ADD模式
                    if (cgMap.CFMapList.Count < cgMap.destCGInfo.size)
                    { cgMap.EvoPattern.ADD = true; }
                    //识别SUBSTRACT模式
                    if (cgMap.CFMapList.Count < cgMap.srcCGInfo.size)
                    { cgMap.EvoPattern.SUBSTRACT = true; }

                    #region 识别STATIC模式
                    if (cgMap.EvoPattern.SAME)
                    {
                        bool textSameFlag = true;    //标记各个CFMap的文本是否完全相同
                        foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
                        {
                            if (cfMap.textSim < 1)
                            { textSameFlag = false; break; }
                        }
                        if (textSameFlag)
                        { cgMap.EvoPattern.STATIC = true; }
                    }
                    #endregion

                    //若发生了SUBSTRACT模式，则必发生INCONSISTENTCHANGE模式
                    if (cgMap.EvoPattern.SUBSTRACT)
                    { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }

                    #region 识别SPLIT模式
                    //在进行克隆群映射时，已将具有相同源的映射放在相邻的位置，因此只需要检查紧邻当前映射的几个映射即可
                    int i = this.CGMapList.IndexOf(cgMap);
                    if (!atGroupFlag[i] && i < this.CGMapList.Count - 1)
                    {
                        //当后面CGMap与当前cgMap同源的时候，继续检查
                        while (i < this.CGMapList.Count - 1 && ((CloneGroupMapping)this.CGMapList[i + 1]).srcCGInfo.id == cgMap.srcCGInfo.id)
                        {
                            if (cgMap.EvoPattern.MapGroupIDs == null)   //默认值（未赋值）为null
                            {
                                cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
                                cgMap.EvoPattern.SPLIT = true;
                                cgMap.EvoPattern.INCONSISTENTCHANGE = true; //若发生SPLIT模式，则必发生INCONSISTENTCHANGE模式
                                atGroupFlag[i] = true;
                            }
                            cgMap.EvoPattern.MapGroupIDs += ",";
                            cgMap.EvoPattern.MapGroupIDs += ((CloneGroupMapping)this.CGMapList[i + 1]).ID;
                            i++;
                        }
                        //将与当前cgMap同源的几个CGMap置为SPLIT状态，并置MapGroupIDs的值
                        for (int j = this.CGMapList.IndexOf(cgMap) + 1; j <= i; j++)
                        {
                            ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.SPLIT = true;
                            cgMap.EvoPattern.INCONSISTENTCHANGE = true; //若发生SPLIT模式，则必发生INCONSISTENTCHANGE模式
                            ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
                            atGroupFlag[j] = true;
                        }
                    }
                    #endregion

                    //若不符合STATIC，又未发生SUBSTRACT及SPLIT模式，则认为是一致变化
                    if (!cgMap.EvoPattern.STATIC && !cgMap.EvoPattern.SUBSTRACT && !cgMap.EvoPattern.SPLIT)
                    { cgMap.EvoPattern.CONSISTENTCHANGE = true; }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 将映射结果保存到带有-MapOnFunctions/Blocks标签的xml文件
        /// </summary>
        public void SaveMappingToXml()
        {
            this.MapXml = new XmlDocument();

            #region 将映射结果存入Xml文档对象
            XmlElement rootNode = this.MapXml.CreateElement("Maps");  //创建根节点
            this.MapXml.AppendChild(rootNode);

            //添加两个节点，保存源文件及目标文件的名称
            XmlElement srcFileNode = this.MapXml.CreateElement("srcFileName");
            srcFileNode.InnerText = this.srcFileName;
            rootNode.AppendChild(srcFileNode);
            XmlElement destFileNode = this.MapXml.CreateElement("destFileName");
            destFileNode.InnerText = this.destFileName;
            rootNode.AppendChild(destFileNode);

            //添加克隆群映射关系（以CGMap元素表示）
            foreach (CloneGroupMapping cgMap in this.CGMapList)
            {
                #region 添加CGMap元素本身
                XmlElement cgMapNode = this.MapXml.CreateElement("CGMap");
                //添加cgMapid属性
                XmlAttribute attr = this.MapXml.CreateAttribute("cgMapid");
                attr.InnerXml = cgMap.ID;   //属性的InnerXml就代表它的值，与Value相同
                cgMapNode.Attributes.Append(attr);
                //添加srcCGid属性
                attr = this.MapXml.CreateAttribute("srcCGid");
                attr.InnerXml = cgMap.srcCGInfo.id;
                cgMapNode.Attributes.Append(attr);
                //添加srcCGsize属性
                attr = this.MapXml.CreateAttribute("srcCGsize");
                attr.InnerXml = cgMap.srcCGInfo.size.ToString();
                cgMapNode.Attributes.Append(attr);
                //添加destCGid属性
                attr = this.MapXml.CreateAttribute("destCGid");
                attr.InnerXml = ((CGInfo)cgMap.destCGInfo).id;
                cgMapNode.Attributes.Append(attr);
                //添加destCGsize属性
                attr = this.MapXml.CreateAttribute("destCGsize");
                attr.InnerXml = ((CGInfo)cgMap.destCGInfo).size.ToString();
                cgMapNode.Attributes.Append(attr);
                #endregion

                #region 为CGMap元素添加CFMap子元素
                foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
                {
                    XmlElement cfMapNode = this.MapXml.CreateElement("CFMap");
                    //添加cfMapid属性
                    attr = this.MapXml.CreateAttribute("cfMapid");
                    attr.InnerXml = cfMap.ID;
                    cfMapNode.Attributes.Append(attr);
                    //添加srcCFid属性
                    attr = this.MapXml.CreateAttribute("srcCFid");
                    attr.InnerXml = cfMap.SrcCFID;
                    cfMapNode.Attributes.Append(attr);
                    //添加destCFid属性
                    attr = this.MapXml.CreateAttribute("destCFid");
                    attr.InnerXml = cfMap.DestCFID;
                    cfMapNode.Attributes.Append(attr);
                    //添加CRDMatchLevel属性
                    attr = this.MapXml.CreateAttribute("CRDMatchLevel");
                    attr.InnerXml = cfMap.CrdMatchLevel.ToString();
                    cfMapNode.Attributes.Append(attr);
                    //添加textSim属性
                    attr = this.MapXml.CreateAttribute("textSim");
                    attr.InnerXml = cfMap.textSim.ToString();
                    cfMapNode.Attributes.Append(attr);

                    cgMapNode.AppendChild(cfMapNode);
                }
                #endregion

                #region 为CGMap元素添加EvolutionPattern子元素
                XmlElement evoPatternNode = this.MapXml.CreateElement("EvolutionPattern");
                //添加STATIC属性
                attr = this.MapXml.CreateAttribute("STATIC");
                attr.InnerXml = cgMap.EvoPattern.STATIC.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加SAME属性
                attr = this.MapXml.CreateAttribute("SAME");
                attr.InnerXml = cgMap.EvoPattern.SAME.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加ADD属性
                attr = this.MapXml.CreateAttribute("ADD");
                attr.InnerXml = cgMap.EvoPattern.ADD.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加DELETE属性
                attr = this.MapXml.CreateAttribute("DELETE");
                attr.InnerXml = cgMap.EvoPattern.SUBSTRACT.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加CONSISTENTCHANGE属性
                attr = this.MapXml.CreateAttribute("CONSISTENTCHANGE");
                attr.InnerXml = cgMap.EvoPattern.CONSISTENTCHANGE.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加INCONSISTENTCHANGE属性
                attr = this.MapXml.CreateAttribute("INCONSISTENTCHANGE");
                attr.InnerXml = cgMap.EvoPattern.INCONSISTENTCHANGE.ToString();
                evoPatternNode.Attributes.Append(attr);
                //添加SPLIT属性
                attr = this.MapXml.CreateAttribute("SPLIT");
                attr.InnerXml = cgMap.EvoPattern.SPLIT.ToString();
                evoPatternNode.Attributes.Append(attr);
                if (cgMap.EvoPattern.MapGroupIDs != null)
                {
                    //添加MapGroupIDs属性
                    attr = this.MapXml.CreateAttribute("MapGroupIDs");
                    attr.InnerXml = cgMap.EvoPattern.MapGroupIDs;
                    evoPatternNode.Attributes.Append(attr);
                }
                cgMapNode.AppendChild(evoPatternNode);
                #endregion

                rootNode.AppendChild(cgMapNode);
            }

            #region 添加UnMappedSrcCG元素，保存未映射源克隆群列表
            if (this.UnMappedSrcCGList != null)
            {
                XmlElement unmapSrcNode = this.MapXml.CreateElement("UnMappedSrcCG");
                foreach (CGInfo info in this.UnMappedSrcCGList)
                {
                    XmlElement subNode = this.MapXml.CreateElement("CGInfo");
                    //添加id属性
                    XmlAttribute attr = this.MapXml.CreateAttribute("id");
                    attr.InnerXml = info.id;
                    subNode.Attributes.Append(attr);
                    //添加size属性
                    attr = this.MapXml.CreateAttribute("size");
                    attr.InnerXml = info.size.ToString();
                    subNode.Attributes.Append(attr);
                    unmapSrcNode.AppendChild(subNode);
                }
                rootNode.AppendChild(unmapSrcNode);
            }
            #endregion

            #region 添加UnMappedDestCG元素，保存未映射目标克隆群列表
            if (this.UnMappedDestCGList != null)
            {
                XmlElement unmapDestNode = this.MapXml.CreateElement("UnMappedDestCG");
                foreach (CGInfo info in this.UnMappedDestCGList)
                {
                    XmlElement subNode = this.MapXml.CreateElement("CGInfo");
                    //添加id属性
                    XmlAttribute attr = this.MapXml.CreateAttribute("id");
                    attr.InnerXml = info.id;
                    subNode.Attributes.Append(attr);
                    //添加size属性
                    attr = this.MapXml.CreateAttribute("size");
                    attr.InnerXml = info.size.ToString();
                    subNode.Attributes.Append(attr);
                    unmapDestNode.AppendChild(subNode);
                }
                rootNode.AppendChild(unmapDestNode);
            }
            #endregion

            #endregion

            #region 写入*-MapOnFunctions/Blocks.xml文件，保存至指定文件夹
            //新建MAPFiles文件夹，用于存放.map文件。保存文件夹路径到MapFileDir静态属性
            AdjacentVersionMapping.MapFileDir = CloneRegionDescriptor.CrdDir.Replace("CRDFiles", "MAPFiles");
            DirectoryInfo mapDir = new DirectoryInfo(AdjacentVersionMapping.MapFileDir);
            mapDir.Create();

            string fileName;
            DirectoryInfo mapSubDir;
            if (this.mapGranularity == GranularityType.FUNCTIONS)
            {
                mapSubDir = new DirectoryInfo(AdjacentVersionMapping.MapFileDir + @"\functions");   //建立blocks子文件夹
                mapSubDir.Create();
                fileName = this.srcSysName + "_" + this.destSysName + "-MapOnFunctions.xml";
            }
            else
            {
                mapSubDir = new DirectoryInfo(AdjacentVersionMapping.MapFileDir + @"\blocks");
                mapSubDir.Create();
                fileName = this.srcSysName + "_" + this.destSysName + "-MapOnBlocks.xml";
            }

            XmlTextWriter writer = new XmlTextWriter(mapSubDir + "\\" + fileName, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            try
            {
                this.MapXml.Save(writer);
            }
            catch (XmlException ee)
            {
                MessageBox.Show("Save MAP file failed! " + ee.Message);
            }
            writer.Close();
            #endregion
        }

        public void ReadMappingFromXml(XmlDocument mapFile)
        {
            this.CGMapList = new MappingList();
            this.UnMappedSrcCGList = new MappingList();
            this.UnMappedDestCGList = new MappingList();

            this.srcFileName = "";
            this.destFileName = "";
            this.srcSysName = "";
            this.destSysName = "";
            this.SrcCrdFile = new XmlDocument();
            this.DestCrdFile = new XmlDocument();

            this.MapXml = mapFile;

            foreach (XmlElement mapEle in mapFile.DocumentElement.ChildNodes)
            {
                if (mapFile.Name == "CGMap")
                {

                }
                else if (mapFile.Name == "UnMappedSrcCG")
                { }
                else//mapFile.Name == "UnMappedDestCG"
                { }
            }
        }

        /// <summary>
        /// 载入含有CRD的xml文件
        /// </summary>
        /// <param name="srcVersion">源系统的crd文件名</param>
        /// <param name="destVersion">目标系统的crd文件名</param>
        public void LoadCrdFile(string srcVersion, string destVersion)
        {
            Global.mainForm.GetCRDDirFromTreeView1();   //获取当前CRD文件夹的路径
            string srcCrdPath;
            string destCrdPath;
            if (this.mapGranularity == GranularityType.BLOCKS)
            {
                srcCrdPath = CloneRegionDescriptor.CrdDir + @"\blocks\" + srcVersion;   //获取crd文件的完整路径
                destCrdPath = CloneRegionDescriptor.CrdDir + @"\blocks\" + destVersion;
            }
            else
            {
                srcCrdPath = CloneRegionDescriptor.CrdDir + @"\functions\" + srcVersion;
                destCrdPath = CloneRegionDescriptor.CrdDir + @"\functions\" + destVersion;
            }
            this.SrcCrdFile = new XmlDocument();
            this.DestCrdFile = new XmlDocument();
            this.SrcCrdFile.Load(srcCrdPath);   //加载两个版本的crd文件
            this.DestCrdFile.Load(destCrdPath);
        }

        #region 采用CRD+LocationOverlap的IsCGMatch方法
        /// <summary>
        /// 根据CRD匹配级别及位置覆盖率，辅以文本相似度，判断两个CG是否构成映射
        /// </summary>
        /// <param name="srcCG"></param>
        /// <param name="destCG"></param>
        /// <param name="matchLevelThres"></param>
        /// <param name="counter">用来统计计算文本相似度的次数</param>
        /// <returns></returns>
        private bool IsCGMatch(XmlElement srcCG, XmlElement destCG, CRDMatchLevel matchLevelThres, ref int counter)
        {
            //分别获取两个克隆群中克隆片段的CRD元素列表
            //括号内是XPath表达式，表示当前节点下的名为CloneRegionDescriptor的任意节点
            XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptor");
            XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptor");
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            //指定覆盖率阈值
            float overlapTh = -1;

            foreach (XmlElement srcNode in srcCGCrdList)
            {
                srcCrd.CreateCRDFromCRDNode(srcNode);
                foreach (XmlElement destNode in destCGCrdList)
                {
                    destCrd.CreateCRDFromCRDNode(destNode);
                    CRDMatchLevel matchLevel = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);
                    if (matchLevel >= matchLevelThres)
                    {
                        if (matchLevelThres >= CRDMatchLevel.METHODNAMEMATCH)
                        {
                            if (matchLevelThres == CRDMatchLevel.METHODINFOMATCH)
                            { overlapTh = CloneRegionDescriptor.locationOverlap1; }
                            else if (matchLevelThres == CRDMatchLevel.METHODNAMEMATCH)
                            { overlapTh = CloneRegionDescriptor.locationOverlap2; }

                            float overlap = CloneRegionDescriptor.GetLocationOverlap(srcCrd, destCrd);  //计算overlap
                            if (overlap >= overlapTh)
                            { return true; }
                            else//当位置覆盖率不能判断时，使用文本相似度
                            {
                                float textSim = CloneRegionDescriptor.GetTextSimilarity(srcCrd, destCrd, true);
                                counter++;
                                if (textSim >= CloneRegionDescriptor.defaultTextSimTh)
                                { return true; }
                            }
                        }
                        if (matchLevelThres == CRDMatchLevel.FILECLASSMATCH)    //在FILECLASSMATCH级别上直接使用文本相似度
                        {
                            float textSim = CloneRegionDescriptor.GetTextSimilarity(srcCrd, destCrd, true);
                            counter++;
                            if (textSim >= CloneRegionDescriptor.defaultTextSimTh)
                            { return true; }
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        /// <summary>
        /// 计算两个CG的最大matchLevel（即其中CF的matchLevel的最大值）
        /// </summary>
        /// <param name="srcCG"></param>
        /// <param name="destCG"></param>
        /// <returns></returns>
        public CRDMatchLevel GetCGMaxMatchLevel(XmlElement srcCG, XmlElement destCG)
        {
            //分别获取两个克隆群中克隆片段的CRD元素列表
            //括号内是XPath表达式，表示当前节点下的名为CloneRegionDescriptor的任意节点
            XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptor");
            XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptor");
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            //CRDMatchLevel[,] matchMatrix = new CRDMatchLevel[srcCGCrdList.Count, destCGCrdList.Count];
            CRDMatchLevel maxMatchLevel = 0;
            foreach (XmlElement srcNode in srcCGCrdList)
            {
                srcCrd.CreateCRDFromCRDNode(srcNode);
                foreach (XmlElement destNode in destCGCrdList)
                {
                    destCrd.CreateCRDFromCRDNode(destNode);
                    CRDMatchLevel matchLevel = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);
                    if (matchLevel > maxMatchLevel)
                    { maxMatchLevel = matchLevel; }
                }
            }
            return maxMatchLevel;
        }

        /// <summary>
        /// 计算两个CG间的最大文本相似度
        /// </summary>
        /// <param name="srcCG"></param>
        /// <param name="destCG"></param>
        /// <returns></returns>
        public float GetCGMaxTextSimilarity(XmlElement srcCG, XmlElement destCG)
        {
            XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptor");
            XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptor");
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            float maxTextSim = CloneRegionDescriptor.defaultTextSimTh;
            foreach (XmlElement srcNode in srcCGCrdList)
            {
                srcCrd.CreateCRDFromCRDNode(srcNode);
                foreach (XmlElement destNode in destCGCrdList)
                {
                    destCrd.CreateCRDFromCRDNode(destNode);
                    float textSim = CloneRegionDescriptor.GetTextSimilarity(srcCrd, destCrd, true);
                    if (textSim > maxTextSim)
                    { maxTextSim = textSim; }
                }
            }
            return maxTextSim;
        }

        /// <summary>
        /// 判断两个crd节点代表的源代码是否文本匹配
        /// </summary>
        /// <param name="srcNode"></param>
        /// <param name="destNode"></param>
        /// <returns></returns>
        public bool IsCRDNodeTextSimilar(XmlElement srcNode, XmlElement destNode)
        {
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            srcCrd.CreateCRDFromCRDNode(srcNode);
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            srcCrd.CreateCRDFromCRDNode(destNode);
            CRDMatchLevel level = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);
            if (level != CRDMatchLevel.DIFFERENT)
            { return true; }
            else
            { return false; }
        }

        /// <summary>
        /// 根据映射文件名获取xml文档对象
        /// </summary>
        /// <param name="mapFile">映射文件名（不含路径）</param>
        /// <returns></returns>
        public static XmlDocument GetXmlDocument(string mapFile)
        {
            string subFile = mapFile.IndexOf("MapOnBlocks") != -1 ? "blocks" : "functions"; //确定子文件夹
            string fileName = AdjacentVersionMapping.MapFileDir + "\\" + subFile + "\\" + mapFile;
            XmlDocument xmlDoc = new XmlDocument();
            try
            { xmlDoc.Load(fileName); }
            catch (Exception ee)
            {
                MessageBox.Show("Get Map file failed! " + ee.Message);
                xmlDoc = null;
            }
            return xmlDoc;
        }

        /// <summary>
        /// 获得映射文件集。参数为true/false，获取blocks/functions子文件夹下的文件
        /// </summary>
        /// <param name="onBlocksFlag"></param>
        /// <returns></returns>
        public static List<string> GetMapFileCollection(bool onBlocksFlag)
        {
            Global.mainForm.GetMAPDirFromTreeView1();
            List<string> mapFileCollection = null;
            DirectoryInfo dir;
            if (onBlocksFlag)
            { dir = new DirectoryInfo(AdjacentVersionMapping.MapFileDir + @"\blocks"); }
            else
            { dir = new DirectoryInfo(AdjacentVersionMapping.MapFileDir + @"\functions"); }
            FileInfo[] mapFiles = null;
            try
            { mapFiles = dir.GetFiles(); }
            catch (Exception ee)
            { MessageBox.Show("Get Map files failed! " + ee.Message); }
            if (mapFiles != null)
            {
                mapFileCollection = new List<string>();
                foreach (FileInfo info in mapFiles)
                {
                    if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
                    { continue; }
                    else
                    {
                        mapFileCollection.Add(info.Name);
                    }
                }
            }
            return mapFileCollection;
        }
    }

    //class CloneFragmentMappingforMetric : Mapping
    //{
    //    public string SrcCFID { get; set; }
    //    public string DestCFID { get; set; }
    //    //保存映射的两个CF的CRD，是为了在识别进化模式时，计算textSim（因为在MapCF中不计算）
    //    public CloneRegionDescriptorforMetric srcCrd { get; set; }
    //    public CloneRegionDescriptorforMetric destCrd { get; set; }

    //    internal CRDMatchLevel crdMatchLevel;
    //    public CRDMatchLevel CrdMatchLevel //新增属性，用来保存两个CF的CRD匹配级别
    //    {
    //        get { return crdMatchLevel; }
    //    }

    //    //public float overlap; //新增成员，用来保存两个CF的位置覆盖率

    //    public float textSim;  //两段源代码的文本相似度，从diff信息中获得
    //    //保存两段克隆代码的位置信息，用于计算diff时读取。sourceInfos[0]为源信息，sourceInfos[1]为目标信息
    //    public CloneSourceInfo[] sourceInfos = new CloneSourceInfo[2];
    //    //public Diff.DiffInfo diffInfo;  //保存两段源代码diff信息的对象（考虑不保存diff信息，而是在需要时再计算，否则过于浪费空间）
    //}
    //class CloneGroupMappingforMetric : Mapping
    //{
    //    public EvolutionPattern EvoPattern;    //保存进化模式信息
    //    public MappingList CFMapList { get; set; }  //保存CF映射的列表
    //    public CGInfo srcCGInfo;    //源克隆群信息
    //    public CGInfo destCGInfo;   //目标克隆群信息

    //    /// <summary>
    //    /// 为以确定匹配的两个CG创建一个映射关系对象
    //    /// </summary>
    //    /// <param name="srcClassEle">表示源克隆群的XmlElement对象</param>
    //    /// <param name="destClassEle">表示目标克隆群的XmlElement对象</param>
    //    /// <param name="mapCount">引用参数，传入及传入当前映射的序号</param>
    //    public void CreateCGMapping(XmlElement srcClassEle, XmlElement destClassEle, ref int mapCount)
    //    {
    //        this.srcCGInfo.id = srcClassEle.GetAttribute("classid");
    //        this.srcCGInfo.size = srcClassEle.ChildNodes.Count;
    //        //构造目标CG信息结构，加入destCGInfoList成员
    //        this.destCGInfo.id = destClassEle.GetAttribute("classid");
    //        this.destCGInfo.size = destClassEle.ChildNodes.Count;
    //        //映射克隆群内的克隆片段映射，结果保存到CFMapList成员中
    //        this.CFMapList = MapCF(srcClassEle, destClassEle);
    //        this.ID = (++mapCount).ToString();
    //    }

    //    /// <summary>
    //    /// 将已匹配的克隆群(CG)中的克隆片段(CF)进行匹配（修改的MapCF算法，不计算textSim）
    //    /// </summary>
    //    /// <param name="srcCG"></param>
    //    /// <param name="destCG"></param>
    //    /// <returns></returns>

    //    #region 计算textSim的MapCF方法
    //    internal MappingList MapCF(XmlElement srcCG, XmlElement destCG)
    //    {
    //        MappingList cfMappingList = new MappingList();
    //        //分别获取两个克隆群中克隆片段的CRD元素列表
    //        XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        CloneRegionDescriptorforMetric srcCrd = new CloneRegionDescriptorforMetric();
    //        CloneRegionDescriptorforMetric destCrd = new CloneRegionDescriptorforMetric();
    //        int i, j;
    //        //建立两个标记数组保存srcCF和destCF的映射情况
    //        bool[] srcCFMapped = new bool[srcCGCrdList.Count];
    //        for (i = 0; i < srcCGCrdList.Count; i++)
    //        { srcCFMapped[i] = false; }
    //        bool[] destCFMapped = new bool[destCGCrdList.Count];
    //        for (j = 0; j < destCGCrdList.Count; j++)
    //        { destCFMapped[j] = false; }

    //        #region 开始映射
    //        if (srcCGCrdList != null && destCGCrdList != null && srcCGCrdList.Count != 0 && destCGCrdList.Count != 0)
    //        {
    //            //建立矩阵保存CRDMatch结果
    //            CRDMatchLevel[,] crdMatchMatrix = new CRDMatchLevel[srcCGCrdList.Count, destCGCrdList.Count];
    //            //建立矩阵保存textSim结果
    //            float[,] textSimMatrix = new float[srcCGCrdList.Count, destCGCrdList.Count];

    //            i = -1;
    //            int mapCount = 0;

    //            #region 第一步，为每对srcCF与destCF计算textSim及CRDMatchLevel
    //            foreach (XmlElement srcNode in srcCGCrdList)
    //            {
    //                srcCrd.CreateCRDFromCRDNode(srcNode);
    //                i++;
    //                j = -1;
    //                foreach (XmlElement destNode in destCGCrdList)
    //                {
    //                    j++;
    //                    if (!destCFMapped[j])
    //                    {
    //                        destCrd.CreateCRDFromCRDNode(destNode);
    //                        CRDMatchLevel matchLevel = CloneRegionDescriptorforMetric.GetCRDMatchLevel(srcCrd, destCrd);
    //                        crdMatchMatrix[i, j] = matchLevel;
    //                        textSimMatrix[i, j] = CloneRegionDescriptorforMetric.GetTextSimilarity(srcCrd, destCrd, true); //最后一个参数指定是否忽略空行
    //                    }
    //                }
    //            }
    //            #endregion

    //            #region 第二步，根据textSim及CRDMatchLevel，共同确定CF映射
    //            for (i = 0; i < srcCGCrdList.Count; i++)
    //            {
    //                //跳过已映射的srcCF
    //                if (!srcCFMapped[i])
    //                {
    //                    int maxTextSimIndex = -1;   //用于记录最大的textSim对应的destCF索引
    //                    int maxMatchLevelIndex = -1;    //用于记录最高的CRDMatchLevel对应的destCF的索引
    //                    float maxTextSim = CloneRegionDescriptorforMetric.defaultTextSimTh;  //用于记录最大的textSim值，下限为阈值
    //                    CRDMatchLevel maxMatchLevel = CRDMatchLevel.DIFFERENT;
    //                    for (j = 0; j < destCGCrdList.Count; j++)
    //                    {
    //                        if (!destCFMapped[j])
    //                        {
    //                            if (textSimMatrix[i, j] > maxTextSim)   //获取最大的textSim及索引
    //                            { maxTextSim = textSimMatrix[i, j]; maxTextSimIndex = j; }
    //                            if (crdMatchMatrix[i, j] > maxMatchLevel)   //获取最高的CRDMatchLevel及索引
    //                            { maxMatchLevel = crdMatchMatrix[i, j]; maxMatchLevelIndex = j; }
    //                        }
    //                        else
    //                        { continue; }
    //                    }
    //                    if (maxTextSimIndex > -1 && maxMatchLevelIndex > -1)  //如果找到
    //                    {
    //                        int finalIndex;
    //                        if (maxTextSimIndex == maxMatchLevelIndex)  //两个最大在同一个destCF上，则创建映射
    //                        {
    //                            finalIndex = maxTextSimIndex;
    //                        }
    //                        else if (crdMatchMatrix[i, maxTextSimIndex] < maxMatchLevel)
    //                        {
    //                            finalIndex = maxMatchLevelIndex;
    //                        }
    //                        else if (textSimMatrix[i, maxMatchLevelIndex] < maxTextSim)
    //                        {
    //                            finalIndex = maxTextSimIndex;
    //                        }
    //                        else//当根据textSim和CRDMatchLevel仍无法确定时，选index最接近i的一个
    //                        {
    //                            if (Math.Abs(maxTextSimIndex - i) < Math.Abs(maxMatchLevelIndex - i))
    //                            { finalIndex = maxTextSimIndex; }
    //                            else
    //                            { finalIndex = maxMatchLevelIndex; }
    //                        }
    //                        CloneFragmentMappingforMetric cfMapping = new CloneFragmentMappingforMetric();
    //                        cfMapping.ID = (++mapCount).ToString();
    //                        cfMapping.SrcCFID = (i + 1).ToString();
    //                        cfMapping.DestCFID = (finalIndex + 1).ToString();
    //                        cfMapping.textSim = textSimMatrix[i, finalIndex];
    //                        cfMapping.crdMatchLevel = crdMatchMatrix[i, finalIndex];
    //                        cfMappingList.Add(cfMapping);
    //                        srcCFMapped[i] = true;
    //                        destCFMapped[finalIndex] = true;
    //                    }
    //                }
    //            }
    //            #endregion
    //        }
    //        #endregion

    //        return cfMappingList;
    //    }
    //    #endregion

    //    //根据映射文件中的CGMap节点创建CloneGroupMapping对象
    //    public void CreateCGMapppingFromCGMapNode(XmlElement cgMapElement)
    //    {
    //        if (cgMapElement.Name != "CGMap")
    //        {
    //            MessageBox.Show("Create CGMapping failed!");
    //            return;
    //        }
    //        //获取源CG及目标CG信息
    //        this.ID = cgMapElement.GetAttribute("cgMapid");
    //        this.srcCGInfo.id = cgMapElement.GetAttribute("srcCGid");
    //        Int32.TryParse(cgMapElement.GetAttribute("srcCGsize"), out this.srcCGInfo.size);
    //        this.destCGInfo.id = cgMapElement.GetAttribute("destCGid");
    //        Int32.TryParse(cgMapElement.GetAttribute("destCGsize"), out this.destCGInfo.size);
    //        //为EvoPattern成员赋值
    //        XmlElement evoPatternEle = (XmlElement)cgMapElement.SelectSingleNode("EvolutionPattern");
    //        this.EvoPattern.STATIC = evoPatternEle.GetAttribute("STATIC") == "True" ? true : false;
    //        this.EvoPattern.SAME = evoPatternEle.GetAttribute("SAME") == "True" ? true : false;
    //        this.EvoPattern.ADD = evoPatternEle.GetAttribute("ADD") == "True" ? true : false;
    //        this.EvoPattern.SUBSTRACT = evoPatternEle.GetAttribute("DELETE") == "True" ? true : false;
    //        this.EvoPattern.CONSISTENTCHANGE = evoPatternEle.GetAttribute("CONSISTENTCHANGE") == "True" ? true : false;
    //        this.EvoPattern.INCONSISTENTCHANGE = evoPatternEle.GetAttribute("INCONSISTENTCHANGE") == "True" ? true : false;
    //        this.EvoPattern.SPLIT = evoPatternEle.GetAttribute("SPLIT") == "True" ? true : false;
    //        this.EvoPattern.MapGroupIDs = evoPatternEle.GetAttribute("MapGroupIDs");
    //        //为CFMapList成员赋值
    //        CloneFragmentMappingforMetric cfMap = new CloneFragmentMappingforMetric();
    //        this.CFMapList = new MappingList();
    //        foreach (XmlElement cfMapNode in cgMapElement.SelectNodes("CFMap"))
    //        {
    //            cfMap.ID = cfMapNode.GetAttribute("cfMapid");
    //            cfMap.SrcCFID = cfMapNode.GetAttribute("srcCFid");
    //            cfMap.DestCFID = cfMapNode.GetAttribute("destCFid");
    //            cfMap.textSim = float.Parse(cfMapNode.GetAttribute("textSim"));
    //            cfMap.sourceInfos = null;   //源代码信息从map文件中无法获取
    //            this.CFMapList.Add(cfMap);
    //        }
    //    }
    //}
    //class AdjacentVersionMappingforMetric : Mapping
    //{
    //    public static string MapFileDir { get; set; }  //保存存放map文件的文件夹的路径
    //    public string FilePath { get; set; }    //保存映射文件(.xml)的完整路径
    //    public XmlDocument MapXml { get; set; } //存放完整映射关系的Xml文档对象
    //    public GranularityType mapGranularity;  //映射的粒度functions或blocks（取决于CRD文件是functions还是blocks）

    //    //三个列表，存储已映射克隆群对和未映射克隆群信息
    //    public MappingList CGMapList { get; set; }  //保存CG映射的列表
    //    public MappingList UnMappedSrcCGList { get; set; } //保存源系统中未找到目标的CG
    //    public MappingList UnMappedDestCGList { get; set; } //保存目标系统中未找到源的CG（新产生的CG）
    //    //源系统和目标系统的信息（名称及crd文件）
    //    public string srcFileName;  //用于映射的源文件（-withCRD.xml文件）名
    //    public string destFileName;
    //    public string srcSysName;  //包括系统名称+版本信息，在OnStartMapping方法中得到值
    //    public string destSysName;

    //    public XmlDocument SrcCrdFile { get; set; }
    //    public XmlDocument DestCrdFile { get; set; }

    //    /// <summary>
    //    /// MapSettingFinished事件的响应函数，MapSettingFinished事件在点击"Map"按钮时被触发
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    public void OnStartMapping(object sender, CGMapEventArgs e)
    //    {
    //        if (e.srcStr.IndexOf("_blocks-") != -1 && e.destStr.IndexOf("_blocks-") != -1)
    //        {
    //            //保存CRD文件名称（带相对路径）
    //            this.srcFileName = e.srcStr;
    //            this.destFileName = e.destStr;
    //            //从crd文件名中提取系统名称
    //            this.srcSysName = e.srcStr.Substring(0, e.srcStr.IndexOf("_blocks-"));
    //            this.destSysName = e.destStr.Substring(0, e.destStr.IndexOf("_blocks-"));
    //            //MapOnBlocks(e.srcStr, e.destStr);
    //            this.mapGranularity = GranularityType.BLOCKS;
    //            Stopwatch sw = new Stopwatch();
    //            sw.Start();
    //            MapBetweenVersions(e.srcStr, e.destStr);
    //            sw.Stop();
    //            RecognizeEvolutionPattern();    //识别进化模式
    //            SaveMappingToXml();
    //            Global.mainForm.RefreshTreeView1(); //因为生成了新的文件，因此刷新主窗口treeview1
    //            if (Global.mappingVersionRange == "ADJACENTVERSIONS")   //当只进行相邻版本映射时，弹出此窗口（表示映射已完成）
    //            {
    //                MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Blocks level Finished! Time Cost:" + sw.ElapsedMilliseconds.ToString() + "ms.");
    //                if (Global.MapState == 0)   //置映射状态
    //                { Global.MapState = 1; }
    //            }
    //        }
    //        else if (e.srcStr.IndexOf("_functions-") != -1 && e.destStr.IndexOf("_functions-") != -1)
    //        {
    //            this.srcFileName = e.srcStr;
    //            this.destFileName = e.destStr;
    //            this.srcSysName = e.srcStr.Substring(0, e.srcStr.IndexOf("_functions-"));
    //            this.destSysName = e.destStr.Substring(0, e.destStr.IndexOf("_functions-"));
    //            this.mapGranularity = GranularityType.FUNCTIONS;
    //            Stopwatch sw = new Stopwatch();
    //            sw.Start();
    //            MapBetweenVersions(e.srcStr, e.destStr);
    //            RecognizeEvolutionPattern();
    //            SaveMappingToXml();
    //            sw.Stop();
    //            Global.mainForm.RefreshTreeView1();
    //            if (Global.mappingVersionRange == "ADJACENTVERSIONS")
    //            {
    //                MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Functions level Finished! Time Cost:" + sw.ElapsedMilliseconds.ToString() + "ms.");
    //                if (Global.MapState == 0)
    //                { Global.MapState = 1; }
    //            }
    //        }
    //        else
    //        {
    //            MessageBox.Show("Granularity unmatched!Choose versions that are both on block level or function level!");
    //        }
    //    }

    //    #region 采用CRD+LocationOverlap的Multi-Round算法
    //    public void MapBetweenVersions(string srcVersion, string destVersion)
    //    {
    //        this.LoadCrdFile(srcVersion, destVersion);

    //        XmlElement srcRoot = this.SrcCrdFile.DocumentElement;
    //        XmlElement destRoot = this.DestCrdFile.DocumentElement;
    //        //获取两个xml文件中的class元素列表
    //        XmlNodeList srcCGList, destCGList;
    //        if (srcRoot.SelectNodes("class").Count != 0)
    //        { srcCGList = srcRoot.SelectNodes("class"); }
    //        else { srcCGList = null; }
    //        if (destRoot.SelectNodes("class").Count != 0)
    //        { destCGList = destRoot.SelectNodes("class"); }
    //        else { destCGList = null; }
    //        //如果srcCGList和destCGList中有一个为空，返回
    //        if (srcCGList == null || destCGList == null)
    //        {
    //            this.CGMapList = null;
    //            MessageBox.Show(@"No Mappping At All:The Src/Dest CG Collection is EMPTY!");
    //            return;
    //        }
    //        //否则，开始。
    //        #region 置初始状态
    //        this.CGMapList = new MappingList();
    //        this.UnMappedSrcCGList = null;
    //        this.UnMappedDestCGList = null;
    //        int mapCount = 0; //用于统计CG映射的数量
    //        int srcIndex, destIndex; //用作CG在列表中的索引及循环变量
    //        int[,] cgMatchLevelMatrix = new int[srcCGList.Count, destCGList.Count];
    //        //float[,] cgTextSimMatrix = new float[srcCGList.Count, destCGList.Count];
    //        float[,] cgLocationOverlapMatrix = new float[srcCGList.Count, destCGList.Count];
    //        for (int i = 0; i < srcCGList.Count; i++)
    //        {
    //            for (int j = 0; j < destCGList.Count; j++)
    //            {
    //                cgMatchLevelMatrix[i, j] = -1;   //-1表示未计算matchLevel
    //                cgLocationOverlapMatrix[i, j] = -1;  //-1表示未计算
    //            }
    //        }

    //        bool[] isSrcCGMapped = new bool[srcCGList.Count];   //源克隆群映射标记数组
    //        bool[] isDestCGMapped = new bool[destCGList.Count];   //目标克隆群映射标记数组

    //        for (srcIndex = 0; srcIndex < srcCGList.Count; srcIndex++)
    //        { isSrcCGMapped[srcIndex] = false; }
    //        for (destIndex = 0; destIndex < destCGList.Count; destIndex++)
    //        { isDestCGMapped[destIndex] = false; }
    //        #endregion

    //        Stopwatch sw1 = new Stopwatch();
    //        Stopwatch sw3 = new Stopwatch();
    //        Stopwatch sw2 = new Stopwatch();
    //        Stopwatch sw4 = new Stopwatch();

    //        int counter = 0;    //统计计算文本相似度的次数

    //        sw1.Start();
    //        #region 第一轮映射（在METHODINFOMATCH级别上映射）
    //        srcIndex = -1;
    //        foreach (XmlElement srcClassEle in srcCGList)
    //        {
    //            srcIndex++;  //用于获得当前srcClassEle在srcCGList中的序号
    //            destIndex = -1;
    //            foreach (XmlElement destClassEle in destCGList)
    //            {
    //                destIndex++;
    //                if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODINFOMATCH, ref counter))
    //                {
    //                    //如果两个CG匹配，则构造一个CG映射关系对象，加入映射列表中
    //                    CloneGroupMappingforMetric cgMapping = new CloneGroupMappingforMetric();
    //                    cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
    //                    //将此CG映射关系加入CGMapList中
    //                    this.CGMapList.Add(cgMapping);
    //                    isSrcCGMapped[srcIndex] = true;
    //                    isDestCGMapped[destIndex] = true; //置已映射标记为真
    //                    break;  //源克隆群找到对应的（一个）CG后，不再继续寻找
    //                }
    //                else
    //                { continue; }
    //            }
    //        }
    //        #endregion
    //        sw1.Stop();

    //        sw2.Start();
    //        #region 第二轮映射（在METHODNAMEMATCH级别上映射，与第一轮基本相似）
    //        srcIndex = -1;
    //        foreach (XmlElement srcClassEle in srcCGList)
    //        {
    //            srcIndex++;
    //            //只考察第一轮映射中未被映射的源克隆群
    //            if (!isSrcCGMapped[srcIndex])
    //            {
    //                destIndex = -1;
    //                foreach (XmlElement destClassEle in destCGList)
    //                {
    //                    destIndex++;
    //                    //只考察未被映射的目标克隆群，计算matchlevel，必要时计算textSim
    //                    if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODNAMEMATCH, ref counter))
    //                    {
    //                        CloneGroupMappingforMetric cgMapping = new CloneGroupMappingforMetric();
    //                        cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
    //                        this.CGMapList.Add(cgMapping);
    //                        isSrcCGMapped[srcIndex] = true;
    //                        isDestCGMapped[destIndex] = true;
    //                        break;
    //                    }
    //                    else
    //                    { continue; }
    //                }
    //            }
    //            else
    //            { continue; }
    //        }
    //        #endregion
    //        sw2.Stop();

    //        sw3.Start();
    //        #region 第三轮映射（在FILECLASSMATCH级别上映射，与第一轮基本相似）
    //        srcIndex = -1;
    //        foreach (XmlElement srcClassEle in srcCGList)
    //        {
    //            srcIndex++;
    //            //只考察未被映射的源克隆群
    //            if (!isSrcCGMapped[srcIndex])
    //            {
    //                destIndex = -1;
    //                foreach (XmlElement destClassEle in destCGList)
    //                {
    //                    destIndex++;
    //                    //只考察未被映射的目标克隆群，计算matchlevel，必要时计算textSim
    //                    if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.FILECLASSMATCH, ref counter))
    //                    {
    //                        CloneGroupMappingforMetric cgMapping = new CloneGroupMappingforMetric();
    //                        cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
    //                        this.CGMapList.Add(cgMapping);
    //                        isSrcCGMapped[srcIndex] = true;
    //                        isDestCGMapped[destIndex] = true;
    //                        break;
    //                    }
    //                    else
    //                    { continue; }
    //                }
    //            }
    //            else
    //            { continue; }
    //        }
    //        #endregion
    //        sw3.Stop();

    //        #region 将前三轮后未被映射的源克隆群加入UnMappedSrcCGList
    //        srcIndex = -1;
    //        foreach (XmlElement srcClassEle in srcCGList)
    //        {
    //            srcIndex++;
    //            if (!isSrcCGMapped[srcIndex])
    //            {
    //                if (this.UnMappedSrcCGList == null)
    //                { this.UnMappedSrcCGList = new MappingList(); }
    //                CGInfo info = new CGInfo();
    //                info.id = srcClassEle.GetAttribute("classid");
    //                info.size = srcClassEle.ChildNodes.Count;
    //                this.UnMappedSrcCGList.Add(info);

    //            }
    //        }
    //        #endregion

    //        sw4.Start();
    //        #region 第四轮映射（处理一对多的情况，在METHODNAMEMATCH级别上考查，与前几轮类似，只是从dest到src方向）
    //        destIndex = -1;
    //        foreach (XmlElement destClassEle in destCGList)
    //        {
    //            destIndex++;
    //            //只考察未被映射的destCG
    //            if (!isDestCGMapped[destIndex])
    //            {
    //                srcIndex = -1;
    //                foreach (XmlElement srcClassEle in srcCGList)
    //                {
    //                    srcIndex++;
    //                    //只考察已映射的源克隆群（若不满足，则不进行后面比较）
    //                    if (isSrcCGMapped[srcIndex] && IsCGMatch(srcClassEle, destClassEle, CRDMatchLevel.METHODNAMEMATCH, ref counter))
    //                    {
    //                        //在现存的映射列表中查找srcClassEle所在的映射，创建一个映射插入它后面的位置，使具有统一源的两个映射位置上相邻
    //                        int mapIndex = -1;
    //                        foreach (CloneGroupMappingforMetric cgMap in this.CGMapList)
    //                        {
    //                            mapIndex++;
    //                            if (cgMap.srcCGInfo.id == srcClassEle.GetAttribute("classid"))
    //                            {
    //                                CloneGroupMappingforMetric cgMapping = new CloneGroupMappingforMetric();
    //                                cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
    //                                //将此CG映射关系加入CGMapList中mapIndex+1的位置
    //                                this.CGMapList.Insert(mapIndex + 1, cgMapping);
    //                                isDestCGMapped[destIndex] = true; //置已映射标记为真                                       
    //                                break;
    //                            }
    //                        }
    //                        break;
    //                    }
    //                    else
    //                    { continue; }
    //                }
    //            }
    //        }
    //        #endregion
    //        sw4.Stop();

    //        #region 将未找到源克隆群的目标克隆群加入UnMappedDestCGList
    //        for (destIndex = 0; destIndex < destCGList.Count; destIndex++)
    //        {
    //            if (!isDestCGMapped[destIndex])
    //            {
    //                if (this.UnMappedDestCGList == null)
    //                { this.UnMappedDestCGList = new MappingList(); }
    //                CGInfo info = new CGInfo();
    //                info.id = ((XmlElement)destCGList[destIndex]).GetAttribute("classid");
    //                info.size = ((XmlElement)destCGList[destIndex]).ChildNodes.Count;
    //                this.UnMappedDestCGList.Add(info);
    //            }
    //        }
    //        #endregion

    //    }
    //    #endregion

    //    /// <summary>
    //    /// 识别克隆群的进化模式
    //    /// </summary>
    //    private void RecognizeEvolutionPattern()
    //    {
    //        bool[] atGroupFlag;
    //        if (this.CGMapList.Count > 0)
    //        {
    //            atGroupFlag = new bool[this.CGMapList.Count];
    //            foreach (CloneGroupMappingforMetric cgMap in this.CGMapList)
    //            {
    //                #region 原来的进化模式识别方法（原来的理解，不在用）
    //                ////识别SAME模式
    //                //if (cgMap.srcCGInfo.size == cgMap.destCGInfo.size)
    //                //{ cgMap.EvoPattern.SAME = true; }
    //                ////识别ADD模式
    //                //if (cgMap.CFMapList.Count < cgMap.destCGInfo.size)
    //                //{ cgMap.EvoPattern.ADD = true; }
    //                ////识别DELETE模式
    //                //if (cgMap.CFMapList.Count < cgMap.srcCGInfo.size)
    //                //{ cgMap.EvoPattern.DELETE = true; }

    //                //#region 识别STATIC模式，进而识别CONSISTENTCHANGE模式
    //                //if (cgMap.EvoPattern.SAME)
    //                //{
    //                //    bool textSameFlag = true;    //标记各个CFMap的文本是否完全相同
    //                //    foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
    //                //    {
    //                //        if (cfMap.textSim < 1)
    //                //        { textSameFlag = false; break; }
    //                //    }
    //                //    if (textSameFlag)
    //                //    { cgMap.EvoPattern.STATIC = true; }
    //                //    else
    //                //    {
    //                //        //判断是否是CONSISTENTCHANGE
    //                //        bool textSimSameFlag = true;    //此时flag标记所有CFMap的textSim是否相同
    //                //        foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
    //                //        {
    //                //            if (cfMap.textSim != ((CloneFragmentMapping)cgMap.CFMapList[0]).textSim)
    //                //            { textSimSameFlag = false; break; }
    //                //        }
    //                //        if (textSimSameFlag)
    //                //        { cgMap.EvoPattern.CONSISTENTCHANGE = true; }
    //                //        else
    //                //        { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }
    //                //    }
    //                //}
    //                //#endregion

    //                ////若发生了DELETE模式，则必发生INCONSISTENTCHANGE模式
    //                //if (cgMap.EvoPattern.DELETE)
    //                //{ cgMap.EvoPattern.INCONSISTENTCHANGE = true; }

    //                //#region 识别SPLIT模式
    //                ////在进行克隆群映射时，已将具有相同源的映射放在相邻的位置，因此只需要检查紧邻当前映射的几个映射即可
    //                //int i = this.CGMapList.IndexOf(cgMap);
    //                //if (!atGroupFlag[i] && i < this.CGMapList.Count - 1)
    //                //{
    //                //    //当后面CGMap与当前cgMap同源的时候，继续检查
    //                //    while (((CloneGroupMapping)this.CGMapList[i + 1]).srcCGInfo.id == cgMap.srcCGInfo.id)
    //                //    {
    //                //        if (cgMap.EvoPattern.MapGroupIDs == null)   //默认值（未赋值）为null
    //                //        {
    //                //            cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
    //                //            cgMap.EvoPattern.SPLIT = true;
    //                //            atGroupFlag[i] = true;
    //                //        }
    //                //        cgMap.EvoPattern.MapGroupIDs += ",";
    //                //        cgMap.EvoPattern.MapGroupIDs += ((CloneGroupMapping)this.CGMapList[i + 1]).ID;
    //                //        i++;
    //                //    }
    //                //    //将与当前cgMap同源的几个CGMap置为SPLIT状态，并置MapGroupIDs的值
    //                //    for (int j = this.CGMapList.IndexOf(cgMap) + 1; j <= i; j++)
    //                //    {
    //                //        ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.SPLIT = true;
    //                //        ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
    //                //        atGroupFlag[j] = true;
    //                //    }
    //                //}
    //                //#endregion 
    //                #endregion


    //                #region 新的进化模式识别方法（按Kim的定义）
    //                //识别SAME模式
    //                if (cgMap.srcCGInfo.size == cgMap.destCGInfo.size)
    //                { cgMap.EvoPattern.SAME = true; }
    //                //识别ADD模式
    //                if (cgMap.CFMapList.Count < cgMap.destCGInfo.size)
    //                { cgMap.EvoPattern.ADD = true; }
    //                //识别SUBSTRACT模式
    //                if (cgMap.CFMapList.Count < cgMap.srcCGInfo.size)
    //                { cgMap.EvoPattern.SUBSTRACT = true; }

    //                #region 识别STATIC模式
    //                if (cgMap.EvoPattern.SAME)
    //                {
    //                    bool textSameFlag = true;    //标记各个CFMap的文本是否完全相同
    //                    foreach (CloneFragmentMappingforMetric cfMap in cgMap.CFMapList)
    //                    {
    //                        if (cfMap.textSim < 1)
    //                        { textSameFlag = false; break; }
    //                    }
    //                    if (textSameFlag)
    //                    { cgMap.EvoPattern.STATIC = true; }
    //                }
    //                #endregion

    //                //若发生了SUBSTRACT模式，则必发生INCONSISTENTCHANGE模式
    //                if (cgMap.EvoPattern.SUBSTRACT)
    //                { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }

    //                #region 识别SPLIT模式
    //                //在进行克隆群映射时，已将具有相同源的映射放在相邻的位置，因此只需要检查紧邻当前映射的几个映射即可
    //                int i = this.CGMapList.IndexOf(cgMap);
    //                if (!atGroupFlag[i] && i < this.CGMapList.Count - 1)
    //                {
    //                    //当后面CGMap与当前cgMap同源的时候，继续检查
    //                    while (i < this.CGMapList.Count - 1 && ((CloneGroupMappingforMetric)this.CGMapList[i + 1]).srcCGInfo.id == cgMap.srcCGInfo.id)
    //                    {
    //                        if (cgMap.EvoPattern.MapGroupIDs == null)   //默认值（未赋值）为null
    //                        {
    //                            cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
    //                            cgMap.EvoPattern.SPLIT = true;
    //                            cgMap.EvoPattern.INCONSISTENTCHANGE = true; //若发生SPLIT模式，则必发生INCONSISTENTCHANGE模式
    //                            atGroupFlag[i] = true;
    //                        }
    //                        cgMap.EvoPattern.MapGroupIDs += ",";
    //                        cgMap.EvoPattern.MapGroupIDs += ((CloneGroupMappingforMetric)this.CGMapList[i + 1]).ID;
    //                        i++;
    //                    }
    //                    //将与当前cgMap同源的几个CGMap置为SPLIT状态，并置MapGroupIDs的值
    //                    for (int j = this.CGMapList.IndexOf(cgMap) + 1; j <= i; j++)
    //                    {
    //                        ((CloneGroupMappingforMetric)this.CGMapList[j]).EvoPattern.SPLIT = true;
    //                        cgMap.EvoPattern.INCONSISTENTCHANGE = true; //若发生SPLIT模式，则必发生INCONSISTENTCHANGE模式
    //                        ((CloneGroupMappingforMetric)this.CGMapList[j]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
    //                        atGroupFlag[j] = true;
    //                    }
    //                }
    //                #endregion

    //                //若不符合STATIC，又未发生SUBSTRACT及SPLIT模式，则认为是一致变化
    //                if (!cgMap.EvoPattern.STATIC && !cgMap.EvoPattern.SUBSTRACT && !cgMap.EvoPattern.SPLIT)
    //                { cgMap.EvoPattern.CONSISTENTCHANGE = true; }
    //                #endregion
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 将映射结果保存到带有-MapOnFunctions/Blocks标签的xml文件
    //    /// </summary>
    //    public void SaveMappingToXml()
    //    {
    //        this.MapXml = new XmlDocument();

    //        #region 将映射结果存入Xml文档对象
    //        XmlElement rootNode = this.MapXml.CreateElement("Maps");  //创建根节点
    //        this.MapXml.AppendChild(rootNode);

    //        //添加两个节点，保存源文件及目标文件的名称
    //        XmlElement srcFileNode = this.MapXml.CreateElement("srcFileName");
    //        srcFileNode.InnerText = this.srcFileName;
    //        rootNode.AppendChild(srcFileNode);
    //        XmlElement destFileNode = this.MapXml.CreateElement("destFileName");
    //        destFileNode.InnerText = this.destFileName;
    //        rootNode.AppendChild(destFileNode);

    //        //添加克隆群映射关系（以CGMap元素表示）
    //        foreach (CloneGroupMappingforMetric cgMap in this.CGMapList)
    //        {
    //            #region 添加CGMap元素本身
    //            XmlElement cgMapNode = this.MapXml.CreateElement("CGMap");
    //            //添加cgMapid属性
    //            XmlAttribute attr = this.MapXml.CreateAttribute("cgMapid");
    //            attr.InnerXml = cgMap.ID;   //属性的InnerXml就代表它的值，与Value相同
    //            cgMapNode.Attributes.Append(attr);
    //            //添加srcCGid属性
    //            attr = this.MapXml.CreateAttribute("srcCGid");
    //            attr.InnerXml = cgMap.srcCGInfo.id;
    //            cgMapNode.Attributes.Append(attr);
    //            //添加srcCGsize属性
    //            attr = this.MapXml.CreateAttribute("srcCGsize");
    //            attr.InnerXml = cgMap.srcCGInfo.size.ToString();
    //            cgMapNode.Attributes.Append(attr);
    //            //添加destCGid属性
    //            attr = this.MapXml.CreateAttribute("destCGid");
    //            attr.InnerXml = ((CGInfo)cgMap.destCGInfo).id;
    //            cgMapNode.Attributes.Append(attr);
    //            //添加destCGsize属性
    //            attr = this.MapXml.CreateAttribute("destCGsize");
    //            attr.InnerXml = ((CGInfo)cgMap.destCGInfo).size.ToString();
    //            cgMapNode.Attributes.Append(attr);
    //            #endregion

    //            #region 为CGMap元素添加CFMap子元素
    //            foreach (CloneFragmentMappingforMetric cfMap in cgMap.CFMapList)
    //            {
    //                XmlElement cfMapNode = this.MapXml.CreateElement("CFMap");
    //                //添加cfMapid属性
    //                attr = this.MapXml.CreateAttribute("cfMapid");
    //                attr.InnerXml = cfMap.ID;
    //                cfMapNode.Attributes.Append(attr);
    //                //添加srcCFid属性
    //                attr = this.MapXml.CreateAttribute("srcCFid");
    //                attr.InnerXml = cfMap.SrcCFID;
    //                cfMapNode.Attributes.Append(attr);
    //                //添加destCFid属性
    //                attr = this.MapXml.CreateAttribute("destCFid");
    //                attr.InnerXml = cfMap.DestCFID;
    //                cfMapNode.Attributes.Append(attr);
    //                //添加CRDMatchLevel属性
    //                attr = this.MapXml.CreateAttribute("CRDMatchLevel");
    //                attr.InnerXml = cfMap.CrdMatchLevel.ToString();
    //                cfMapNode.Attributes.Append(attr);
    //                //添加textSim属性
    //                attr = this.MapXml.CreateAttribute("textSim");
    //                attr.InnerXml = cfMap.textSim.ToString();
    //                cfMapNode.Attributes.Append(attr);

    //                cgMapNode.AppendChild(cfMapNode);
    //            }
    //            #endregion

    //            #region 为CGMap元素添加EvolutionPattern子元素
    //            XmlElement evoPatternNode = this.MapXml.CreateElement("EvolutionPattern");
    //            //添加STATIC属性
    //            attr = this.MapXml.CreateAttribute("STATIC");
    //            attr.InnerXml = cgMap.EvoPattern.STATIC.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加SAME属性
    //            attr = this.MapXml.CreateAttribute("SAME");
    //            attr.InnerXml = cgMap.EvoPattern.SAME.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加ADD属性
    //            attr = this.MapXml.CreateAttribute("ADD");
    //            attr.InnerXml = cgMap.EvoPattern.ADD.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加DELETE属性
    //            attr = this.MapXml.CreateAttribute("DELETE");
    //            attr.InnerXml = cgMap.EvoPattern.SUBSTRACT.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加CONSISTENTCHANGE属性
    //            attr = this.MapXml.CreateAttribute("CONSISTENTCHANGE");
    //            attr.InnerXml = cgMap.EvoPattern.CONSISTENTCHANGE.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加INCONSISTENTCHANGE属性
    //            attr = this.MapXml.CreateAttribute("INCONSISTENTCHANGE");
    //            attr.InnerXml = cgMap.EvoPattern.INCONSISTENTCHANGE.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            //添加SPLIT属性
    //            attr = this.MapXml.CreateAttribute("SPLIT");
    //            attr.InnerXml = cgMap.EvoPattern.SPLIT.ToString();
    //            evoPatternNode.Attributes.Append(attr);
    //            if (cgMap.EvoPattern.MapGroupIDs != null)
    //            {
    //                //添加MapGroupIDs属性
    //                attr = this.MapXml.CreateAttribute("MapGroupIDs");
    //                attr.InnerXml = cgMap.EvoPattern.MapGroupIDs;
    //                evoPatternNode.Attributes.Append(attr);
    //            }
    //            cgMapNode.AppendChild(evoPatternNode);
    //            #endregion

    //            rootNode.AppendChild(cgMapNode);
    //        }

    //        #region 添加UnMappedSrcCG元素，保存未映射源克隆群列表
    //        if (this.UnMappedSrcCGList != null)
    //        {
    //            XmlElement unmapSrcNode = this.MapXml.CreateElement("UnMappedSrcCG");
    //            foreach (CGInfo info in this.UnMappedSrcCGList)
    //            {
    //                XmlElement subNode = this.MapXml.CreateElement("CGInfo");
    //                //添加id属性
    //                XmlAttribute attr = this.MapXml.CreateAttribute("id");
    //                attr.InnerXml = info.id;
    //                subNode.Attributes.Append(attr);
    //                //添加size属性
    //                attr = this.MapXml.CreateAttribute("size");
    //                attr.InnerXml = info.size.ToString();
    //                subNode.Attributes.Append(attr);
    //                unmapSrcNode.AppendChild(subNode);
    //            }
    //            rootNode.AppendChild(unmapSrcNode);
    //        }
    //        #endregion

    //        #region 添加UnMappedDestCG元素，保存未映射目标克隆群列表
    //        if (this.UnMappedDestCGList != null)
    //        {
    //            XmlElement unmapDestNode = this.MapXml.CreateElement("UnMappedDestCG");
    //            foreach (CGInfo info in this.UnMappedDestCGList)
    //            {
    //                XmlElement subNode = this.MapXml.CreateElement("CGInfo");
    //                //添加id属性
    //                XmlAttribute attr = this.MapXml.CreateAttribute("id");
    //                attr.InnerXml = info.id;
    //                subNode.Attributes.Append(attr);
    //                //添加size属性
    //                attr = this.MapXml.CreateAttribute("size");
    //                attr.InnerXml = info.size.ToString();
    //                subNode.Attributes.Append(attr);
    //                unmapDestNode.AppendChild(subNode);
    //            }
    //            rootNode.AppendChild(unmapDestNode);
    //        }
    //        #endregion

    //        #endregion

    //        #region 写入*-MapOnFunctions/Blocks.xml文件，保存至指定文件夹
    //        //新建MAPFiles文件夹，用于存放.map文件。保存文件夹路径到MapFileDir静态属性
    //        AdjacentVersionMappingforMetric.MapFileDir = CloneRegionDescriptorforMetric.CrdDir.Replace("emCRDFiles", "emMAPFiles");
    //        DirectoryInfo mapDir = new DirectoryInfo(AdjacentVersionMappingforMetric.MapFileDir);
    //        mapDir.Create();

    //        string fileName;
    //        DirectoryInfo mapSubDir;
    //        if (this.mapGranularity == GranularityType.FUNCTIONS)
    //        {
    //            mapSubDir = new DirectoryInfo(AdjacentVersionMappingforMetric.MapFileDir + @"\functions");   //建立blocks子文件夹
    //            mapSubDir.Create();
    //            fileName = this.srcSysName + "_" + this.destSysName + "-emMapOnFunctions.xml";
    //        }
    //        else
    //        {
    //            mapSubDir = new DirectoryInfo(AdjacentVersionMappingforMetric.MapFileDir + @"\blocks");
    //            mapSubDir.Create();
    //            fileName = this.srcSysName + "_" + this.destSysName + "-emMapOnBlocks.xml";
    //        }

    //        XmlTextWriter writer = new XmlTextWriter(mapSubDir + "\\" + fileName, Encoding.Default);
    //        writer.Formatting = Formatting.Indented;
    //        try
    //        {
    //            this.MapXml.Save(writer);
    //        }
    //        catch (XmlException ee)
    //        {
    //            MessageBox.Show("Save MAP file failed! " + ee.Message);
    //        }
    //        writer.Close();
    //        #endregion
    //    }

    //    public void ReadMappingFromXml(XmlDocument mapFile)
    //    {
    //        this.CGMapList = new MappingList();
    //        this.UnMappedSrcCGList = new MappingList();
    //        this.UnMappedDestCGList = new MappingList();

    //        this.srcFileName = "";
    //        this.destFileName = "";
    //        this.srcSysName = "";
    //        this.destSysName = "";
    //        this.SrcCrdFile = new XmlDocument();
    //        this.DestCrdFile = new XmlDocument();

    //        this.MapXml = mapFile;

    //        foreach (XmlElement mapEle in mapFile.DocumentElement.ChildNodes)
    //        {
    //            if (mapFile.Name == "CGMap")
    //            {

    //            }
    //            else if (mapFile.Name == "UnMappedSrcCG")
    //            { }
    //            else//mapFile.Name == "UnMappedDestCG"
    //            { }
    //        }
    //    }

    //    /// <summary>
    //    /// 载入含有CRD的xml文件
    //    /// </summary>
    //    /// <param name="srcVersion">源系统的crd文件名</param>
    //    /// <param name="destVersion">目标系统的crd文件名</param>
    //    public void LoadCrdFile(string srcVersion, string destVersion)
    //    {
    //        Global.mainForm.GetCRDDirFromTreeView1();   //获取当前CRD文件夹的路径
    //        string srcCrdPath;
    //        string destCrdPath;
    //        if (this.mapGranularity == GranularityType.BLOCKS)
    //        {
    //            srcCrdPath = CloneRegionDescriptorforMetric.CrdDir + @"\blocks\" + srcVersion;   //获取crd文件的完整路径
    //            destCrdPath = CloneRegionDescriptorforMetric.CrdDir + @"\blocks\" + destVersion;
    //        }
    //        else
    //        {
    //            srcCrdPath = CloneRegionDescriptorforMetric.CrdDir + @"\functions\" + srcVersion;
    //            destCrdPath = CloneRegionDescriptorforMetric.CrdDir + @"\functions\" + destVersion;
    //        }
    //        this.SrcCrdFile = new XmlDocument();
    //        this.DestCrdFile = new XmlDocument();
    //        this.SrcCrdFile.Load(srcCrdPath);   //加载两个版本的crd文件
    //        this.DestCrdFile.Load(destCrdPath);
    //    }

    //    #region 采用CRD+LocationOverlap的IsCGMatch方法
    //    /// <summary>
    //    /// 根据CRD匹配级别及位置覆盖率，辅以文本相似度，判断两个CG是否构成映射
    //    /// </summary>
    //    /// <param name="srcCG"></param>
    //    /// <param name="destCG"></param>
    //    /// <param name="matchLevelThres"></param>
    //    /// <param name="counter">用来统计计算文本相似度的次数</param>
    //    /// <returns></returns>
    //    private bool IsCGMatch(XmlElement srcCG, XmlElement destCG, CRDMatchLevel matchLevelThres, ref int counter)
    //    {
    //        //分别获取两个克隆群中克隆片段的CRD元素列表
    //        //括号内是XPath表达式，表示当前节点下的名为CloneRegionDescriptor的任意节点
    //        XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        CloneRegionDescriptorforMetric srcCrd = new CloneRegionDescriptorforMetric();
    //        CloneRegionDescriptorforMetric destCrd = new CloneRegionDescriptorforMetric();
    //        //指定覆盖率阈值
    //        float overlapTh = -1;

    //        foreach (XmlElement srcNode in srcCGCrdList)
    //        {
    //            srcCrd.CreateCRDFromCRDNode(srcNode);
    //            foreach (XmlElement destNode in destCGCrdList)
    //            {
    //                destCrd.CreateCRDFromCRDNode(destNode);
    //                CRDMatchLevel matchLevel = CloneRegionDescriptorforMetric.GetCRDMatchLevel(srcCrd, destCrd);
    //                if (matchLevel >= matchLevelThres)
    //                {
    //                    if (matchLevelThres >= CRDMatchLevel.METHODNAMEMATCH)
    //                    {
    //                        if (matchLevelThres == CRDMatchLevel.METHODINFOMATCH)
    //                        { overlapTh = CloneRegionDescriptorforMetric.locationOverlap1; }
    //                        else if (matchLevelThres == CRDMatchLevel.METHODNAMEMATCH)
    //                        { overlapTh = CloneRegionDescriptorforMetric.locationOverlap2; }

    //                        float overlap = CloneRegionDescriptorforMetric.GetLocationOverlap(srcCrd, destCrd);  //计算overlap
    //                        if (overlap >= overlapTh)
    //                        { return true; }
    //                        else//当位置覆盖率不能判断时，使用文本相似度
    //                        {
    //                            float textSim = CloneRegionDescriptorforMetric.GetTextSimilarity(srcCrd, destCrd, true);
    //                            counter++;
    //                            if (textSim >= CloneRegionDescriptorforMetric.defaultTextSimTh)
    //                            { return true; }
    //                        }
    //                    }
    //                    if (matchLevelThres == CRDMatchLevel.FILECLASSMATCH)    //在FILECLASSMATCH级别上直接使用文本相似度
    //                    {
    //                        float textSim = CloneRegionDescriptorforMetric.GetTextSimilarity(srcCrd, destCrd, true);
    //                        counter++;
    //                        if (textSim >= CloneRegionDescriptorforMetric.defaultTextSimTh)
    //                        { return true; }
    //                    }
    //                }
    //            }
    //        }
    //        return false;
    //    }
    //    #endregion

    //    /// <summary>
    //    /// 计算两个CG的最大matchLevel（即其中CF的matchLevel的最大值）
    //    /// </summary>
    //    /// <param name="srcCG"></param>
    //    /// <param name="destCG"></param>
    //    /// <returns></returns>
    //    public CRDMatchLevel GetCGMaxMatchLevel(XmlElement srcCG, XmlElement destCG)
    //    {
    //        //分别获取两个克隆群中克隆片段的CRD元素列表
    //        //括号内是XPath表达式，表示当前节点下的名为CloneRegionDescriptor的任意节点
    //        XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        CloneRegionDescriptorforMetric srcCrd = new CloneRegionDescriptorforMetric();
    //        CloneRegionDescriptorforMetric destCrd = new CloneRegionDescriptorforMetric();
    //        //CRDMatchLevel[,] matchMatrix = new CRDMatchLevel[srcCGCrdList.Count, destCGCrdList.Count];
    //        CRDMatchLevel maxMatchLevel = 0;
    //        foreach (XmlElement srcNode in srcCGCrdList)
    //        {
    //            srcCrd.CreateCRDFromCRDNode(srcNode);
    //            foreach (XmlElement destNode in destCGCrdList)
    //            {
    //                destCrd.CreateCRDFromCRDNode(destNode);
    //                CRDMatchLevel matchLevel = CloneRegionDescriptorforMetric.GetCRDMatchLevel(srcCrd, destCrd);
    //                if (matchLevel > maxMatchLevel)
    //                { maxMatchLevel = matchLevel; }
    //            }
    //        }
    //        return maxMatchLevel;
    //    }

    //    /// <summary>
    //    /// 计算两个CG间的最大文本相似度
    //    /// </summary>
    //    /// <param name="srcCG"></param>
    //    /// <param name="destCG"></param>
    //    /// <returns></returns>
    //    public float GetCGMaxTextSimilarity(XmlElement srcCG, XmlElement destCG)
    //    {
    //        XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptorforMetric");
    //        CloneRegionDescriptorforMetric srcCrd = new CloneRegionDescriptorforMetric();
    //        CloneRegionDescriptorforMetric destCrd = new CloneRegionDescriptorforMetric();
    //        float maxTextSim = CloneRegionDescriptorforMetric.defaultTextSimTh;
    //        foreach (XmlElement srcNode in srcCGCrdList)
    //        {
    //            srcCrd.CreateCRDFromCRDNode(srcNode);
    //            foreach (XmlElement destNode in destCGCrdList)
    //            {
    //                destCrd.CreateCRDFromCRDNode(destNode);
    //                float textSim = CloneRegionDescriptorforMetric.GetTextSimilarity(srcCrd, destCrd, true);
    //                if (textSim > maxTextSim)
    //                { maxTextSim = textSim; }
    //            }
    //        }
    //        return maxTextSim;
    //    }

    //    /// <summary>
    //    /// 判断两个crd节点代表的源代码是否文本匹配
    //    /// </summary>
    //    /// <param name="srcNode"></param>
    //    /// <param name="destNode"></param>
    //    /// <returns></returns>
    //    public bool IsCRDNodeTextSimilar(XmlElement srcNode, XmlElement destNode)
    //    {
    //        CloneRegionDescriptorforMetric srcCrd = new CloneRegionDescriptorforMetric();
    //        srcCrd.CreateCRDFromCRDNode(srcNode);
    //        CloneRegionDescriptorforMetric destCrd = new CloneRegionDescriptorforMetric();
    //        srcCrd.CreateCRDFromCRDNode(destNode);
    //        CRDMatchLevel level = CloneRegionDescriptorforMetric.GetCRDMatchLevel(srcCrd, destCrd);
    //        if (level != CRDMatchLevel.DIFFERENT)
    //        { return true; }
    //        else
    //        { return false; }
    //    }

    //    /// <summary>
    //    /// 根据映射文件名获取xml文档对象
    //    /// </summary>
    //    /// <param name="mapFile">映射文件名（不含路径）</param>
    //    /// <returns></returns>
    //    public static XmlDocument GetXmlDocument(string mapFile)
    //    {
    //        string subFile = mapFile.IndexOf("emMapOnBlocks") != -1 ? "blocks" : "functions"; //确定子文件夹
    //        string fileName = AdjacentVersionMappingforMetric.MapFileDir + "\\" + subFile + "\\" + mapFile;
    //        XmlDocument xmlDoc = new XmlDocument();
    //        try
    //        { xmlDoc.Load(fileName); }
    //        catch (Exception ee)
    //        {
    //            MessageBox.Show("Get Map file failed! " + ee.Message);
    //            xmlDoc = null;
    //        }
    //        return xmlDoc;
    //    }

    //    /// <summary>
    //    /// 获得映射文件集。参数为true/false，获取blocks/functions子文件夹下的文件
    //    /// </summary>
    //    /// <param name="onBlocksFlag"></param>
    //    /// <returns></returns>
    //    public static List<string> GetMapFileCollection(bool onBlocksFlag)
    //    {
    //        Global.mainForm.GetMAPDirFromTreeView1();
    //        List<string> mapFileCollection = null;
    //        DirectoryInfo dir;
    //        if (onBlocksFlag)
    //        { dir = new DirectoryInfo(AdjacentVersionMappingforMetric.MapFileDir + @"\blocks"); }
    //        else
    //        { dir = new DirectoryInfo(AdjacentVersionMappingforMetric.MapFileDir + @"\functions"); }
    //        FileInfo[] mapFiles = null;
    //        try
    //        { mapFiles = dir.GetFiles(); }
    //        catch (Exception ee)
    //        { MessageBox.Show("Get Map files failed! " + ee.Message); }
    //        if (mapFiles != null)
    //        {
    //            mapFileCollection = new List<string>();
    //            foreach (FileInfo info in mapFiles)
    //            {
    //                if ((info.Attributes & FileAttributes.Hidden) != 0)  //不处理隐藏文件
    //                { continue; }
    //                else
    //                {
    //                    mapFileCollection.Add(info.Name);
    //                }
    //            }
    //        }
    //        return mapFileCollection;
    //    }
    //}
}
