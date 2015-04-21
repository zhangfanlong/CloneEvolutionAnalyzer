using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;   //使用正则表达式功能

namespace CloneEvolutionAnalyzer
{
    #region 各种类型声明
    //BType 块类型
    public enum BlockType
    {
        IF = 0,
        ELSE,
        SWITCH,
        FOR,
        WHILE,
        DO,
        TRY,
        CATCH,
    }
    public struct BlockInfo
    {
        //块的类型
        public BlockType bType;
        //块的标记：分支的条件，循环的终止条件，try和catch块的此项为空
        public string anchor;
    }

    //使用BlockInfoList作为ArrayList的别名，以体现其含义：表示块信息的集合，其中存放的是一组BlockInfo对象
    //使用类继承方式更好些，不采用using语句
    public class BlockInfoList : ArrayList
    {
        //重写Equals方法
        public override bool Equals(object obj)
        {
            BlockInfoList target = obj as BlockInfoList;    //将obj转换成要当前类型
            if (Count != target.Count)
            { return false; }
            for (int i = 0; i < Count; i++)
            {
                if (!this[i].Equals(target[i]))  //注意此处不能用！=
                { return false; }
            }
            //return base.Equals(obj);  //使用此语句需要重写GetHashCode方法
            return true;
        }
    }

