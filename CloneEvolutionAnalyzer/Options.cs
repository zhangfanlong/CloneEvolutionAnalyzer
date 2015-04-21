using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloneEvolutionAnalyzer
{
    //Specify programming languages which can be handled.
    public enum ProgLanguage
    {
        Java = 0,
        CSharp = 1,
        C = 2,
        Python = 3,
        //CPlusplus = 4,C++的情况暂不考虑
    }
    //granularity of clone detection
    public enum GranularityType
    {
        FUNCTIONS = 0,
        BLOCKS = 1,
    }
    /// <summary>
    /// 选项类，保存程序的首选项及参数设置
    /// </summary>
    public class Options
    {
        //Attribute ProLanguage
        public static ProgLanguage Language { get; set; }
        //Attribute Mode
        public static int Mode { get; set; } //工作模式，0为Mannal，1为Auto
        public static GranularityType Granularity;  //克隆代码检测的粒度
        //other perferences
        //
    }
}
