using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Xml;

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
    abstract class  Mapping
    {
        public string ID { get; set; }  //映射关系编号
        //public string Src { get; set; } //映射关系中的源
        //public string Dest { get; set; }//映射关系中的目标
    }    
    class CloneFragmentMapping : Mapping
    {
        public string SrcCFID { get; set; }
        public string DestCFID { get; set; }

        //private CRDMatchLevel crdMatchLevel;
        //public CRDMatchLevel CrdMatchLevel //新增属性，用来保存两个CF的CRD匹配级别
        //{
        //    get { return crdMatchLevel; }
        //}

        public float textSim;  //两段源代码的文本相似度，从diff信息中获得
        //保存两段克隆代码的位置信息，用于计算diff时读取。sourceInfos[0]为源信息，sourceInfos[1]为目标信息
        public CloneSourceInfo[] sourceInfos = new CloneSourceInfo[2];
        //public Diff.DiffInfo diffInfo;  //保存两段源代码diff信息的对象（考虑不保存diff信息，而是在需要时再计算，否则过于浪费空间）
    }    

    internal struct CGInfo   //CG信息结构体，包含CG的ID和大小（包含克隆片段的数量）
    {
        public string id;
        public int size;
    }

    //定义进化模式
    struct EvolutionPattern
    {
        public bool STATIC;    //定义7种进化模式
        public bool SAME;
        public bool ADD;
        public bool DELETE;
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
        /// 将已匹配的克隆群(CG)中的克隆片段(CF)进行匹配
        /// </summary>
        /// <param name="srcCG"></param>
        /// <param name="destCG"></param>
        /// <returns></returns>
        public MappingList MapCF(XmlElement srcCG, XmlElement destCG)
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
            if (srcCGCrdList != null && destCGCrdList != null&&srcCGCrdList.Count!=0&&destCGCrdList.Count!=0)
            {
                //建立矩阵保存CRDMatch结果（已取消）
                //CRDMatchLevel[,] crdMatchMatrix = new CRDMatchLevel[srcCGCrdList.Count,destCGCrdList.Count];
                //建立矩阵保存textSim结果
                float[,] textSimMatrix = new float[srcCGCrdList.Count, destCGCrdList.Count];

                i = -1;
                int mapCount = 0;
                //第一步，为每对srcCF与destCF计算textSim
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
                            //CRDMatchLevel matchLevel = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);

                            //获取克隆片段源代码
                            List<string> srcCode = CloneRegionDescriptor.GetCFSourceFromCRD(srcCrd);
                            List<string> destCode = CloneRegionDescriptor.GetCFSourceFromCRD(destCrd);
                            //保存两段源代码的diff信息及文本相似度
                            Diff.DiffInfo diffInfo = Diff.DiffFiles(srcCode, destCode);
                            textSimMatrix[i, j] = Diff.FileSimilarity(diffInfo, srcCode.Count, destCode.Count, true);  //最后一个参数指定是否忽略空行
                            //若textSim为1，则直接创建映射
                            if (textSimMatrix[i, j] == 1)
                            {
                                CloneFragmentMapping cfMapping = new CloneFragmentMapping();
                                cfMapping.ID = (++mapCount).ToString();
                                cfMapping.SrcCFID = (i + 1).ToString();
                                cfMapping.DestCFID = (j + 1).ToString();
                                cfMapping.textSim = textSimMatrix[i, j];
                                cfMappingList.Add(cfMapping);
                                srcCFMapped[i] = true;
                                destCFMapped[j] = true;
                                break;
                            } 
                        }
                    }
                }
                //第二步，根据textSim，确定CF映射
                for (i = 0; i < srcCGCrdList.Count; i++)
                {
                    //跳过已映射的srcCF
                    if (!srcCFMapped[i])
                    {
                        int maxIndex = -1;   //用于记录最大的textSim对应的destCF索引
                        float maxTextSim = CloneRegionDescriptor.defaultTextSimTh;  //用于记录最大的textSim值，下限为阈值
                        for (j = 0; j < destCGCrdList.Count; j++)
                        {
                            if (!destCFMapped[j])
                            {
                                if (textSimMatrix[i, j] > maxTextSim)
                                { maxTextSim = textSimMatrix[i, j]; maxIndex = j; }
                            }
                            else
                            { continue; }
                        }
                        if (maxIndex > -1)  //如果找到可以匹配的并且textSim最大的destCF，则映射
                        {
                            CloneFragmentMapping cfMapping = new CloneFragmentMapping();
                            cfMapping.ID = (++mapCount).ToString();
                            cfMapping.SrcCFID = (i + 1).ToString();
                            cfMapping.DestCFID = (maxIndex + 1).ToString();
                            cfMapping.textSim = maxTextSim;
                            cfMappingList.Add(cfMapping);
                            srcCFMapped[i] = true;
                            destCFMapped[maxIndex] = true;
                        }
                    }
                }
            }
            #endregion
            //考虑未映射项的问题：某个CF可能映射到了另一个CG中。怎么表示这种情况？？
            #region 未映射项不进行操作
            //for (i = 0; i < srcCGCrdList.Count; i++)
            //{
            //    if (!srcCFMapped[i])
            //    {
            //        CloneFragmentMapping cfMapping = new CloneFragmentMapping();
            //        cfMapping.ID = (++mapCount).ToString();
            //        cfMapping.SrcCFID = (i + 1).ToString();
            //        cfMapping.DestCFID = "";
            //        cfMapping.diffInfo = null;
            //        cfMapping.textSim = 0;
            //        cfMappingList.Add(cfMapping);
            //    }
            //}
            //for (j = 0; j < destCGCrdList.Count; j++)
            //{
            //    if (!destCFMapped[j])
            //    {
            //        CloneFragmentMapping cfMapping = new CloneFragmentMapping();
            //        cfMapping.SrcCFID = null;
            //        cfMapping.DestCFID = (j + 1).ToString();
            //        cfMapping.diffInfo = null;
            //        cfMapping.textSim = 0;
            //        cfMappingList.Add(cfMapping);
            //    }
            //}
            #endregion

            return cfMappingList;
        }
        
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
            Int32.TryParse(cgMapElement.GetAttribute("srcCGsize"),out this.srcCGInfo.size);
            this.destCGInfo.id = cgMapElement.GetAttribute("destCGid");
            Int32.TryParse(cgMapElement.GetAttribute("destCGsize"), out this.destCGInfo.size);
            //为EvoPattern成员赋值
            XmlElement evoPatternEle=(XmlElement)cgMapElement.SelectSingleNode("EvolutionPattern");
            this.EvoPattern.STATIC = evoPatternEle.GetAttribute("STATIC") == "True" ? true : false;
            this.EvoPattern.SAME = evoPatternEle.GetAttribute("SAME") == "True" ? true : false;
            this.EvoPattern.ADD = evoPatternEle.GetAttribute("ADD") == "True" ? true : false;
            this.EvoPattern.DELETE = evoPatternEle.GetAttribute("DELETE") == "True" ? true : false;
            this.EvoPattern.CONSISTENTCHANGE = evoPatternEle.GetAttribute("CONSISTENTCHANGE") == "True" ? true : false;
            this.EvoPattern.INCONSISTENTCHANGE = evoPatternEle.GetAttribute("INCONSISTENTCHANGE") == "True" ? true : false;
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
    class AdjacentVersionMapping:Mapping
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
        //public string srcVersionNo; //保存版本号
        //public string destVersionNo;
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
                MapBetweenVersions(e.srcStr, e.destStr);
                RecognizeEvolutionPattern();    //识别进化模式
                SaveMappingToXml();
                Global.mainForm.RefreshTreeView1(); //因为生成了新的文件，因此刷新主窗口treeview1
                if (Global.mappingVersionRange == "ADJACENTVERSIONS")   //当只进行相邻版本映射时，弹出此窗口（表示映射已完成）
                { 
                    MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Blocks level Finished!");
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
                MapBetweenVersions(e.srcStr, e.destStr);
                RecognizeEvolutionPattern();
                SaveMappingToXml();
                Global.mainForm.RefreshTreeView1();
                if (Global.mappingVersionRange == "ADJACENTVERSIONS")
                { 
                    MessageBox.Show("Mapping between " + this.srcSysName + " and " + this.destSysName + " on Functions level Finished!");
                    if (Global.MapState == 0)
                    { Global.MapState = 1; }
                }
            }
            else
            {
                MessageBox.Show("Granularity unmatched!Choose versions that are both on block level or function level!");
            }
        }

        /// <summary>
        /// 从源版本到目标版本进行克隆群映射（Granularity=functions&blocks）
        /// </summary>
        /// <param name="srcVersion">源系统名称+版本字符串</param>
        /// <param name="destVersion">目标系统名称+版本字符串</param>
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
            //否则，开始。置初始状态
            this.CGMapList = new MappingList(); 
            this.UnMappedSrcCGList = null;
            this.UnMappedDestCGList = null;
            int mapCount = 0; //用于统计CG映射的数量
            int srcIndex,destIndex; //用作CG在列表中的索引及循环变量

            bool[] isSrcCGMapped = new bool[srcCGList.Count];   //源克隆群映射标记数组
            bool[] isDestCGMapped = new bool[destCGList.Count];   //目标克隆群映射标记数组
            //这个检查标记似乎没有作用？？
            //bool[] isDestCGChecked = new bool[destCGList.Count];  //目标克隆群是否为检查过标记数组

            for (srcIndex = 0; srcIndex < srcCGList.Count; srcIndex++)
            { isSrcCGMapped[srcIndex] = false; }
            for (destIndex = 0; destIndex < destCGList.Count; destIndex++)
            { isDestCGMapped[destIndex] = false; }
                
            #region 第一轮映射（从每个源克隆群向目标克隆群映射）
            srcIndex = -1;
            foreach (XmlElement srcClassEle in srcCGList)
            {
                srcIndex++;  //用于获得当前srcClassEle在srcCGList中的序号
                destIndex = -1;
                foreach (XmlElement destClassEle in destCGList)
                {
                    destIndex++;
                    //只考察未被映射的目标克隆群。使用严格的文本相似度（要求方法名相同，参数可以不同）
                    if (!isDestCGMapped[destIndex]&&IsCGMatch(srcClassEle, destClassEle,false))
                    {
                        //如果两个CG匹配，则构造一个CG映射关系对象，加入映射列表中
                        CloneGroupMapping cgMapping = new CloneGroupMapping();
                        cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount); 
                        //将此CG映射关系加入CGMapList中
                        this.CGMapList.Add(cgMapping);
                        isSrcCGMapped[srcIndex] = true;
                        //isDestCGChecked[index] = true;  //置已检查标记为真
                        isDestCGMapped[destIndex] = true; //置已映射标记为真
                        break;  //源克隆群找到对应的（一个）CG后，不再继续寻找
                    }
                    else
                    {
                        //isDestCGChecked[destIndex] = true;  //置已检查标记为真
                        continue; 
                    }
                }
            } 
            #endregion

            #region 第二轮映射（针对第一轮映射中未找到目标的源克隆群，使用 宽松的 文本相似度再次寻找）
            //注：严格的文本相似度，是对两个方法名相同，但参数不同的克隆片段，计算其文本相似度
            //注：宽泛的文本相似度，是对两个方法名不同的克隆片段，计算其文本相似度
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
                        #region 原来的代码
                        //只考察在第一轮映射中未被映射的目标克隆群
                        //if (!isDestCGMapped[destIndex])
                        //{
                            //分别获取两个克隆群中克隆片段的CRD元素列表
                            //XmlNodeList srcCGCrdList = srcClassEle.SelectNodes("CloneRegionDescriptor");
                            //XmlNodeList destCGCrdList = destClassEle.SelectNodes("CloneRegionDescriptor");
                            //foreach (XmlElement srcNode in srcCGCrdList)
                            //{
                            //    foreach (XmlElement destNode in destCGCrdList)
                            //    {
                            //        string srcFileName = ((XmlElement)srcNode.SelectSingleNode("fileName")).InnerText;
                            //        string destFileName = ((XmlElement)destNode.SelectSingleNode("fileName")).InnerText;
                            //        if (Options.Language == ProgLanguage.CSharp || Options.Language == ProgLanguage.Java)
                            //        {
                            //            string srcClassName = ((XmlElement)srcNode.SelectSingleNode("className")).InnerText;
                            //            string destClassName = ((XmlElement)destNode.SelectSingleNode("className")).InnerText;
                            //            if (srcFileName.Equals(destFileName) && srcClassName.Equals(destClassName))
                            //            {
                            //                if (IsCRDNodeTextSimilar(srcNode, destNode))
                            //                {
                            //                    CloneGroupMapping cgMapping = CreateCGMapping(srcNode, destNode, ref mapCount);
                            //                    this.CGMapList.Add(cgMapping);
                            //                    isSrcCGMapped[srcIndex] = true;
                            //                    isDestCGMapped[destIndex] = true;
                            //                    //this.UnMappedSrcCGList.Remove(srcNode); //从未映射列表中移除此源克隆群
                            //                    break;
                            //                }
                            //            }
                            //        }
                            //        else
                            //        {
                            //            //若没有class信息，则只比较fileName
                            //            if (srcFileName.Equals(destFileName))
                            //            {
                            //                if (IsCRDNodeTextSimilar(srcNode, destNode))
                            //                {
                            //                    CloneGroupMapping cgMapping = CreateCGMapping(srcNode, destNode, ref mapCount);
                            //                    this.CGMapList.Add(cgMapping);
                            //                    isSrcCGMapped[srcIndex] = true;
                            //                    isDestCGMapped[destIndex] = true;
                            //                    //this.UnMappedSrcCGList.Remove(srcNode); //从未映射列表中移除此源克隆群
                            //                    break;
                            //                }
                            //            }
                            //        }
                            //    }
                            //    break;
                            //}
                            ////isDestCGChecked[destIndex] = true;
                            //break; 
                        //}
                        //else
                        //{ continue; }
                        #endregion
                        //只考察未被映射的目标克隆群。使用宽泛的文本相似度（方法名，参数都可以不同）（应提高阈值？）
                        if (!isDestCGMapped[destIndex] && IsCGMatch(srcClassEle, destClassEle, true))
                        {
                            CloneGroupMapping cgMapping = new CloneGroupMapping();
                            cgMapping.CreateCGMapping(srcClassEle, destClassEle, ref mapCount);
                            this.CGMapList.Add(cgMapping);
                            isSrcCGMapped[srcIndex] = true;
                            isDestCGMapped[destIndex] = true;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    } 
                }
            }
            #endregion

            #region 将前两轮后未被映射的源克隆群加入UnMappedSrcCGList
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

            #region 第三轮映射（处理一对多的情况）
            //（针对一次映射中未被映射的目标克隆群，由源克隆群向其进行映射。注：直接使用宽松的文本相似）
            //只考察已映射的源克隆群即可
            destIndex = -1;
            foreach (XmlElement destClassEle in destCGList)
            {
                destIndex++;
                //只考察没有被映射的destCG
                if (!isDestCGMapped[destIndex])
                {
                    srcIndex = -1;
                    foreach (XmlElement srcClassEle in srcCGList)
                    {
                        srcIndex++;
                        //只考察已映射的源克隆群（若不满足，则不进行后面比较）
                        if (isSrcCGMapped[srcIndex]&&IsCGMatch(srcClassEle, destClassEle,true))
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
                                    this.CGMapList.Insert(mapIndex+1,cgMapping);
                                    isDestCGMapped[destIndex] = true; //置已映射标记为真                                       
                                    break;
                                }
                            }
                            break;
                        }                       
                    }
                }
            } 
            #endregion

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

        /// <summary>
        /// 识别克隆群的进化模式
        /// </summary>
        private void RecognizeEvolutionPattern()
        {
            //List<string> mapGroupID = new List<string>();    //存放当前CGMap所在同源映射组（仅存放映射的ID）
            //int[] atGroupID;   //其中的值表示该CGMap所在的同源映射组的编号。不在任何组内，则为0。（未使用）
            //int groupID = 0;    //记录当前映射组的编号（未使用）
            bool[] atGroupFlag;
            if (this.CGMapList.Count > 0)
            {
                //atGroupID = new int[this.CGMapList.Count];
                atGroupFlag = new bool[this.CGMapList.Count];
                foreach (CloneGroupMapping cgMap in this.CGMapList)
                {
                    //识别SAME模式
                    if (cgMap.srcCGInfo.size == cgMap.destCGInfo.size)
                    { cgMap.EvoPattern.SAME = true; }
                    //识别ADD模式
                    if (cgMap.CFMapList.Count < cgMap.destCGInfo.size)
                    { cgMap.EvoPattern.ADD = true; }
                    //识别DELETE模式
                    if (cgMap.CFMapList.Count < cgMap.srcCGInfo.size)
                    { cgMap.EvoPattern.DELETE = true; }

                    #region 识别STATIC模式，进而识别CONSISTENTCHANGE模式
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
                        else
                        {
                            //判断是否是CONSISTENTCHANGE
                            bool textSimSameFlag = true;    //此时flag标记所有CFMap的textSim是否相同
                            foreach (CloneFragmentMapping cfMap in cgMap.CFMapList)
                            {
                                if (cfMap.textSim != ((CloneFragmentMapping)cgMap.CFMapList[0]).textSim)
                                { textSimSameFlag = false; break; }
                            }
                            if (textSimSameFlag)
                            { cgMap.EvoPattern.CONSISTENTCHANGE = true; }
                            else
                            { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }
                        }
                    } 
                    #endregion

                    //若发生了DELETE模式，则必发生INCONSISTENTCHANGE模式
                    if (cgMap.EvoPattern.DELETE)
                    { cgMap.EvoPattern.INCONSISTENTCHANGE = true; }

                    #region 识别SPLIT模式
                    #region 扫描所有其他CGMap以确定是否发生SPLIT的方法（不在用）
                    //if (atGroupID[this.CGMapList.IndexOf(cgMap)]==0)   //根据标记判断是否已属于某个映射组
                    //{

                    //    foreach (CloneGroupMapping otherCGMap in this.CGMapList)
                    //    {
                    //        if (otherCGMap.ID != cgMap.ID && otherCGMap.srcCGInfo.id == cgMap.srcCGInfo.id)
                    //        {
                    //            if (atGroupID[this.CGMapList.IndexOf(cgMap)] == 0)
                    //            {
                    //                atGroupID[this.CGMapList.IndexOf(cgMap)] = ++groupID;   //出现新的映射组，groupID加1
                    //                cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
                    //                cgMap.EvoPattern.SPLIT = true;
                    //            }
                    //            atGroupID[this.CGMapList.IndexOf(otherCGMap)] = groupID;
                    //            otherCGMap.EvoPattern.SPLIT = true;
                    //            cgMap.EvoPattern.MapGroupIDs += ",";
                    //            cgMap.EvoPattern.MapGroupIDs += otherCGMap.ID;  //将新的cgMap的ID加入串中
                    //        }
                    //    } 

                    //    for (int i = 0; i < this.CGMapList.Count; i++)  //将同属一组的CGMap的EvoPattern.MapGroupIDs置为相同值
                    //    {
                    //        if (groupID != 0 && atGroupID[i] == groupID && i != this.CGMapList.IndexOf(cgMap))
                    //        {
                    //            ((CloneGroupMapping)this.CGMapList[i]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
                    //        }
                    //    }
                    //}
                    #endregion
                    //在进行克隆群映射时，已将具有相同源的映射放在相邻的位置，因此只需要检查紧邻当前映射的几个映射即可
                    int i = this.CGMapList.IndexOf(cgMap);
                    if (!atGroupFlag[i] && i < this.CGMapList.Count - 1)
                    {
                        //当后面CGMap与当前cgMap同源的时候，继续检查
                        while (((CloneGroupMapping)this.CGMapList[i + 1]).srcCGInfo.id == cgMap.srcCGInfo.id)
                        {
                            if (cgMap.EvoPattern.MapGroupIDs == null)   //默认值（未赋值）为null
                            {
                                cgMap.EvoPattern.MapGroupIDs = cgMap.ID;
                                cgMap.EvoPattern.SPLIT = true;
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
                            ((CloneGroupMapping)this.CGMapList[j]).EvoPattern.MapGroupIDs = cgMap.EvoPattern.MapGroupIDs;
                            atGroupFlag[j] = true;
                        }
                    } 
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
                attr.InnerXml = cgMap.ID ;   //属性的InnerXml就代表它的值，与Value相同
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
                attr.InnerXml = cgMap.EvoPattern.DELETE.ToString();
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
        
        /// <summary>
        /// 由表示CRD的Xml节点构造一个CRD对象
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        //private CloneRegionDescriptor CreateCRDFromCRDNode(XmlElement node)
        //{
        //    if (node.Name != "CloneRegionDescriptor")
        //    {
        //        MessageBox.Show("This is not a CRD node! Please pass the right parameter!");
        //        return null;
        //    }
        //    CloneRegionDescriptor crd = new CloneRegionDescriptor();
        //    crd.FileName = ((XmlElement)node.SelectSingleNode("fileName")).InnerText;
        //    if (node.SelectSingleNode("className") != null)
        //    {
        //        crd.ClassName = ((XmlElement)node.SelectSingleNode("className")).InnerText;
        //    }
        //    XmlElement methodInfoNode = (XmlElement)node.SelectSingleNode("methodInfo");
        //    if (methodInfoNode == null)
        //    { crd.MethodInfo = null; }
        //    else
        //    {
        //        //构造CRD的MethodInfo成员
        //        crd.MethodInfo = new MethodInfoType();
        //        crd.MethodInfo.methodName = ((XmlElement)methodInfoNode.SelectSingleNode("mName")).InnerXml;
        //        XmlNodeList paraList = methodInfoNode.SelectNodes("mPara");
        //        if (paraList == null)
        //        { crd.MethodInfo.mParaTypeList = null; }
        //        else
        //        {   //构造CRD的MethodInfo的参数信息
        //            crd.MethodInfo.mParaTypeList = new ParaTypeList();
        //            crd.MethodInfo.mParaNum = paraList.Count;
        //            foreach (XmlElement para in paraList)
        //            {
        //                crd.MethodInfo.mParaTypeList.Add(para.InnerText);
        //            }
        //        }
        //        XmlNodeList blockInfoList = ((XmlElement)node).SelectNodes("blockInfo");
        //        if (blockInfoList.Count == 0)
        //        { crd.BlockInfoList = null; }
        //        else
        //        {
        //            //构造CRD的BlockInfoList成员
        //            crd.BlockInfoList = new BlockInfoList();
        //            foreach (XmlElement blockInfo in blockInfoList)
        //            {
        //                BlockInfo info = new BlockInfo();
        //                #region 使用switch结构构造info
        //                switch (((XmlElement)blockInfo.SelectSingleNode("bType")).InnerText)
        //                {
        //                    case "IF":
        //                        {
        //                            info.bType = BlockType.IF;
        //                            info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
        //                            break;
        //                        }
        //                    case "ELSE":
        //                        {
        //                            info.bType = BlockType.ELSE; info.anchor = null; break;
        //                        }
        //                    case "SWITCH":
        //                        {
        //                            info.bType = BlockType.SWITCH;
        //                            info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
        //                            break;
        //                        }
        //                    case "FOR":
        //                        {
        //                            info.bType = BlockType.FOR;
        //                            info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
        //                            break;
        //                        }
        //                    case "WHILE":
        //                        {
        //                            info.bType = BlockType.WHILE;
        //                            info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
        //                            break;
        //                        }
        //                    case "DO":
        //                        {
        //                            info.bType = BlockType.DO; info.anchor = null; break;
        //                        }
        //                    case "TRY":
        //                        {
        //                            info.bType = BlockType.TRY; info.anchor = null; break;
        //                        }
        //                    case "CATCH":
        //                        {
        //                            info.bType = BlockType.CATCH; info.anchor = null; break;
        //                        }
        //                    default: break;
        //                }
        //                #endregion
        //                crd.BlockInfoList.Add(info);
        //            }
        //        }
        //    }
        //    crd.StartLine = ((XmlElement)node.ParentNode).GetAttribute("startline");
        //    crd.EndLine = ((XmlElement)node.ParentNode).GetAttribute("endline");
        //    crd.Path = CloneRegionDescriptor.CrdDir + @"\" + crd.FileName;
        //    return crd;
        //}
       
        /// <summary>
        /// 判断两个克隆群是否匹配（只包含CRD完全匹配及除了函数参数不匹配，其他都匹配的情况）
        /// </summary>
        /// <param name="srcClassEle">表示源克隆群的Xml元素</param>
        /// <param name="destClassEle">表示目标克隆群的Xml元素</param>
        /// <param name="broadTextSim">指定是否使用宽泛的文本相似度</param>
        /// <returns></returns>
        private bool IsCGMatch(XmlElement srcCG, XmlElement destCG,bool broadTextSim)
        {
            //分别获取两个克隆群中克隆片段的CRD元素列表
            //括号内是XPath表达式，表示当前节点下的名为CloneRegionDescriptor的任意节点
            XmlNodeList srcCGCrdList = srcCG.SelectNodes(".//CloneRegionDescriptor");
            XmlNodeList destCGCrdList = destCG.SelectNodes(".//CloneRegionDescriptor");
            CloneRegionDescriptor srcCrd = new CloneRegionDescriptor();
            CloneRegionDescriptor destCrd = new CloneRegionDescriptor();
            foreach (XmlElement srcNode in srcCGCrdList)
            {
                srcCrd.CreateCRDFromCRDNode(srcNode);
                foreach (XmlElement destNode in destCGCrdList)
                {
                    destCrd.CreateCRDFromCRDNode(destNode);
                    CRDMatchLevel matchLevel = CloneRegionDescriptor.GetCRDMatchLevel(srcCrd, destCrd);
                    if (broadTextSim)   //宽泛的文本相似度，则允许方法名不相等（即不低于FILECLASSMATCH即可）
                    {
                        if (matchLevel != CRDMatchLevel.DIFFERENT)
                        { return true; }
                        else
                        { continue; }
                    }
                    else
                    {
                        //严格的文本相似度，要求方法名相同，参数可以不同（即不低于METHODNAMEMATCH）
                        if ((matchLevel != CRDMatchLevel.DIFFERENT) && (matchLevel != CRDMatchLevel.FILECLASSMATCH))
                        { return true; }
                        else
                        { continue; }
                    }
                }
            }
            return false;
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
}
