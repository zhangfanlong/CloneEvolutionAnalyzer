using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;

namespace CloneEvolutionAnalyzer
{
    //Halstead度量在HalsteadMetric类中实现

    //上下文类型定义
    public enum ContextType
    {
        LOOP = 0,   //for语句、while语句、do-while语句统称为loop语句
        SELECT,     //if-else,switch语句统称为select语句
        FUNCLASSDEF,//函数/类定义或调用？？
        EXCPHANDLE, //异常处理
        SYNC,       //synchronized（同步锁）语句，针对java而设
    }

    //定义变化复杂度类型
    public struct ChangeComplexityType
    {
        public int ChangeInLateQuarPeriod;  //近1/4版本周期中的变化次数
        public int ChangeInLateHalfPeriod;  //近1/2版本周期中的变化次数
    }

    //定义进化特征类型
    public struct EvolutionFeatureType
    {
 
    }

    public class MetricCollection
    {
        #region 静态度量定义
        //克隆代码行数（不称为克隆粒度，因为会与Functions/Blocks混淆）
        private int lineNumOfCF;
        public int LinenNumOfCF
        {
            get { return lineNumOfCF; }
        }
        //克隆代码所在文件名（包含相对路径）
        private string fileName;
        public string FileName
        {
            get { return fileName; }
        }
        //克隆代码所在类名（包含包或命名空间信息？？），针对Java/C++/C#语言（C语言系统此项为空）（注：暂不考虑C++的情况）
        private string className;
        public string ClassName
        {
            get { return className; }
        }
        //上下文类型（考虑：上下文信息是否只针对Blocks的情形，因为对于Functions的情况好像没有意义？？）
        private ContextType contextInfo;
        public ContextType ContextInfo
        {
            get { return contextInfo; }
        }
        //Halstead度量对象 
        private HalsteadMetric halsteadMetric;
        public HalsteadMetric HalsteadMetric
        {
            get { return halsteadMetric; }
        }

        #endregion

        #region 进化度量定义（进化度量以ev或Ev开头，区别于静态度量）
        //注：这里的进化特征是克隆片段的进化特征，而不是克隆群的进化特征
        //进化特征
        private EvolutionFeatureType evEvolutionFeature;
        public EvolutionFeatureType EvEvolutionFeature
        {
            get { return evEvolutionFeature; }
        }
        //预期寿命
        private int evExpectAge;
        public int EvExpectAge
        {
            get { return evExpectAge; }
        }
        //变化复杂度
        private ChangeComplexityType evChangeComplexity;
        public ChangeComplexityType EvChangeComplexity
        {
            get { return evChangeComplexity; }
        }
        #endregion

        private void GetHalsteadMetric(List<string> codeFragment)
        {
            halsteadMetric.GetFromCode(codeFragment);
        }

        private void GetClassInfo(List<string> codeFragment)
        {
 
        }

        public void GetContextInfo(List<string> codeFragment)
        {
            
        }

        public void GenerateForCF(XmlElement sourceElement)
        {
            CloneSourceInfo sourceInfo = new CloneSourceInfo();
            //这个判断其实是不必要的，因为合法的克隆代码xml文件，必定具有这三个属性
            if (sourceElement.HasAttribute("file") && sourceElement.HasAttribute("startline") && sourceElement.HasAttribute("endline"))
            {
                sourceInfo.sourcePath = sourceElement.GetAttribute("file");
                this.fileName = sourceElement.GetAttribute("file"); //确定文件分布
                Int32.TryParse(sourceElement.GetAttribute("startline"), out sourceInfo.startLine);
                Int32.TryParse(sourceElement.GetAttribute("endline"), out sourceInfo.endLine);
                this.lineNumOfCF = sourceInfo.endLine - sourceInfo.startLine + 1;   //确定克隆代码行数
            }
            //对于Java或C#文件，直接从文件名中提取类名，确定CRD的ClassName字段；否则，将该字段置null。不考虑C++语言，因为nicad不能处理C++语言
            if (Options.Language == ProgLanguage.Java || Options.Language == ProgLanguage.CSharp)
            {
                //考虑从源代码获得类信息，而不是简单通过文件名
                this.className = CloneRegionDescriptor.GetClassNameFromFileName(this.FileName); //使用CRD类的方法
            }
            else
            {
                this.className = null;
            }

            #region 获取克隆片段源代码
            //此处使用GetCFSourceFromSourcInfo，而不用GetCFSourceFromCRD的原因是CRD正在构造中
            //获取目标系统文件夹起始路径
            string subSysStartPath = Global.mainForm.subSysDirectory;
            //绝对路径=起始路径+相对路径
            string subSysPath = subSysStartPath + "\\" + sourceInfo.sourcePath;
            //保存源代码文件的List<string>对象
            List<string> fileContent = new List<string>();
            List<string> cloneFragment = new List<string>();
            fileContent = Global.GetFileContent(subSysPath);
            cloneFragment = CloneRegionDescriptor.GetCFSourceFromSourcInfo(fileContent, sourceInfo);
            #endregion


        }
        
        public void GenerateForSys(XmlDocument xmlFile, string fileName)
        {
            XmlElement rootElement = xmlFile.DocumentElement;

            foreach (XmlElement firstLevelElement in rootElement.ChildNodes)
            {
                if (firstLevelElement.SelectNodes("source").Count != 0)
                {
                    XmlNodeList elementList = firstLevelElement.SelectNodes("source");
                    foreach (XmlElement srcElement in elementList)
                    {
                    }
                }
            }
        }
    }
}