    //ParaTypeList类继承自ArrayList类，用于存放方法参数类型列表
    public class ParaTypeList : ArrayList
    {
        //重写Equals方法
        public override bool Equals(object obj)
        {
            ParaTypeList target = obj as ParaTypeList;
            if (this.Count != target.Count)
            { return false; }
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].Equals(target[i]))  //为什么不能用==或!=来判断
                { return false; }
            }
            //return base.Equals(obj);
            return true;
        }
    }

    //方法信息类，没有定义为struct类型，因为会出现代码为CS1612的错误，而且不能赋值为null
    public class MethodInfoType
    {
        public string methodName;  //方法名
        //后两项没有声明为属性，是因为它们在GetArguInfo方法中作为输出参数（属性不能作为输出参数）
        public ParaTypeList mParaTypeList; //参数类型列表，无参时，为null
        public int mParaNum;   //参数个数
        //重写Equals方法
        public override bool Equals(object obj)
        {
            MethodInfoType target = obj as MethodInfoType;
            if (this.methodName != target.methodName)
            { return false; }
            if (this.mParaNum != target.mParaNum)
            { return false; }
            if (!this.mParaTypeList.Equals(target.mParaTypeList))
            { return false; }
            return true;
        }
    }

    //CollaborationMetric（证据度量）部分已删除  

    public struct CloneSourceInfo   //克隆代码信息：文件名，行区间
    {
        public string sourcePath;
        public int startLine;
        public int endLine;
    }
    //衡量两个CRD匹配程度的枚举类型
    public enum CRDMatchLevel
    {
        //文本匹配的意思是相似度高于阈值（Diff类默认值为0.8）
        DIFFERENT = 0,    //完全不同（各层级信息不同，文本不匹配）
        //FILEMATCH,      //文件名相同，无类名或类名不同，文本匹配（删除原因:文件名不同认为不匹配）
        FILECLASSMATCH,     //文件名和类信息都相同，无方法或方法信息不同，文本匹配
        METHODNAMEMATCH,    //文件名，类名，方法名相同，参数不同，块信息不同，文本匹配
        METHODINFOMATCH,    //文件名，类名，方法名，参数都相同，块信息不同，文本匹配
        BLOCKMATCH,     //文件名，类名，方法信息，块信息相同，文本匹配
        //EXACTMATCH,     //各层次完全相同，文本完全相同
    }
    #endregion

    /// <summary>
    /// CloneRegionDescriptor类（简称CRD类），克隆区域描述符，描述克隆代码的位置信息，由所在文件，类，方法和块信息组成。
    /// </summary>
    public class CloneRegionDescriptor
    {
        #region CRD成员
        //注：这些属性不应该有set属性（有时间修改！）
        //克隆代码所在文件名（包括路径信息）
        public string FileName { get; set; }
        //开始行号。注：行号只作为CRD的附加信息。在CRD中加入行区间信息，是为通过CRD查找源代码时方便（不必再去查source元素）
        public string StartLine { get; set; }
        //结束行号
        public string EndLine { get; set; }
        //相对行号，在方法中的行号.（对于不在方法中的克隆代码，此两项为空）
        public string RelStartLine { get; set; }
        public string RelEndLine { get; set; }
        //克隆代码所在类名
        public string ClassName { get; set; }
        //克隆代码所在方法名
        public MethodInfoType MethodInfo { get; set; }
        //克隆代码所在的块信息（当granularity=functions时，此字段为空）
        public BlockInfoList BlockInfoList { get; set; }   //属性名与类名相同是否冲突？？
        //记录生成的.crd文件的路径信息
        public string Path { get; set; }
        public HalsteadMetric halmetric;
        #endregion

        #region 静态成员
        //记录保存crd文件的文件夹路径
        public static string CrdDir { get; set; }
        //静态成员，保存块关键字列表。此列表在静态函数InitBlockKeyWords中初始化
        public static List<string> blockKeyWords;
        //静态成员函数，初始化静态成员变量blockKeyWords
        public static void InitBlockKeyWords()
        {
            blockKeyWords = new List<string>();
            //初始化关键字列表，注意：关键字添加的顺序不能变（对应BlockType类型） 
            blockKeyWords.Add("if");
            blockKeyWords.Add("else");
            blockKeyWords.Add("switch");
            blockKeyWords.Add("for");
            blockKeyWords.Add("while");
            blockKeyWords.Add("foreach");   //针对C#语言而设，对C和Java语言无影响
            blockKeyWords.Add("do");
            blockKeyWords.Add("try");
            blockKeyWords.Add("catch");
        }
        //文本相似度阈值默认值0.8
        public static float defaultTextSimTh = (float)0.7;  //此处0.7的来源是nicad允许近似克隆的阈值为0.7
        //位置覆盖率阈值
        public static float locationOverlap1 = (float)0.5;  //在METHODINFOMATCH情况下使用
        public static float locationOverlap2 = (float)0.7;  //在METHODNAMEMATCH情况下使用
        #endregion

        /// <summary>
        /// 根据source元素构造CRD
        /// </summary>
        /// <param name="sourceElement">XmlElement类型对象</param>
        private void GenerateForCF(XmlElement sourceElement)
        {
            CloneSourceInfo sourceInfo = new CloneSourceInfo();
            //这个判断其实是不必要的，因为合法的克隆代码xml文件，必定具有这三个属性
            if (sourceElement.HasAttribute("file") && sourceElement.HasAttribute("startline") && sourceElement.HasAttribute("endline"))
            {
                sourceInfo.sourcePath = sourceElement.GetAttribute("file");
                this.FileName = sourceElement.GetAttribute("file");   //确定CRD的FileName字段
                //StartLine和EndLine属性不会写入XML中，因此无法保存（只是在CRD作为参数传递时起作用）
                Int32.TryParse(sourceElement.GetAttribute("startline"), out sourceInfo.startLine);
                this.StartLine = sourceInfo.startLine.ToString();
                Int32.TryParse(sourceElement.GetAttribute("endline"), out sourceInfo.endLine);
                this.EndLine = sourceInfo.endLine.ToString();
            }
            // 对于Java或C#文件，直接从文件名中提取类名，确定CRD的ClassName字段；否则，将该字段置null（注：不准确，因为一个文件可能有多个类！！）
            if (Options.Language == ProgLanguage.Java || Options.Language == ProgLanguage.CSharp)
            {
                this.ClassName = GetClassNameFromFileName(this.FileName);
            }
            else
            {
                this.ClassName = null;
            }

            #region 获取克隆片段源代码
            //此处使用GetCFSourceFromSourcInfo，而不用GetCFSourceFromCRD的原因是CRD正在构造中
            //获取目标系统文件夹起始路径
            string subSysStartPath = Global.mainForm.subSysDirectory;
            //绝对路径=起始路径+相对路径
            string subSysPath = subSysStartPath + "\\" + sourceInfo.sourcePath;
            //保存源代码文件的ArrayList对象
            List<string> sourceContent = new List<string>();
            sourceContent = Global.GetFileContent(subSysPath);
            sourceContent = PreProcessing.IngoreComments(sourceContent);    //对源代码进行预处理，去除注释部分
            List<string> cloneFragment = GetCFSourceFromSourcInfo(sourceContent, sourceInfo);
            #endregion
            #region added by edward...for extracting Halstead metrics
            //this.halmetric = new HalsteadMetric();
            //HalsteadMetric.InitHalsteadParam();
            //this.halmetric.GetFromCode(cloneFragment);
            #endregion


            if (IsMethod(cloneFragment))
            {
                this.MethodInfo = GetMethodInfo(cloneFragment);
                this.RelStartLine = "1";    //如果克隆代码本身是方法，则开始行号为1
                this.RelEndLine = (sourceInfo.endLine - sourceInfo.startLine + 1).ToString();   //计算结束行号
                this.BlockInfoList = null;
            }
            else
            {
                this.MethodInfo = null; //先置为空，在GetBlockInfo中可以获得值
                //不是方法时，获取块信息（其中包含获取方法信息，如果有的话）
                this.BlockInfoList = GetBlockInfo(sourceContent, cloneFragment, sourceInfo.startLine, sourceInfo.endLine);
            }
            if (this.MethodInfo == null)    //如果克隆代码不在方法内，则相对位置信息为null
            {
                this.RelStartLine = null;
                this.RelEndLine = null;
            }
        }

        /// <summary>
        /// 根据包含系统克隆群信息的xml文件生成每段克隆代码的CRD（采用xml对象方式处理XML文件）
        /// </summary>
        /// <param name="xmlFile">包含克隆群及克隆代码信息的xml文件</param>
        /// <param name="fileName">xml文件的文件名（因为不能xmlFile中提取，所以要传入）</param>
        public void GenerateForSys(XmlDocument xmlFile, string fileName)
        {
            if (fileName.IndexOf("-classes.xml") == -1)
            {
                MessageBox.Show(fileName + " is not \"-class.xml\" file! Can't Generate CRD for it!");
                return;
            }
            //记录当前处理的克隆代码的粒度
            if (fileName.IndexOf("_blocks-") != -1)
            { Options.Granularity = GranularityType.BLOCKS; }
            if (fileName.IndexOf("_functions-") != -1)
            { Options.Granularity = GranularityType.FUNCTIONS; }

            XmlElement rootElement = xmlFile.DocumentElement;
            //为系统中每个CG所包含的每个CF生成CRD
            foreach (XmlElement firstLevelElement in rootElement.ChildNodes)
            {
                if (firstLevelElement.SelectNodes("source").Count != 0)
                {
                    XmlNodeList elementList = firstLevelElement.SelectNodes("source");
                    foreach (XmlElement srcElement in elementList)
                    {
                        GenerateForCF(srcElement);  //构造source元素的CRD

                        #region 将CRD元素加入XML文件
                        XmlElement crdNode = xmlFile.CreateElement("CloneRegionDescriptor");  //创建<CloneRegionDescriptor>元素
                        //添加fileName子元素
                        XmlElement crdChildNode = xmlFile.CreateElement("fileName");
                        crdChildNode.InnerText = this.FileName;
                        crdNode.AppendChild(crdChildNode);
                        //注：行号信息不在XML中显示，因此不必添加
                        //添加className子元素
                        if (this.ClassName != null)
                        {
                            crdChildNode = xmlFile.CreateElement("className");
                            crdChildNode.InnerText = this.ClassName;
                            crdNode.AppendChild(crdChildNode);
                        }

                        #region added by edward...for extracting Halstead metrics
                        //crdChildNode = xmlFile.CreateElement("UniqueOprator");
                        //crdChildNode.InnerText = this.halmetric.UniOPERATORCount.ToString();
                        //crdNode.AppendChild(crdChildNode);
                        //crdChildNode = xmlFile.CreateElement("UniqueOprand");
                        //crdChildNode.InnerText = this.halmetric.UniOperandCount.ToString();
                        //crdNode.AppendChild(crdChildNode);
                        //crdChildNode = xmlFile.CreateElement("TotalOprator");
                        //crdChildNode.InnerText = this.halmetric.TotalOPERATORCount.ToString();
                        //crdNode.AppendChild(crdChildNode);
                        //crdChildNode = xmlFile.CreateElement("TotalOprand");
                        //crdChildNode.InnerText = this.halmetric.TotalOperandCount.ToString();
                        //crdNode.AppendChild(crdChildNode);
                        #endregion

                        //添加methodInfo子元素
                        if (this.MethodInfo != null)
                        {
                            crdChildNode = xmlFile.CreateElement("methodInfo");
                            //将参数个数信息作为methodInfo元素的属性
                            XmlAttribute mParaNum = xmlFile.CreateAttribute("mParaNum");
                            mParaNum.InnerXml = this.MethodInfo.mParaNum.ToString();   //属性节点的InnerXml就代表它的值
                            crdChildNode.Attributes.Append(mParaNum);
                            //添加方法名子元素
                            XmlElement mInfoChildNode = xmlFile.CreateElement("mName");
                            mInfoChildNode.InnerText = this.MethodInfo.methodName;
                            crdChildNode.AppendChild(mInfoChildNode);
                            //若有参数，添加参数类型
                            if (this.MethodInfo.mParaNum != 0)  //如果有参数
                            {
                                //为每个参数建立一个mPara元素
                                foreach (string type in this.MethodInfo.mParaTypeList)
                                {
                                    mInfoChildNode = xmlFile.CreateElement("mPara");
                                    mInfoChildNode.InnerText = type;
                                    crdChildNode.AppendChild(mInfoChildNode);
                                }
                            }
                            crdNode.AppendChild(crdChildNode);
                        }
                        //添加blockInfoList子元素
                        if (this.BlockInfoList != null)
                        {
                            foreach (BlockInfo bInfo in this.BlockInfoList)
                            {
                                crdChildNode = xmlFile.CreateElement("blockInfo");
                                XmlElement bInfoChildNode = xmlFile.CreateElement("bType");
                                bInfoChildNode.InnerText = bInfo.bType.ToString();
                                crdChildNode.AppendChild(bInfoChildNode);
                                if (bInfo.anchor != null)
                                {
                                    bInfoChildNode = xmlFile.CreateElement("Anchor");
                                    bInfoChildNode.InnerText = bInfo.anchor;
                                    crdChildNode.AppendChild(bInfoChildNode);
                                }
                                crdNode.AppendChild(crdChildNode);
                            }
                        }
                        //添加Location子元素，保存克隆代码的相对位置信息
                        if (this.RelStartLine != null & this.RelEndLine != null)
                        {
                            crdChildNode = xmlFile.CreateElement("Location");
                            XmlAttribute attr = xmlFile.CreateAttribute("relStartLine");
                            attr.InnerXml = this.RelStartLine.ToString();
                            crdChildNode.Attributes.Append(attr);
                            attr = xmlFile.CreateAttribute("relEndLine");
                            attr.InnerXml = this.RelEndLine.ToString();
                            crdChildNode.Attributes.Append(attr);
                            crdNode.AppendChild(crdChildNode);
                        }
                        srcElement.AppendChild(crdNode);
                        #endregion
                    }
                }
            }

            #region 将包含-withCRD.xml文件写入指定文件夹
            string crdFileName = fileName.Replace(@".xml", @"-withCRD.xml");    //修改文件名后缀
            //新建XMLFilesWithCRD文件夹，用于存放_withCRD.xml文件
            DirectoryInfo crdDir = new DirectoryInfo(Global.mainForm._folderPath + @"\CRDFiles");
            crdDir.Create();    //当此文件夹不存在时才创建
            CloneRegionDescriptor.CrdDir = crdDir.FullName; //将XMLFilesWithCRD文件夹的路径存入CrdDir中保存
            DirectoryInfo crdSubDir;
            if (Options.Granularity == GranularityType.BLOCKS)
            { crdSubDir = new DirectoryInfo(CloneRegionDescriptor.CrdDir + @"\blocks"); } //建立blocks子文件夹
            else
            { crdSubDir = new DirectoryInfo(CloneRegionDescriptor.CrdDir + @"\functions"); }
            crdSubDir.Create();
            string crdFullName = crdSubDir.FullName + @"\" + crdFileName;
            XmlTextWriter writer = new XmlTextWriter(crdFullName, Encoding.Default);   //另存文件
            writer.Formatting = Formatting.Indented;
            try
            {
                xmlFile.Save(writer);
            }
            catch (XmlException ee)
            {
                MessageBox.Show("Save CRD file failed! " + ee.Message);
            }
            this.Path = crdFullName;
            writer.Close();
            #endregion
        }

        /// <summary>
        /// 判断克隆代码是否是方法块
        /// </summary>
        /// <param name="code">克隆代码段</param>
        /// <returns>若是，返回true；否则返回false</returns>
        private bool IsMethod(List<string> code)
        {
            CloneRegionDescriptor.InitBlockKeyWords();  //首先初始化块关键字列表
            string firstLines = "";
            int i = 0;
            int indexLBracket;
            do
            {
                firstLines += code[i].ToString();
                indexLBracket = code[i].ToString().IndexOf("{");
                i++;
                if (indexLBracket != -1)    //若出现"{"，根据"{"之前的i行判断是否是函数
                {
                    indexLBracket = firstLines.IndexOf("{");
                    if (indexLBracket < firstLines.Length - 1)
                    {
                        firstLines = firstLines.Remove(indexLBracket + 1);  //去掉"{"后面的部分 
                    }
                    //根据是否同时含有"（"，"）"并且不含块关键字来判断是否为函数
                    if (firstLines.IndexOf("(") != -1 && firstLines.IndexOf(")") != -1)
                    {
                        string headStr;
                        if (firstLines.IndexOf("(") != 0)
                        { headStr = firstLines.Substring(0, firstLines.IndexOf("(")).Trim(); } //截取"("前的串
                        else
                        { return false; }
                        if (!Regex.IsMatch(headStr, @"_|[A-Za-z][A-Za-z0-9_]*")) //判断headStr是否是自定义标识符，若是，继续判断，否则，返回false
                        { return false; }

                        string middleStr;   //保存从最右的")"到"{"之间的串
                        if (firstLines.IndexOf("{") - firstLines.LastIndexOf(")") > 1)
                        {
                            middleStr = firstLines.Substring(firstLines.LastIndexOf(")") + 1, firstLines.IndexOf("{") - firstLines.LastIndexOf(")") - 1);
                        }
                        else
                        { middleStr = ""; }
                        //分别考察headStr和middleStr中是否包含块关键字
                        foreach (string key in CloneRegionDescriptor.blockKeyWords)
                        {
                            if (headStr.IndexOf(key) != -1)
                            {
                                int keyIndexHead, keyIndexTail;  //关键字起止位置
                                keyIndexHead = headStr.IndexOf(key);
                                keyIndexTail = keyIndexHead + key.Length - 1;
                                //判断出现的关键字串是否是自定义标识符的一部分。
                                //使用正则表达式。 "[A-Za-z0-9_]"，正则表达式，匹配单个字母，数字或下划线
                                if ((keyIndexHead == 0 || !Regex.IsMatch(headStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                    (keyIndexTail == headStr.Length - 1 || !Regex.IsMatch(headStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                                { return false; }
                            }
                            if (middleStr != "")
                            {
                                if (middleStr.IndexOf(key) != -1)
                                {
                                    int keyIndexHead, keyIndexTail;  //关键字起止位置
                                    keyIndexHead = middleStr.IndexOf(key);
                                    keyIndexTail = keyIndexHead + key.Length - 1;
                                    if ((keyIndexHead == 0 || !Regex.IsMatch(middleStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                        (keyIndexTail == middleStr.Length - 1 || !Regex.IsMatch(middleStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                                    { return false; }
                                }
                            }
                        }
                        return true;
                        //取")"和"{"之间的部分
                        //string str = firstLines.Substring(firstLines.IndexOf(")") + 1, firstLines.IndexOf("{") - firstLines.IndexOf(")")).Trim();
                        //再判断")"和"{"之间是空白或含有throws单词和Exception串
                        //if (str == "" || str.IndexOf("throws ") != -1 && str.IndexOf("Exception") != -1)
                        //{ return true; }

                        //判断str中是否包含";"，若不包含则返回true，否则返回false
                        //if (str.IndexOf(";") == -1)
                        //{ return true; }
                        //else
                        //{ return false; }
                    }
                    else
                    { return false; }
                }
            } while (i < code.Count);
            return false;
        }

        /// <summary>
        /// 获取克隆代码段所在的方法信息
        /// </summary>
        /// </summary>
        /// <param name="codeClone">代表源代码文件的ArrayList对象</param>
        /// <returns>返回方法信息对象，若提取失败，返回null</returns>
        private MethodInfoType GetMethodInfo(List<string> codeClone)
        {
            MethodInfoType mInfo = new MethodInfoType();

            int indexLParen, indexRParen, indexMName;  //parenthesis，圆括号
            int i;
            for (i = 0; i < codeClone.Count; i++)
            {
                indexLParen = codeClone[i].ToString().IndexOf("(");
                indexRParen = codeClone[i].ToString().IndexOf(")");
                if (indexLParen != -1) //找到方法名后面的"("
                {
                    indexMName = codeClone[i].ToString().Substring(0, indexLParen).TrimEnd().LastIndexOf(" ");
                    #region 提取方法名
                    if (indexMName != -1)    //方法名前有空格
                    {
                        indexMName++;
                        mInfo.methodName = codeClone[i].ToString().Substring(indexMName, indexLParen - indexMName).Trim();
                    }
                    else  //方法名从行顶头开始
                    {
                        mInfo.methodName = codeClone[i].ToString().Substring(0, indexLParen).Trim();
                    }
                    #endregion

                    #region 提取参数信息
                    string arguList = "";
                    if (indexRParen != -1)  //如果本行找到")"
                    {
                        arguList = codeClone[i].ToString().Substring(indexLParen + 1, indexRParen - indexLParen - 1).Trim(Global.charsToTrim);
                        GetArguInfo(arguList, out mInfo.mParaTypeList, out mInfo.mParaNum);
                    }
                    else
                    {
                        int indexComma = codeClone[i].ToString().LastIndexOf(",");
                        if (indexComma != -1 && indexComma > indexLParen)
                        {
                            arguList = codeClone[i].ToString().Substring(indexLParen + 1, indexComma - indexLParen).Trim(Global.charsToTrim);
                        }
                        else
                        { arguList = ""; }
                        int j = i + 1;
                        indexRParen = codeClone[j].ToString().IndexOf(")");
                        while (indexRParen == -1)
                        {
                            indexComma = codeClone[j].ToString().IndexOf(",");
                            if (indexComma != -1)
                            {
                                arguList += codeClone[j].ToString().Substring(0, indexComma + 1).Trim(Global.charsToTrim);
                            }
                            j++;
                            indexRParen = codeClone[j].ToString().IndexOf(")");
                        }
                        arguList += codeClone[j].ToString().Substring(0, indexRParen).Trim(Global.charsToTrim);
                        GetArguInfo(arguList, out mInfo.mParaTypeList, out mInfo.mParaNum);
                    }

                    #endregion
                    break;
                }
                else { continue; }
            }
            return mInfo;
        }

        /// <summary>
        /// 从函数参数字符串中，提取出参数类型和参数个数信息
        /// </summary>
        /// <param name="arguStr">待提取的参数字符串</param>
        /// <param name="mParaTypeList">输出参数：返回类型列表</param>
        /// <param name="mParaNum">输出参数：返回参数个数</param>
        private void GetArguInfo(string arguStr, out ParaTypeList mParaTypeList, out int mParaNum)
        {
            if (arguStr == "" || arguStr.Trim() == "void")  //没有参数
            {
                mParaTypeList = null;
                mParaNum = 0;
            }
            else
            {
                mParaTypeList = new ParaTypeList();
                mParaNum = 0;
                int indexComma = arguStr.IndexOf(",");  //保存'，'的位置
                List<string> arguInfo = new List<string>();
                if (indexComma == -1)   //只有一个参数
                {
                    mParaNum++;
                    arguInfo = GetWords(arguStr);
                    string aType = "";
                    for (int i = 0; i < arguInfo.Count - 1; i++)//当一个参数有多个修饰符时（如out int），使用‘+’连接
                    {
                        aType = aType + " " + arguInfo[i].ToString();
                    }
                    mParaTypeList.Add(aType);
                }
                else
                {
                    string curArguStr, otherArguStr;
                    otherArguStr = arguStr;
                    do
                    {
                        indexComma = otherArguStr.IndexOf(",");
                        if (indexComma != -1)
                        {
                            curArguStr = otherArguStr.Substring(0, indexComma).Trim(Global.charsToTrim);//提取出第一个参数字符串
                            otherArguStr = otherArguStr.Substring(indexComma + 1).Trim();   //余下的参数字符串 
                        }
                        else
                        { curArguStr = otherArguStr; otherArguStr = ""; }
                        arguInfo = GetWords(curArguStr);
                        string aType = "";
                        for (int i = 0; i < arguInfo.Count - 1; i++)
                        {
                            aType = aType + " " + arguInfo[i].ToString();
                        }
                        mParaTypeList.Add(aType);
                        mParaNum++;
                    } while (otherArguStr != "");
                }
            }
        }

        /// <summary>
        /// 从一段字符串中提取出单词
        /// </summary>
        /// <param name="str">待提取的字符串</param>
        /// <returns>返回ArrayList对象的的单词列表</returns>
        public List<string> GetWords(string str)
        {
            if (str == "")
            { return null; }
            else
            {
                List<string> words = new List<string>();
                str = str.Trim(Global.charsToTrim);
                int indexSpace = str.IndexOf(" ");  //空格的位置
                if (indexSpace == -1)   //只有一个单词
                {
                    words.Add(str);
                }
                else
                {
                    string curWord, otherWords;
                    otherWords = str;
                    do
                    {
                        indexSpace = otherWords.IndexOf(" ");
                        if (indexSpace != -1)
                        {
                            curWord = otherWords.Substring(0, indexSpace).Trim();
                            otherWords = otherWords.Substring(indexSpace).Trim();
                        }
                        else
                        { curWord = otherWords; otherWords = ""; }
                        words.Add(curWord);
                    } while (otherWords != "");
                }
                return words;
            }
        }

        /// <summary>
        /// 获取克隆代码段所在的多层块信息
        /// </summary>
        /// <param name="source">代表源代码文件的ArrayList对象</param>
        /// <param name="codeClone">代表克隆代码片段的ArrayList对象</param>
        /// <param name="startLine">克隆代码段在源文件中的起始行号</param>
        /// <param name="endLine">克隆代码段在源文件中的结束行号</param>
        /// <returns>返回多层块信息</returns>
        private BlockInfoList GetBlockInfo(List<string> source, List<string> codeClone, int startLine, int endLine)
        {
            BlockInfoList blockInfoList = new BlockInfoList();

            //从克隆片段第一行开始向前
            int lineIndex = startLine - 1;    //注意：行号从1起，索引从0起
            string curLine = source[lineIndex].ToString();
            //存储克隆片段加入前面若干行后的代码段，这个用于进行IsMethod判断.初始值为codeClone.
            List<string> extendedCodeClone = new List<string>(codeClone);
            int rBracketUnMatched = 0;   //记录未匹配的右括号数量
            bool newLBracket = true;   //标记是否新出现单独的左括号，初始值为true
            string blockHead = ""; //用于保存关键字块头的若干行（即从关键字结束到"{"之间的若干行）
            int lBracketIndex = -1;
            //当没有遇到class关键字时，执行循环体
            while (curLine.IndexOf(" class ") == -1)
            {
                if (lineIndex < startLine - 1) //当前行不是克隆代码第一行时，记录大括号匹配情况
                {
                    if (curLine.IndexOf("}") != -1)
                    { rBracketUnMatched++; newLBracket = false; }
                    //if (curLine.IndexOf("{") != -1&&curLine[curLine.IndexOf("{")-1]!='\"')
                    if (curLine.IndexOf("{") != -1)
                    {
                        if (rBracketUnMatched == 0)
                        { newLBracket = true; }
                        else
                        { newLBracket = false; rBracketUnMatched--; }
                    }
                }
                if (newLBracket)    //只有新出现单个的左括号时，才检查块信息
                {
                    lBracketIndex = lineIndex;  //记录当前"{"的位置（行号-1）

                    blockHead = blockHead.Insert(0, curLine);   //构造块头字符串
                    string keyWord;
                    bool isBlock = false;
                    int i = -1; //i用于获取关键字在列表中的序号，以对应BlockType枚举类型中的值
                    foreach (string key in blockKeyWords)
                    {
                        i++;
                        int keyIndexHead, keyIndexTail;  //关键字起止位置
                        keyIndexHead = curLine.IndexOf(key);
                        if (keyIndexHead != -1)
                        {
                            keyIndexTail = keyIndexHead + key.Length - 1;
                            //判断出现的关键字串是否是自定义标识符的一部分
                            // 使用正则表达式。"[A-Za-z0-9_]"，正则表达式，匹配单个字母，数字或下划线
                            if ((keyIndexHead == 0 || !Regex.IsMatch(curLine[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                (keyIndexTail == curLine.Length - 1 || !Regex.IsMatch(curLine[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                            {
                                keyWord = key;
                                isBlock = true;
                                break;
                            }
                        }
                    }
                    if (isBlock)
                    {
                        BlockInfo bInfo = new BlockInfo();
                        bInfo.bType = (BlockType)i;
                        if (i == 1 || i > 5)  //如果是else,do,try或catch快，anchor为null
                        { bInfo.anchor = null; }
                        else
                        {
                            //获取"（"与"）"之间的表达式
                            int indexHead, indexTail;
                            //当blockHead中未包含"{"时（这种情况只出现在克隆代码第一行），向后寻找"{"，构造完整的blockHead
                            if (blockHead.IndexOf("{") == -1)
                            {
                                int indexBack = lineIndex + 1;
                                while (source[indexBack].IndexOf("{") == -1)
                                { blockHead = blockHead + source[indexBack]; }
                                blockHead = blockHead + source[indexBack];
                            }
                            //截取"{"前的子串，即块的头部
                            blockHead = blockHead.Substring(0, blockHead.IndexOf("{"));
                            indexHead = blockHead.IndexOf("(");
                            indexTail = blockHead.LastIndexOf(")");
                            if (indexHead != -1 && indexTail != -1 && indexTail > indexHead)
                            {
                                bInfo.anchor = blockHead.Substring(indexHead, indexTail - indexHead + 1).ToString().Trim();
                                bInfo.anchor = bInfo.anchor.Replace("\n", " ");
                                bInfo.anchor = bInfo.anchor.Replace("\t", " ");
                            }
                            else
                            { bInfo.anchor = null; }
                        }
                        blockInfoList.Insert(0, bInfo); //将当前层的块信息插入到块信息列表的头部
                        newLBracket = false;    //检查完当前关键字块后，清空newLBracket信息
                        blockHead = ""; //检查完当前关键字块后，清空blockHead信息
                        lBracketIndex = -1; //若"{"是块而不是方法，则清空lBracketIndex
                    }
                }

                lineIndex--;
                curLine = source[lineIndex].ToString();   //读取前一行
                extendedCodeClone.Insert(0, curLine);
                //如果到达方法边界，而且是包含当前克隆片段的方法（通过是否出现新的左大括号来判断），获取方法名，并停止向前扫描
                if (IsMethod(extendedCodeClone))
                {
                    if (curLine.IndexOf("{") != -1)
                    {
                        if (rBracketUnMatched == 0)
                        { newLBracket = true; }
                        else
                        { newLBracket = false; }
                    }
                    if (newLBracket)//如果出现新的左大括号，认为到达包含本克隆片段的方法边界；否则，认为是独立于本段克隆代码的方法
                    {
                        this.MethodInfo = GetMethodInfo(extendedCodeClone);
                        //if (lBracketIndex!=-1)
                        //{
                        this.RelStartLine = (startLine - lineIndex).ToString(); //计算相对行号
                        this.RelEndLine = (endLine - lineIndex).ToString();
                        //}
                        break;
                    }
                }
                //当上面的条件不满足时，继续向前搜索
            }
            //如果到类边界才停止，说明克隆代码不在方法内部（当然本身也不是方法，前面判断过）。第三个条件是排除已获得方法信息，但方法名中包含类名字符串的情况
            //if ((curLine.IndexOf(" class ") != -1 || curLine.IndexOf(this.ClassName) != -1) && this.MethodInfo == null)
            if (curLine.IndexOf(" class ") != -1)    //废弃其他条件，只根据是否出现class关键字来判断
            {
                this.MethodInfo = null;
                blockInfoList = null;
            }
            else if (blockInfoList.Count == 0)   //如果最终没有获得块信息，则置此字段为空
            { blockInfoList = null; }
            return blockInfoList;
        }

        /// <summary>
        /// 从源代码文件中提取克隆代码片段
        /// </summary>
        /// <param name="source">保存源代码的ArrayList对象</param>
        /// <param name="sourceInfo">以文件名和行号表示的克隆代码信息</param>
        /// <returns></returns>
        internal static List<string> GetCFSourceFromSourcInfo(List<string> source, CloneSourceInfo sourceInfo)
        {
            List<string> codeClone = new List<string>();
            for (int i = sourceInfo.startLine - 1; i < sourceInfo.endLine; i++) //注意，索引从0起算，而行号从1起算
            { codeClone.Add(source[i].ToString()); }
            return codeClone;
        }

        /// <summary>
        /// 根据CRD对象信息获取克隆片段源代码
        /// </summary>
        /// <param name="crd"></param>
        /// <returns>ArrayList类型的源代码</returns>
        internal static List<string> GetCFSourceFromCRD(CloneRegionDescriptor crd)
        {
            List<string> fullSource = new List<string>();
            List<string> cloneFragment = new List<string>();
            CloneSourceInfo sourceInfo = new CloneSourceInfo();
            sourceInfo.sourcePath = crd.FileName;
            Int32.TryParse(crd.StartLine, out sourceInfo.startLine);
            Int32.TryParse(crd.EndLine, out sourceInfo.endLine);
            //获取目标系统文件夹起始路径
            string subSysStartPath = Global.mainForm.subSysDirectory;
            //绝对路径=起始路径+相对路径
            string subSysPath = subSysStartPath + "\\" + sourceInfo.sourcePath;
            //保存源代码文件的ArrayList对象
            fullSource = Global.GetFileContent(subSysPath);
            cloneFragment = GetCFSourceFromSourcInfo(fullSource, sourceInfo);
            return cloneFragment;
        }

        /// <summary>
        /// 对于Java或C#文件，本方法直接从文件名中提取类名（设为static是为了在别的类中可以调用）
        /// </summary>
        /// <param name="fileName">带有路径信息的文件名</param>
        /// <returns>返回类名字符串</returns>
        internal static string GetClassNameFromFileName(string fileName)
        {
            string className = null;
            int indexHead, indexTail;
            indexTail = fileName.LastIndexOf(".");
            indexHead = fileName.LastIndexOf("/");
            className = fileName.Substring(indexHead + 1, indexTail - indexHead - 1).Trim();
            return className;
        }

        /// <summary>
        /// 计算两个CRD匹配的程度（只考虑CRD的内容，不考虑文本相似度）
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <returns>返回CRDMatchLevel类型值</returns>
        public static CRDMatchLevel GetCRDMatchLevel(CloneRegionDescriptor src, CloneRegionDescriptor dest)
        {
            //除去文件名字符串中系统名称的部分，如testdnsjava-0-3,testdnsjava-0-4
            string srcFileName = src.FileName.Substring(src.FileName.IndexOf("/"));
            string destFileName = dest.FileName.Substring(dest.FileName.IndexOf("/"));
            //对于特定输入，在如下分支中，只走一条路径
            if (!srcFileName.Equals(destFileName))
            { return CRDMatchLevel.DIFFERENT; } //文件名不同的认为不匹配
            else//文件信息相同
            {
                if ((src.ClassName != null && dest.ClassName != null) && !src.ClassName.Equals(dest.ClassName) || src.ClassName != null && dest.ClassName == null ||
                    src.ClassName == null && dest.ClassName != null)
                { return CRDMatchLevel.DIFFERENT; } //类信息不同的认为不匹配
                else//类信息相同，或两个类信息都为空
                {
                    //如果二者方法信息都为空
                    if (src.MethodInfo == null && dest.MethodInfo == null)
                    { return CRDMatchLevel.FILECLASSMATCH; }
                    //二者中有一个方法信息为空，另一个不为空
                    else if (src.MethodInfo != null && dest.MethodInfo == null || src.MethodInfo == null && dest.MethodInfo != null)
                    { return CRDMatchLevel.DIFFERENT; }
                    //如果二者方法信息都不为空
                    else
                    {
                        //如果方法信息相同（方法名，参数信息都相同），检查块信息
                        if (src.MethodInfo.Equals(dest.MethodInfo))
                        {
                            if (src.BlockInfoList != null && dest.BlockInfoList != null)
                            {
                                if (src.BlockInfoList.Equals(dest.BlockInfoList))//块信息相同
                                { return CRDMatchLevel.BLOCKMATCH; }
                                else//块信息不同
                                { return CRDMatchLevel.METHODINFOMATCH; }
                            }
                            //有一个块信息为空，另一个不为空，或两个块信息都为空（包含Granularity=functions的情况）
                            else
                            { return CRDMatchLevel.METHODINFOMATCH; }

                        }
                        //方法名相同，参数信息不同
                        else if ((src.MethodInfo.methodName == dest.MethodInfo.methodName) &&
                            !src.MethodInfo.mParaTypeList.Equals(dest.MethodInfo.mParaTypeList))
                        { return CRDMatchLevel.METHODNAMEMATCH; }
                        //方法名，参数信息都不同
                        else
                        { return CRDMatchLevel.FILECLASSMATCH; }
                    }
                }
            }
        }

        /// <summary>
        /// 计算两段克隆代码的文本相似度（使用Diff类）
        /// </summary>
        /// <param name="srcCrd">源CRD</param>
        /// <param name="destCrd">目标CRD</param>
        /// <param name="ignoreEmptyLines">指定是否忽略空行</param>
        /// <returns></returns>
        public static float GetTextSimilarity(CloneRegionDescriptor srcCrd, CloneRegionDescriptor destCrd, bool ignoreEmptyLines)
        {
            List<string> srcFileContent = new List<string>();
            List<string> destFileContent = new List<string>();
            List<string> srcFragment = new List<string>();
            List<string> destFragment = new List<string>();
            CloneSourceInfo info = new CloneSourceInfo();

            #region 获得srcCrdNode源代码文本
            info.sourcePath = srcCrd.FileName;  //获得源文件名
            string fullName = Global.mainForm.subSysDirectory + "\\" + info.sourcePath;
            //获取源代码行区间
            Int32.TryParse(srcCrd.StartLine, out info.startLine);
            Int32.TryParse(srcCrd.EndLine, out info.endLine);
            srcFileContent = Global.GetFileContent(fullName);
            srcFragment = CloneRegionDescriptor.GetCFSourceFromSourcInfo(srcFileContent, info);
            #endregion

            #region 获得destCrdNode源代码文本
            info.sourcePath = destCrd.FileName;    //获得源文件名
            fullName = Global.mainForm.subSysDirectory + "\\" + info.sourcePath;
            //获取源代码行区间
            Int32.TryParse(destCrd.StartLine, out info.startLine);
            Int32.TryParse(destCrd.EndLine, out info.endLine);
            destFileContent = Global.GetFileContent(fullName);
            destFragment = CloneRegionDescriptor.GetCFSourceFromSourcInfo(destFileContent, info);
            #endregion

            //使用Diff类计算两段代码的相似度
            Diff.UseDefaultStrSimTh();  //使用行相似度阈值默认值0.5
            Diff.DiffInfo diffFile = Diff.DiffFiles(srcFragment, destFragment);
            float sim = Diff.FileSimilarity(diffFile, srcFragment.Count, destFragment.Count, ignoreEmptyLines);

            return sim;
        }

        /// <summary>
        /// 计算两段克隆代码的位置覆盖率（采用在方法中的相对位置计算）
        /// </summary>
        /// <param name="srcCrd"></param>
        /// <param name="destCrd"></param>
        /// <returns></returns>
        public static float GetLocationOverlap(CloneRegionDescriptor srcCrd, CloneRegionDescriptor destCrd)
        {
            if (srcCrd.RelStartLine == null || srcCrd.RelEndLine == null || destCrd.RelStartLine == null || destCrd.RelEndLine == null)
            { return -1; }
            int startLine1, startLine2, endLine1, endLine2;
            startLine1 = Int32.Parse(srcCrd.RelStartLine);
            endLine1 = Int32.Parse(srcCrd.RelEndLine);
            startLine2 = Int32.Parse(destCrd.RelStartLine);
            endLine2 = Int32.Parse(destCrd.RelEndLine);
            int startLine = startLine1 > startLine2 ? startLine1 : startLine2;  //取startLine中较大的
            int endLine = endLine1 < endLine2 ? endLine1 : endLine2;    //取endLine中较小的
            //计算overLapping
            return (float)(endLine - startLine) / (float)(endLine2 - startLine2);
        }

        /// <summary>
        /// 根据Xml节点构造CRD对象
        /// </summary>
        /// <param name="node"></param>
        public void CreateCRDFromCRDNode(XmlElement node)
        {
            if (node.Name != "CloneRegionDescriptor")
            {
                MessageBox.Show("This is not a CRD node! Please pass the right parameter!");
                return;
            }
            this.FileName = ((XmlElement)node.SelectSingleNode("fileName")).InnerText;
            if (node.SelectSingleNode("className") != null)
            {
                this.ClassName = ((XmlElement)node.SelectSingleNode("className")).InnerText;
            }
            XmlElement methodInfoNode = (XmlElement)node.SelectSingleNode("methodInfo");
            if (methodInfoNode == null)
            { this.MethodInfo = null; }
            else
            {
                //构造CRD的MethodInfo成员
                this.MethodInfo = new MethodInfoType();
                this.MethodInfo.methodName = ((XmlElement)methodInfoNode.SelectSingleNode("mName")).InnerXml;
                XmlNodeList paraList = methodInfoNode.SelectNodes("mPara");
                if (paraList == null)
                { this.MethodInfo.mParaTypeList = null; }
                else
                {   //构造CRD的MethodInfo的参数信息
                    this.MethodInfo.mParaTypeList = new ParaTypeList();
                    this.MethodInfo.mParaNum = paraList.Count;
                    foreach (XmlElement para in paraList)
                    {
                        this.MethodInfo.mParaTypeList.Add(para.InnerText);
                    }
                }
                XmlNodeList blockInfoList = ((XmlElement)node).SelectNodes("blockInfo");
                if (blockInfoList.Count == 0)
                { this.BlockInfoList = null; }
                else
                {
                    //构造CRD的BlockInfoList成员
                    this.BlockInfoList = new BlockInfoList();
                    foreach (XmlElement blockInfo in blockInfoList)
                    {
                        BlockInfo info = new BlockInfo();
                        #region 使用switch结构构造info
                        switch (((XmlElement)blockInfo.SelectSingleNode("bType")).InnerText)
                        {
                            case "IF":
                                {
                                    info.bType = BlockType.IF;
                                    //此判断是为了防止有些anchor未提取出来的情况（比如if的"("和")"不在同一行）
                                    if ((XmlElement)blockInfo.SelectSingleNode("Anchor") != null)
                                    {
                                        info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    }
                                    break;
                                }
                            case "ELSE":
                                {
                                    info.bType = BlockType.ELSE; info.anchor = null; break;
                                }
                            case "SWITCH":
                                {
                                    info.bType = BlockType.SWITCH;
                                    if ((XmlElement)blockInfo.SelectSingleNode("Anchor") != null)
                                    {
                                        info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    }
                                    break;
                                }
                            case "FOR":
                                {
                                    info.bType = BlockType.FOR;
                                    if ((XmlElement)blockInfo.SelectSingleNode("Anchor") != null)
                                    {
                                        info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    }
                                    break;
                                }
                            case "WHILE":
                                {
                                    info.bType = BlockType.WHILE;
                                    if ((XmlElement)blockInfo.SelectSingleNode("Anchor") != null)
                                    {
                                        info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    }
                                    break;
                                }
                            case "DO":
                                {
                                    info.bType = BlockType.DO; info.anchor = null; break;
                                }
                            case "TRY":
                                {
                                    info.bType = BlockType.TRY; info.anchor = null; break;
                                }
                            case "CATCH":
                                {
                                    info.bType = BlockType.CATCH; info.anchor = null; break;
                                }
                            default: break;
                        }
                        #endregion
                        this.BlockInfoList.Add(info);
                    }
                }
            }
            this.StartLine = ((XmlElement)node.ParentNode).GetAttribute("startline");
            this.EndLine = ((XmlElement)node.ParentNode).GetAttribute("endline");
            //构造附加的相对位置信息
            XmlElement locationNode = (XmlElement)node.SelectSingleNode("Location");
            if (locationNode != null)
            {
                this.RelStartLine = locationNode.GetAttribute("relStartLine");
                this.RelEndLine = locationNode.GetAttribute("relEndLine");
            }
            else
            {
                this.RelStartLine = null;
                this.RelEndLine = null;
            }
            if (Options.Granularity == GranularityType.BLOCKS)
            { this.Path = CloneRegionDescriptor.CrdDir + @"\blocks\" + this.FileName; }
            else
            { this.Path = CloneRegionDescriptor.CrdDir + @"\functions\" + this.FileName; }
        }
    }


    /// <summary>
    /// CloneRegionDescriptorforMetric类（简称CRDforMetric类），克隆区域描述符，描述克隆代码的位置信息，由所在文件，类，方法和块信息组成。
    /// </summary>
    public class CloneRegionDescriptorforMetric
    {
        #region CRD成员
        //注：这些属性不应该有set属性（有时间修改！）
        //克隆代码所在文件名（包括路径信息）
        public string FileName { get; set; }
        //开始行号。注：行号只作为CRD的附加信息。在CRD中加入行区间信息，是为通过CRD查找源代码时方便（不必再去查source元素）
        public string StartLine { get; set; }
        //结束行号
        public string EndLine { get; set; }
        //克隆代码所在类名
        public string RelStartLine { get; set; }
        public string RelEndLine { get; set; }
        public string ClassName { get; set; }
        //克隆代码所在方法名
        public MethodInfoType MethodInfo { get; set; }
        //克隆代码所在的块信息（当granularity=functions时，此字段为空）
        public BlockInfoList BlockInfoList { get; set; }   //属性名与类名相同是否冲突？？
        //记录生成的.crd文件的路径信息
        public string Path { get; set; }
        public HalsteadMetric halmetric;
        #endregion

        #region 静态成员
        //记录保存crd文件的文件夹路径
        public static string CrdDir { get; set; }
        //静态成员，保存块关键字列表。此列表在静态函数InitBlockKeyWords中初始化
        public static List<string> blockKeyWords;
        //静态成员函数，初始化静态成员变量blockKeyWords
        public static void InitBlockKeyWords()
        {
            blockKeyWords = new List<string>();
            //初始化关键字列表，注意：关键字添加的顺序不能变（对应BlockType类型） 
            blockKeyWords.Add("if");
            blockKeyWords.Add("else");
            blockKeyWords.Add("switch");
            blockKeyWords.Add("for");
            blockKeyWords.Add("while");
            blockKeyWords.Add("foreach");   //针对C#语言而设，对C和Java语言无影响
            blockKeyWords.Add("do");
            blockKeyWords.Add("try");
            blockKeyWords.Add("catch");
        }
        //文本相似度阈值默认值0.8
        public static float defaultTextSimTh = (float)0.7;
        //位置覆盖率阈值
        public static float locationOverlap1 = (float)0.5;  //在METHODINFOMATCH情况下使用
        public static float locationOverlap2 = (float)0.7;  //在METHODNAMEMATCH情况下使用
        #endregion

        /// <summary>
        /// 根据source元素构造CRD
        /// </summary>
        /// <param name="sourceElement">XmlElement类型对象</param>
        private void GenerateForCF(XmlElement sourceElement)
        {
            CloneSourceInfo sourceInfo = new CloneSourceInfo();
            //这个判断其实是不必要的，因为合法的克隆代码xml文件，必定具有这三个属性
            if (sourceElement.HasAttribute("file") && sourceElement.HasAttribute("startline") && sourceElement.HasAttribute("endline"))
            {
                sourceInfo.sourcePath = sourceElement.GetAttribute("file");
                this.FileName = sourceElement.GetAttribute("file");   //确定CRD的FileName字段
                //StartLine和EndLine属性不会写入XML中，因此无法保存（只是在CRD作为参数传递时起作用）
                Int32.TryParse(sourceElement.GetAttribute("startline"), out sourceInfo.startLine);
                this.StartLine = sourceInfo.startLine.ToString();
                Int32.TryParse(sourceElement.GetAttribute("endline"), out sourceInfo.endLine);
                this.EndLine = sourceInfo.endLine.ToString();
            }
            // 对于Java或C#文件，直接从文件名中提取类名，确定CRD的ClassName字段；否则，将该字段置null（注：不准确，因为一个文件可能有多个类！！）
            if (Options.Language == ProgLanguage.Java || Options.Language == ProgLanguage.CSharp)
            {
                this.ClassName = GetClassNameFromFileName(this.FileName);
            }
            else
            {
                this.ClassName = null;
            }

            #region 获取克隆片段源代码
            //此处使用GetCFSourceFromSourcInfo，而不用GetCFSourceFromCRD的原因是CRD正在构造中
            //获取目标系统文件夹起始路径
            string subSysStartPath = Global.mainForm.subSysDirectory;
            //绝对路径=起始路径+相对路径
            string subSysPath = subSysStartPath + "\\" + sourceInfo.sourcePath;
            //保存源代码文件的ArrayList对象
            List<string> sourceContent = new List<string>();
            sourceContent = Global.GetFileContent(subSysPath);
            sourceContent = PreProcessing.IngoreComments(sourceContent);    //对源代码进行预处理，去除注释部分
            List<string> cloneFragment = GetCFSourceFromSourcInfo(sourceContent, sourceInfo);
            #endregion
            //added by edward...for extracting Halstead metrics
            this.halmetric = new HalsteadMetric();
            HalsteadMetric.InitHalsteadParam();
            this.halmetric.GetFromCode(cloneFragment);


            if (IsMethod(cloneFragment))
            {
                this.MethodInfo = GetMethodInfo(cloneFragment);
                this.RelStartLine = "1";    //如果克隆代码本身是方法，则开始行号为1
                this.RelEndLine = (sourceInfo.endLine - sourceInfo.startLine + 1).ToString();   //计算结束行号
                this.BlockInfoList = null;
            }
            else
            {
                this.MethodInfo = null; //先置为空，在GetBlockInfo中可以获得值
                //不是方法时，获取块信息（其中包含获取方法信息，如果有的话）
                this.BlockInfoList = GetBlockInfo(sourceContent, cloneFragment, sourceInfo.startLine, sourceInfo.endLine);
            }
            if (this.MethodInfo == null)    //如果克隆代码不在方法内，则相对位置信息为null
            {
                this.RelStartLine = null;
                this.RelEndLine = null;
            }
        }

        /// <summary>
        /// 根据包含系统克隆群信息的xml文件生成每段克隆代码的CRD（采用xml对象方式处理XML文件）
        /// </summary>
        /// <param name="xmlFile">包含克隆群及克隆代码信息的xml文件</param>
        /// <param name="fileName">xml文件的文件名（因为不能xmlFile中提取，所以要传入）</param>
        public void GenerateForSys(XmlDocument xmlFile, string fileName)
        {
            if (fileName.IndexOf("-classes.xml") == -1)
            {
                MessageBox.Show(fileName + " is not \"-class.xml\" file! Can't Generate CRD for it!");
                return;
            }
            //记录当前处理的克隆代码的粒度
            if (fileName.IndexOf("_blocks-") != -1)
            { Options.Granularity = GranularityType.BLOCKS; }
            if (fileName.IndexOf("_functions-") != -1)
            { Options.Granularity = GranularityType.FUNCTIONS; }

            XmlElement rootElement = xmlFile.DocumentElement;
            //为系统中每个CG所包含的每个CF生成CRD
            foreach (XmlElement firstLevelElement in rootElement.ChildNodes)
            {
                if (firstLevelElement.SelectNodes("source").Count != 0)
                {
                    XmlNodeList elementList = firstLevelElement.SelectNodes("source");
                    foreach (XmlElement srcElement in elementList)
                    {
                        GenerateForCF(srcElement);  //构造source元素的CRD

                        #region 将CRD元素加入XML文件
                        XmlElement crdNode = xmlFile.CreateElement("CloneRegionDescriptorforMetric");  //创建<CloneRegionDescriptorforMetric>元素
                        //添加fileName子元素
                        XmlElement crdChildNode = xmlFile.CreateElement("fileName");
                        crdChildNode.InnerText = this.FileName;
                        crdNode.AppendChild(crdChildNode);
                        //注：行号信息不在XML中显示，因此不必添加
                        //添加className子元素
                        if (this.ClassName != null)
                        {
                            crdChildNode = xmlFile.CreateElement("className");
                            crdChildNode.InnerText = this.ClassName;
                            crdNode.AppendChild(crdChildNode);
                        }
                        //added by edward...for extracting Halstead metrics
                        crdChildNode = xmlFile.CreateElement("UniqueOprator");
                        crdChildNode.InnerText = this.halmetric.UniOPERATORCount.ToString();
                        crdNode.AppendChild(crdChildNode);
                        crdChildNode = xmlFile.CreateElement("UniqueOprand");
                        crdChildNode.InnerText = this.halmetric.UniOperandCount.ToString();
                        crdNode.AppendChild(crdChildNode);
                        crdChildNode = xmlFile.CreateElement("TotalOprator");
                        crdChildNode.InnerText = this.halmetric.TotalOPERATORCount.ToString();
                        crdNode.AppendChild(crdChildNode);
                        crdChildNode = xmlFile.CreateElement("TotalOprand");
                        crdChildNode.InnerText = this.halmetric.TotalOperandCount.ToString();
                        crdNode.AppendChild(crdChildNode);

                        //添加methodInfo子元素
                        if (this.MethodInfo != null)
                        {
                            crdChildNode = xmlFile.CreateElement("methodInfo");
                            //将参数个数信息作为methodInfo元素的属性
                            XmlAttribute mParaNum = xmlFile.CreateAttribute("mParaNum");
                            mParaNum.InnerXml = this.MethodInfo.mParaNum.ToString();   //属性节点的InnerXml就代表它的值
                            crdChildNode.Attributes.Append(mParaNum);
                            //添加方法名子元素
                            XmlElement mInfoChildNode = xmlFile.CreateElement("mName");
                            mInfoChildNode.InnerText = this.MethodInfo.methodName;
                            crdChildNode.AppendChild(mInfoChildNode);
                            //若有参数，添加参数类型
                            if (this.MethodInfo.mParaNum != 0)  //如果有参数
                            {
                                //为每个参数建立一个mPara元素
                                foreach (string type in this.MethodInfo.mParaTypeList)
                                {
                                    mInfoChildNode = xmlFile.CreateElement("mPara");
                                    mInfoChildNode.InnerText = type;
                                    crdChildNode.AppendChild(mInfoChildNode);
                                }
                            }
                            crdNode.AppendChild(crdChildNode);
                        }
                        //添加blockInfoList子元素
                        if (this.BlockInfoList != null)
                        {
                            foreach (BlockInfo bInfo in this.BlockInfoList)
                            {
                                crdChildNode = xmlFile.CreateElement("blockInfo");
                                XmlElement bInfoChildNode = xmlFile.CreateElement("bType");
                                bInfoChildNode.InnerText = bInfo.bType.ToString();
                                crdChildNode.AppendChild(bInfoChildNode);
                                if (bInfo.anchor != null)
                                {
                                    bInfoChildNode = xmlFile.CreateElement("Anchor");
                                    bInfoChildNode.InnerText = bInfo.anchor;
                                    crdChildNode.AppendChild(bInfoChildNode);
                                }
                                crdNode.AppendChild(crdChildNode);
                            }
                        }
                        //添加Location子元素，保存克隆代码的相对位置信息
                        if (this.RelStartLine != null & this.RelEndLine != null)
                        {
                            crdChildNode = xmlFile.CreateElement("Location");
                            XmlAttribute attr = xmlFile.CreateAttribute("relStartLine");
                            attr.InnerXml = this.RelStartLine.ToString();
                            crdChildNode.Attributes.Append(attr);
                            attr = xmlFile.CreateAttribute("relEndLine");
                            attr.InnerXml = this.RelEndLine.ToString();
                            crdChildNode.Attributes.Append(attr);
                            crdNode.AppendChild(crdChildNode);
                        }
                        srcElement.AppendChild(crdNode);
                        #endregion
                    }
                }
            }

            #region 将包含-emCRD.xml文件写入指定文件夹
            string crdFileName = fileName.Replace(@".xml", @"-emCRD.xml");    //修改文件名后缀
            //新建XMLFilesWithemCRD文件夹，用于存放_emCRD.xml文件
            DirectoryInfo crdDir = new DirectoryInfo(Global.mainForm._folderPath + @"\emCRDFiles");
            crdDir.Create();    //当此文件夹不存在时才创建
            CloneRegionDescriptorforMetric.CrdDir = crdDir.FullName; //将XMLFilesWithCRD文件夹的路径存入CrdDir中保存
            DirectoryInfo crdSubDir;
            if (Options.Granularity == GranularityType.BLOCKS)
            { crdSubDir = new DirectoryInfo(CloneRegionDescriptorforMetric.CrdDir + @"\blocks"); } //建立blocks子文件夹
            else
            { crdSubDir = new DirectoryInfo(CloneRegionDescriptorforMetric.CrdDir + @"\functions"); }
            crdSubDir.Create();
            string crdFullName = crdSubDir.FullName + @"\" + crdFileName;
            XmlTextWriter writer = new XmlTextWriter(crdFullName, Encoding.Default);   //另存文件
            writer.Formatting = Formatting.Indented;
            try
            {
                xmlFile.Save(writer);
            }
            catch (XmlException ee)
            {
                MessageBox.Show("Save CRD file failed! " + ee.Message);
            }
            this.Path = crdFullName;
            writer.Close();
            #endregion
        }

        #region 删除的GetSourceInfo方法
        /// <summary>
        /// 从source元素的属性中读取文件名和行区间信息
        /// </summary>
        /// <param name="curLine">当前的source标记所在的行</param>
        /// <returns>构造好的CloneSourceInfo对象</returns>
        //private CloneSourceInfo GetSourceInfo(string curLine)
        //{
        //    CloneSourceInfo sourceInfo = new CloneSourceInfo();
        //    int index = -1;
        //    string tempStr; //定义临时字符串变量
        //    //从file属性中获取源文件路径
        //    index = curLine.IndexOf("file=\"");
        //    index += 6;
        //    tempStr = "";
        //    while (curLine[index] != '\"')
        //    { tempStr += curLine[index++]; }
        //    sourceInfo.sourcePath = tempStr;
        //    //从startline属性获取开始行号
        //    index = curLine.IndexOf("startline", index);
        //    index += 11;
        //    tempStr = "";
        //    while (curLine[index] != '\"')
        //    { tempStr += curLine[index++]; }
        //    Int32.TryParse(tempStr, out sourceInfo.startLine);
        //    //从endline属性获取结束行号
        //    index = curLine.IndexOf("endline", index);
        //    index += 9;
        //    tempStr = "";
        //    while (curLine[index] != '\"')
        //    { tempStr += curLine[index++]; }
        //    Int32.TryParse(tempStr, out sourceInfo.endLine);

        //    return sourceInfo;
        //} 
        #endregion

        /// <summary>
        /// 判断克隆代码是否是方法块
        /// </summary>
        /// <param name="code">克隆代码段</param>
        /// <returns>若是，返回true；否则返回false</returns>
        private bool IsMethod(List<string> code)
        {
            CloneRegionDescriptorforMetric.InitBlockKeyWords();  //首先初始化块关键字列表
            string firstLines = "";
            int i = 0;
            int indexLBracket;
            do
            {
                firstLines += code[i].ToString();
                indexLBracket = code[i].ToString().IndexOf("{");
                i++;
                if (indexLBracket != -1)    //若出现"{"，根据"{"之前的i行判断是否是函数
                {
                    indexLBracket = firstLines.IndexOf("{");
                    if (indexLBracket < firstLines.Length - 1)
                    {
                        firstLines = firstLines.Remove(indexLBracket + 1);  //去掉"{"后面的部分 
                    }
                    //根据是否同时含有"（"，"）"并且不含块关键字来判断是否为函数
                    if (firstLines.IndexOf("(") != -1 && firstLines.IndexOf(")") != -1)
                    {
                        string headStr;
                        if (firstLines.IndexOf("(") != 0)
                        { headStr = firstLines.Substring(0, firstLines.IndexOf("(")).Trim(); } //截取"("前的串
                        else
                        { return false; }
                        if (!Regex.IsMatch(headStr, @"_|[A-Za-z][A-Za-z0-9_]*")) //判断headStr是否是自定义标识符，若是，继续判断，否则，返回false
                        { return false; }

                        string middleStr;   //保存从最右的")"到"{"之间的串
                        if (firstLines.IndexOf("{") - firstLines.LastIndexOf(")") > 1)
                        {
                            middleStr = firstLines.Substring(firstLines.LastIndexOf(")") + 1, firstLines.IndexOf("{") - firstLines.LastIndexOf(")") - 1);
                        }
                        else
                        { middleStr = ""; }
                        //分别考察headStr和middleStr中是否包含块关键字
                        foreach (string key in CloneRegionDescriptorforMetric.blockKeyWords)
                        {
                            if (headStr.IndexOf(key) != -1)
                            {
                                int keyIndexHead, keyIndexTail;  //关键字起止位置
                                keyIndexHead = headStr.IndexOf(key);
                                keyIndexTail = keyIndexHead + key.Length - 1;
                                //判断出现的关键字串是否是自定义标识符的一部分。
                                #region 被替换掉的方法一
                                //方法一：使用组合条件
                                //if (keyIndexHead == 0 || (!(firstLines[keyIndexHead - 1] >= 'a' && firstLines[keyIndexHead - 1] <= 'z') &&
                                //   !(firstLines[keyIndexHead - 1] >= 'A' && firstLines[keyIndexHead - 1] <= 'Z') &&
                                //   !(firstLines[keyIndexHead - 1] >= '0' && firstLines[keyIndexHead - 1] <= '9') &&
                                //   !(firstLines[keyIndexHead - 1] == '_' && firstLines[keyIndexHead - 1] == '_'))
                                //   &&
                                //   (keyIndexTail == firstLines.Length - 1 || !(firstLines[keyIndexTail + 1] >= 'a' && firstLines[keyIndexTail + 1] <= 'z') &&
                                //   !(firstLines[keyIndexTail + 1] >= 'A' && firstLines[keyIndexTail + 1] <= 'Z') &&
                                //   !(firstLines[keyIndexTail + 1] >= '0' && firstLines[keyIndexTail + 1] <= '9') &&
                                //   !(firstLines[keyIndexTail + 1] == '_' && firstLines[keyIndexTail + 1] == '_'))
                                //    ) 
                                #endregion
                                //方法二：使用正则表达式。 "[A-Za-z0-9_]"，正则表达式，匹配单个字母，数字或下划线
                                if ((keyIndexHead == 0 || !Regex.IsMatch(headStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                    (keyIndexTail == headStr.Length - 1 || !Regex.IsMatch(headStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                                { return false; }
                            }
                            if (middleStr != "")
                            {
                                if (middleStr.IndexOf(key) != -1)
                                {
                                    int keyIndexHead, keyIndexTail;  //关键字起止位置
                                    keyIndexHead = middleStr.IndexOf(key);
                                    keyIndexTail = keyIndexHead + key.Length - 1;
                                    if ((keyIndexHead == 0 || !Regex.IsMatch(middleStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                        (keyIndexTail == middleStr.Length - 1 || !Regex.IsMatch(middleStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                                    { return false; }
                                }
                            }
                        }
                        return true;
                        //取")"和"{"之间的部分
                        //string str = firstLines.Substring(firstLines.IndexOf(")") + 1, firstLines.IndexOf("{") - firstLines.IndexOf(")")).Trim();
                        //再判断")"和"{"之间是空白或含有throws单词和Exception串
                        //if (str == "" || str.IndexOf("throws ") != -1 && str.IndexOf("Exception") != -1)
                        //{ return true; }

                        //判断str中是否包含";"，若不包含则返回true，否则返回false
                        //if (str.IndexOf(";") == -1)
                        //{ return true; }
                        //else
                        //{ return false; }
                    }
                    else
                    { return false; }
                }
            } while (i < code.Count);
            return false;
        }

        /// <summary>
        /// 获取克隆代码段所在的方法信息
        /// </summary>
        /// </summary>
        /// <param name="codeClone">代表源代码文件的ArrayList对象</param>
        /// <returns>返回方法信息对象，若提取失败，返回null</returns>
        private MethodInfoType GetMethodInfo(List<string> codeClone)
        {
            MethodInfoType mInfo = new MethodInfoType();

            int indexLParen, indexRParen, indexMName;  //parenthesis，圆括号
            int i;
            for (i = 0; i < codeClone.Count; i++)
            {
                indexLParen = codeClone[i].ToString().IndexOf("(");
                indexRParen = codeClone[i].ToString().IndexOf(")");
                if (indexLParen != -1) //找到方法名后面的"("
                {
                    indexMName = codeClone[i].ToString().Substring(0, indexLParen).TrimEnd().LastIndexOf(" ");
                    #region 提取方法名
                    if (indexMName != -1)    //方法名前有空格
                    {
                        indexMName++;
                        mInfo.methodName = codeClone[i].ToString().Substring(indexMName, indexLParen - indexMName).Trim();
                    }
                    else  //方法名从行顶头开始
                    {
                        mInfo.methodName = codeClone[i].ToString().Substring(0, indexLParen).Trim();
                    }
                    #endregion

                    #region 提取参数信息
                    string arguList = "";
                    if (indexRParen != -1)  //如果本行找到")"
                    {
                        arguList = codeClone[i].ToString().Substring(indexLParen + 1, indexRParen - indexLParen - 1).Trim(Global.charsToTrim);
                        GetArguInfo(arguList, out mInfo.mParaTypeList, out mInfo.mParaNum);
                    }
                    else
                    {
                        int indexComma = codeClone[i].ToString().LastIndexOf(",");
                        if (indexComma != -1 && indexComma > indexLParen)
                        {
                            arguList = codeClone[i].ToString().Substring(indexLParen + 1, indexComma - indexLParen).Trim(Global.charsToTrim);
                        }
                        else
                        { arguList = ""; }
                        int j = i + 1;
                        indexRParen = codeClone[j].ToString().IndexOf(")");
                        while (indexRParen == -1)
                        {
                            indexComma = codeClone[j].ToString().IndexOf(",");
                            if (indexComma != -1)
                            {
                                arguList += codeClone[j].ToString().Substring(0, indexComma + 1).Trim(Global.charsToTrim);
                            }
                            j++;
                            indexRParen = codeClone[j].ToString().IndexOf(")");
                        }
                        arguList += codeClone[j].ToString().Substring(0, indexRParen).Trim(Global.charsToTrim);
                        GetArguInfo(arguList, out mInfo.mParaTypeList, out mInfo.mParaNum);
                    }

                    #endregion
                    break;
                }
                else { continue; }
            }
            return mInfo;
        }

        /// <summary>
        /// 从函数参数字符串中，提取出参数类型和参数个数信息
        /// </summary>
        /// <param name="arguStr">待提取的参数字符串</param>
        /// <param name="mParaTypeList">输出参数：返回类型列表</param>
        /// <param name="mParaNum">输出参数：返回参数个数</param>
        private void GetArguInfo(string arguStr, out ParaTypeList mParaTypeList, out int mParaNum)
        {
            if (arguStr == "" || arguStr.Trim() == "void")  //没有参数
            {
                mParaTypeList = null;
                mParaNum = 0;
            }
            else
            {
                mParaTypeList = new ParaTypeList();
                mParaNum = 0;
                int indexComma = arguStr.IndexOf(",");  //保存'，'的位置
                List<string> arguInfo = new List<string>();
                if (indexComma == -1)   //只有一个参数
                {
                    mParaNum++;
                    arguInfo = GetWords(arguStr);
                    string aType = "";
                    for (int i = 0; i < arguInfo.Count - 1; i++)//当一个参数有多个修饰符时（如out int），使用‘+’连接
                    {
                        aType = aType + " " + arguInfo[i].ToString();
                    }
                    mParaTypeList.Add(aType);
                }
                else
                {
                    string curArguStr, otherArguStr;
                    otherArguStr = arguStr;
                    do
                    {
                        indexComma = otherArguStr.IndexOf(",");
                        if (indexComma != -1)
                        {
                            curArguStr = otherArguStr.Substring(0, indexComma).Trim(Global.charsToTrim);//提取出第一个参数字符串
                            otherArguStr = otherArguStr.Substring(indexComma + 1).Trim();   //余下的参数字符串 
                        }
                        else
                        { curArguStr = otherArguStr; otherArguStr = ""; }
                        arguInfo = GetWords(curArguStr);
                        string aType = "";
                        for (int i = 0; i < arguInfo.Count - 1; i++)
                        {
                            aType = aType + " " + arguInfo[i].ToString();
                        }
                        mParaTypeList.Add(aType);
                        mParaNum++;
                    } while (otherArguStr != "");
                }
            }
        }

        /// <summary>
        /// 从一段字符串中提取出单词
        /// </summary>
        /// <param name="str">待提取的字符串</param>
        /// <returns>返回ArrayList对象的的单词列表</returns>
        public List<string> GetWords(string str)
        {
            if (str == "")
            { return null; }
            else
            {
                List<string> words = new List<string>();
                str = str.Trim(Global.charsToTrim);
                int indexSpace = str.IndexOf(" ");  //空格的位置
                if (indexSpace == -1)   //只有一个单词
                {
                    words.Add(str);
                }
                else
                {
                    string curWord, otherWords;
                    otherWords = str;
                    do
                    {
                        indexSpace = otherWords.IndexOf(" ");
                        if (indexSpace != -1)
                        {
                            curWord = otherWords.Substring(0, indexSpace).Trim();
                            otherWords = otherWords.Substring(indexSpace).Trim();
                        }
                        else
                        { curWord = otherWords; otherWords = ""; }
                        words.Add(curWord);
                    } while (otherWords != "");
                }
                return words;
            }
        }

        /// <summary>
        /// 获取克隆代码段所在的多层块信息
        /// </summary>
        /// <param name="source">代表源代码文件的ArrayList对象</param>
        /// <param name="codeClone">代表克隆代码片段的ArrayList对象</param>
        /// <param name="startLine">克隆代码段在源文件中的起始行号</param>
        /// <returns>返回多层块信息</returns>
        private BlockInfoList GetBlockInfo(List<string> source, List<string> codeClone, int startLine ,int endLine)
        {
            BlockInfoList blockInfoList = new BlockInfoList();

            //从克隆片段第一行开始向前
            int lineIndex = startLine - 1;    //注意：行号从1起，索引从0起
            string curLine = source[lineIndex].ToString();
            //存储克隆片段加入前面若干行后的代码段，这个用于进行IsMethod判断.初始值为codeClone.
            List<string> extendedCodeClone = new List<string>(codeClone);
            int rBracketUnMatched = 0;   //记录未匹配的右括号数量
            bool newLBracket = true;   //标记是否新出现单独的左括号，初始值为true
            string blockHead = ""; //用于保存关键字块头的若干行（即从关键字结束到"{"之间的若干行）
            //当没有遇到class关键字时，执行循环体
            while (curLine.IndexOf(" class ") == -1)
            //while(curLine.IndexOf(" class ") == -1 && curLine.IndexOf(this.ClassName) == -1)
            {
                if (lineIndex < startLine - 1) //当前行不是克隆代码第一行时，记录大括号匹配情况
                {
                    if (curLine.IndexOf("}") != -1)
                    { rBracketUnMatched++; newLBracket = false; }
                    //if (curLine.IndexOf("{") != -1 && curLine[curLine.IndexOf("{") - 1] != '\"')
                    if (curLine.IndexOf("{") != -1)
                    {
                        if (rBracketUnMatched == 0)
                        { newLBracket = true; }
                        else
                        { newLBracket = false; rBracketUnMatched--; }
                    }
                }
                if (newLBracket)    //只有新出现单个的左括号时，才检查块信息
                {
                    blockHead = blockHead.Insert(0, curLine);   //构造块头字符串
                    string keyWord;
                    bool isBlock = false;
                    int i = -1; //i用于获取关键字在列表中的序号，以对应BlockType枚举类型中的值
                    foreach (string key in blockKeyWords)
                    {
                        i++;
                        int keyIndexHead, keyIndexTail;  //关键字起止位置
                        keyIndexHead = curLine.IndexOf(key);
                        if (keyIndexHead != -1)
                        {
                            keyIndexTail = keyIndexHead + key.Length - 1;
                            //判断出现的关键字串是否是自定义标识符的一部分
                            #region 被替换掉的方法一
                            //方法一：使用组合条件
                            //if (keyIndexHead == 0 || (!(curLine[keyIndexHead - 1] >= 'a' && curLine[keyIndexHead - 1] <= 'z') &&
                            //   !(curLine[keyIndexHead - 1] >= 'A' && curLine[keyIndexHead - 1] <= 'Z') &&
                            //   !(curLine[keyIndexHead - 1] >= '0' && curLine[keyIndexHead - 1] <= '9') &&
                            //   !(curLine[keyIndexHead - 1] == '_' && curLine[keyIndexHead - 1] == '_'))
                            //   &&
                            //   (keyIndexTail == curLine.Length - 1 || !(curLine[keyIndexTail + 1] >= 'a' && curLine[keyIndexTail + 1] <= 'z') &&
                            //   !(curLine[keyIndexTail + 1] >= 'A' && curLine[keyIndexTail + 1] <= 'Z') &&
                            //   !(curLine[keyIndexTail + 1] >= '0' && curLine[keyIndexTail + 1] <= '9') &&
                            //   !(curLine[keyIndexTail + 1] == '_' && curLine[keyIndexTail + 1] == '_'))
                            //    ) 
                            #endregion
                            // 方法二：使用正则表达式。"[A-Za-z0-9_]"，正则表达式，匹配单个字母，数字或下划线
                            if ((keyIndexHead == 0 || !Regex.IsMatch(curLine[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                                (keyIndexTail == curLine.Length - 1 || !Regex.IsMatch(curLine[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                            {
                                keyWord = key;
                                isBlock = true;
                                break;
                            }
                        }
                    }
                    if (isBlock)
                    {
                        BlockInfo bInfo = new BlockInfo();
                        bInfo.bType = (BlockType)i;
                        if (i == 1 || i > 5)  //如果是else,do,try或catch快，anchor为null
                        { bInfo.anchor = null; }
                        else
                        {
                            //获取"（"与"）"之间的表达式
                            int indexHead, indexTail;
                            //当blockHead中未包含"{"时（这种情况只出现在克隆代码第一行），向后寻找"{"，构造完整的blockHead
                            if (blockHead.IndexOf("{") == -1)
                            {
                                int indexBack = lineIndex + 1;
                                while (source[indexBack].IndexOf("{") == -1)
                                { blockHead = blockHead + source[indexBack]; }
                                blockHead = blockHead + source[indexBack];
                            }
                            //截取"{"前的子串，即块的头部
                            blockHead = blockHead.Substring(0, blockHead.IndexOf("{"));
                            indexHead = blockHead.IndexOf("(");
                            indexTail = blockHead.LastIndexOf(")");
                            bInfo.anchor = blockHead.Substring(indexHead, indexTail - indexHead + 1).ToString().Trim();
                            bInfo.anchor = bInfo.anchor.Replace("\n", " ");
                            bInfo.anchor = bInfo.anchor.Replace("\t", " ");
                        }
                        blockInfoList.Insert(0, bInfo); //将当前层的块信息插入到块信息列表的头部
                        newLBracket = false;    //检查完当前关键字块后，清空newLBracket信息
                        blockHead = ""; //检查完当前关键字块后，清空blockHead信息
                    }

                }

                lineIndex--;
                curLine = source[lineIndex].ToString();   //读取前一行
                extendedCodeClone.Insert(0, curLine);
                //如果到达方法边界，而且是包含当前克隆片段的方法（通过是否出现新的左大括号来判断），获取方法名，并停止向前扫描
                if (IsMethod(extendedCodeClone))
                {
                    if (curLine.IndexOf("{") != -1)
                    {
                        if (rBracketUnMatched == 0)
                        { newLBracket = true; }
                        else
                        { newLBracket = false; }
                    }
                    if (newLBracket)//如果出现新的左大括号，认为到达包含本克隆片段的方法边界；否则，认为是独立于本段克隆代码的方法
                    {
                        this.MethodInfo = GetMethodInfo(extendedCodeClone);
                        this.RelStartLine = (startLine - lineIndex).ToString(); //计算相对行号
                        this.RelEndLine = (endLine - lineIndex).ToString(); 
                        break;
                    }
                }
                //当上面的条件不满足时，继续向前搜索
            }
            //如果到类边界才停止，说明克隆代码不在方法内部（当然本身也不是方法，前面判断过）。第三个条件是排除已获得方法信息，但方法名中包含类名字符串的情况
            //if ((curLine.IndexOf(" class ") != -1 || curLine.IndexOf(this.ClassName) != -1) && this.MethodInfo == null)
            if (curLine.IndexOf(" class ") != -1)    //废弃其他条件，只根据是否出现class关键字来判断
            {
                this.MethodInfo = null;
                blockInfoList = null;
            }
            else if (blockInfoList.Count == 0)   //如果最终没有获得块信息，则置此字段为空
            { blockInfoList = null; }
            return blockInfoList;
        }

        /// <summary>
        /// 从源代码文件中提取克隆代码片段
        /// </summary>
        /// <param name="source">保存源代码的ArrayList对象</param>
        /// <param name="sourceInfo">以文件名和行号表示的克隆代码信息</param>
        /// <returns></returns>
        internal static List<string> GetCFSourceFromSourcInfo(List<string> source, CloneSourceInfo sourceInfo)
        {
            List<string> codeClone = new List<string>();
            for (int i = sourceInfo.startLine - 1; i < sourceInfo.endLine; i++) //注意，索引从0起算，而行号从1起算
            { codeClone.Add(source[i].ToString()); }
            return codeClone;
        }

        /// <summary>
        /// 根据CRD对象信息获取克隆片段源代码
        /// </summary>
        /// <param name="crd"></param>
        /// <returns>ArrayList类型的源代码</returns>
        internal static List<string> GetCFSourceFromCRD(CloneRegionDescriptorforMetric crd)
        {
            List<string> fullSource = new List<string>();
            List<string> cloneFragment = new List<string>();
            CloneSourceInfo sourceInfo = new CloneSourceInfo();
            sourceInfo.sourcePath = crd.FileName;
            Int32.TryParse(crd.StartLine, out sourceInfo.startLine);
            Int32.TryParse(crd.EndLine, out sourceInfo.endLine);
            //获取目标系统文件夹起始路径
            string subSysStartPath = Global.mainForm.subSysDirectory;
            //绝对路径=起始路径+相对路径
            string subSysPath = subSysStartPath + "\\" + sourceInfo.sourcePath;
            //保存源代码文件的ArrayList对象
            fullSource = Global.GetFileContent(subSysPath);
            cloneFragment = GetCFSourceFromSourcInfo(fullSource, sourceInfo);
            return cloneFragment;
        }

        /// <summary>
        /// 对于Java或C#文件，本方法直接从文件名中提取类名（设为static是为了在别的类中可以调用）
        /// </summary>
        /// <param name="fileName">带有路径信息的文件名</param>
        /// <returns>返回类名字符串</returns>
        internal static string GetClassNameFromFileName(string fileName)
        {
            string className = null;
            int indexHead, indexTail;
            indexTail = fileName.LastIndexOf(".");
            indexHead = fileName.LastIndexOf("/");
            className = fileName.Substring(indexHead + 1, indexTail - indexHead - 1).Trim();
            return className;
        }

        /// <summary>
        /// 计算两个CRD匹配的程度（只考虑CRD的内容，不考虑文本相似度）
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <returns>返回CRDMatchLevel类型值</returns>
        public static CRDMatchLevel GetCRDMatchLevel(CloneRegionDescriptorforMetric src, CloneRegionDescriptorforMetric dest)
        {
            //除去文件名字符串中系统名称的部分，如testdnsjava-0-3,testdnsjava-0-4
            string srcFileName = src.FileName.Substring(src.FileName.IndexOf("/"));
            string destFileName = dest.FileName.Substring(dest.FileName.IndexOf("/"));
            //对于特定输入，在如下分支中，只走一条路径
            if (!srcFileName.Equals(destFileName))
            { return CRDMatchLevel.DIFFERENT; } //文件名不同的认为不匹配
            else//文件信息相同
            {
                if ((src.ClassName != null && dest.ClassName != null) && !src.ClassName.Equals(dest.ClassName) || src.ClassName != null && dest.ClassName == null ||
                    src.ClassName == null && dest.ClassName != null)
                { return CRDMatchLevel.DIFFERENT; } //类信息不同的认为不匹配
                else//类信息相同，或两个类信息都为空
                {
                    //如果二者方法信息都为空
                    if (src.MethodInfo == null && dest.MethodInfo == null)
                    { return CRDMatchLevel.FILECLASSMATCH; }
                    //二者中有一个方法信息为空，另一个不为空
                    else if (src.MethodInfo != null && dest.MethodInfo == null || src.MethodInfo == null && dest.MethodInfo != null)
                    { return CRDMatchLevel.DIFFERENT; }
                    //如果二者方法信息都不为空
                    else
                    {
                        //如果方法信息相同（方法名，参数信息都相同），检查块信息
                        if (src.MethodInfo.Equals(dest.MethodInfo))
                        {
                            if (src.BlockInfoList != null && dest.BlockInfoList != null)
                            {
                                if (src.BlockInfoList.Equals(dest.BlockInfoList))//块信息相同
                                { return CRDMatchLevel.BLOCKMATCH; }
                                else//块信息不同
                                { return CRDMatchLevel.METHODINFOMATCH; }
                            }
                            //有一个块信息为空，另一个不为空，或两个块信息都为空（包含Granularity=functions的情况）
                            else
                            { return CRDMatchLevel.METHODINFOMATCH; }

                        }
                        //方法名相同，参数信息不同
                        else if ((src.MethodInfo.methodName == dest.MethodInfo.methodName) &&
                            !src.MethodInfo.mParaTypeList.Equals(dest.MethodInfo.mParaTypeList))
                        { return CRDMatchLevel.METHODNAMEMATCH; }
                        //方法名，参数信息都不同
                        else
                        { return CRDMatchLevel.FILECLASSMATCH; }
                    }
                }
            }
        }

        /// <summary>
        /// 计算两段克隆代码的文本相似度（使用Diff类）
        /// </summary>
        /// <param name="srcCrd">源CRD</param>
        /// <param name="destCrd">目标CRD</param>
        /// <param name="ignoreEmptyLines">指定是否忽略空行</param>
        /// <returns></returns>
        public static float GetTextSimilarity(CloneRegionDescriptorforMetric srcCrd, CloneRegionDescriptorforMetric destCrd, bool ignoreEmptyLines)
        {
            List<string> srcFileContent = new List<string>();
            List<string> destFileContent = new List<string>();
            List<string> srcFragment = new List<string>();
            List<string> destFragment = new List<string>();
            CloneSourceInfo info = new CloneSourceInfo();

            #region 获得srcCrdNode源代码文本
            info.sourcePath = srcCrd.FileName;  //获得源文件名
            string fullName = Global.mainForm.subSysDirectory + "\\" + info.sourcePath;
            //获取源代码行区间
            Int32.TryParse(srcCrd.StartLine, out info.startLine);
            Int32.TryParse(srcCrd.EndLine, out info.endLine);
            srcFileContent = Global.GetFileContent(fullName);
            srcFragment = CloneRegionDescriptorforMetric.GetCFSourceFromSourcInfo(srcFileContent, info);
            #endregion

            #region 获得destCrdNode源代码文本
            info.sourcePath = destCrd.FileName;    //获得源文件名
            fullName = Global.mainForm.subSysDirectory + "\\" + info.sourcePath;
            //获取源代码行区间
            Int32.TryParse(destCrd.StartLine, out info.startLine);
            Int32.TryParse(destCrd.EndLine, out info.endLine);
            destFileContent = Global.GetFileContent(fullName);
            destFragment = CloneRegionDescriptorforMetric.GetCFSourceFromSourcInfo(destFileContent, info);
            #endregion

            //使用Diff类计算两段代码的相似度
            Diff.UseDefaultStrSimTh();  //使用行相似度阈值默认值0.5
            Diff.DiffInfo diffFile = Diff.DiffFiles(srcFragment, destFragment);
            float sim = Diff.FileSimilarity(diffFile, srcFragment.Count, destFragment.Count, ignoreEmptyLines);

            return sim;
        }

        public static float GetLocationOverlap(CloneRegionDescriptorforMetric srcCrd, CloneRegionDescriptorforMetric destCrd)
        {
            if (srcCrd.RelStartLine == null || srcCrd.RelEndLine == null || destCrd.RelStartLine == null || destCrd.RelEndLine == null)
            { return -1; }
            int startLine1, startLine2, endLine1, endLine2;
            startLine1 = Int32.Parse(srcCrd.RelStartLine);
            endLine1 = Int32.Parse(srcCrd.RelEndLine);
            startLine2 = Int32.Parse(destCrd.RelStartLine);
            endLine2 = Int32.Parse(destCrd.RelEndLine);
            int startLine = startLine1 > startLine2 ? startLine1 : startLine2;  //取startLine中较大的
            int endLine = endLine1 < endLine2 ? endLine1 : endLine2;    //取endLine中较小的
            //计算overLapping
            return (float)(endLine - startLine) / (float)(endLine2 - startLine2);
        }

        public void CreateCRDFromCRDNode(XmlElement node)
        {
            if (node.Name != "CloneRegionDescriptorforMetric")
            {
                MessageBox.Show("This is not a CRD node! Please pass the right parameter!");
                return;
            }
            this.FileName = ((XmlElement)node.SelectSingleNode("fileName")).InnerText;
            if (node.SelectSingleNode("className") != null)
            {
                this.ClassName = ((XmlElement)node.SelectSingleNode("className")).InnerText;
            }
            XmlElement methodInfoNode = (XmlElement)node.SelectSingleNode("methodInfo");
            if (methodInfoNode == null)
            { this.MethodInfo = null; }
            else
            {
                //构造CRD的MethodInfo成员
                this.MethodInfo = new MethodInfoType();
                this.MethodInfo.methodName = ((XmlElement)methodInfoNode.SelectSingleNode("mName")).InnerXml;
                XmlNodeList paraList = methodInfoNode.SelectNodes("mPara");
                if (paraList == null)
                { this.MethodInfo.mParaTypeList = null; }
                else
                {   //构造CRD的MethodInfo的参数信息
                    this.MethodInfo.mParaTypeList = new ParaTypeList();
                    this.MethodInfo.mParaNum = paraList.Count;
                    foreach (XmlElement para in paraList)
                    {
                        this.MethodInfo.mParaTypeList.Add(para.InnerText);
                    }
                }
                XmlNodeList blockInfoList = ((XmlElement)node).SelectNodes("blockInfo");
                if (blockInfoList.Count == 0)
                { this.BlockInfoList = null; }
                else
                {
                    //构造CRD的BlockInfoList成员
                    this.BlockInfoList = new BlockInfoList();
                    foreach (XmlElement blockInfo in blockInfoList)
                    {
                        BlockInfo info = new BlockInfo();
                        #region 使用switch结构构造info
                        switch (((XmlElement)blockInfo.SelectSingleNode("bType")).InnerText)
                        {
                            case "IF":
                                {
                                    info.bType = BlockType.IF;
                                    info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    break;
                                }
                            case "ELSE":
                                {
                                    info.bType = BlockType.ELSE; info.anchor = null; break;
                                }
                            case "SWITCH":
                                {
                                    info.bType = BlockType.SWITCH;
                                    info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    break;
                                }
                            case "FOR":
                                {
                                    info.bType = BlockType.FOR;
                                    info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    break;
                                }
                            case "WHILE":
                                {
                                    info.bType = BlockType.WHILE;
                                    info.anchor = ((XmlElement)blockInfo.SelectSingleNode("Anchor")).InnerText;
                                    break;
                                }
                            case "DO":
                                {
                                    info.bType = BlockType.DO; info.anchor = null; break;
                                }
                            case "TRY":
                                {
                                    info.bType = BlockType.TRY; info.anchor = null; break;
                                }
                            case "CATCH":
                                {
                                    info.bType = BlockType.CATCH; info.anchor = null; break;
                                }
                            default: break;
                        }
                        #endregion
                        this.BlockInfoList.Add(info);
                    }
                }
            }
            this.StartLine = ((XmlElement)node.ParentNode).GetAttribute("startline");
            this.EndLine = ((XmlElement)node.ParentNode).GetAttribute("endline");

            //构造附加的相对位置信息
            XmlElement locationNode = (XmlElement)node.SelectSingleNode("Location");
            if (locationNode != null)
            {
                this.RelStartLine = locationNode.GetAttribute("relStartLine");
                this.RelEndLine = locationNode.GetAttribute("relEndLine");
            }
            else
            {
                this.RelStartLine = null;
                this.RelEndLine = null;
            }

            if (Options.Granularity == GranularityType.BLOCKS)
            { this.Path = CloneRegionDescriptorforMetric.CrdDir + @"\blocks\" + this.FileName; }
            else
            { this.Path = CloneRegionDescriptorforMetric.CrdDir + @"\functions\" + this.FileName; }
        }
    }
     
}
