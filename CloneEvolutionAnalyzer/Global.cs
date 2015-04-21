using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
//using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
//using System.Runtime.InteropServices;   //调用API函数需要引用此命名空间，本程序暂未用到
using System.Windows.Forms;

namespace CloneEvolutionAnalyzer
{
    /// <summary>
    /// Global类，存放程序运行的全局信息，如：MainForm对象，全局的设置，状态等。
    /// </summary>
    class Global
    {
        #region 静态对象定义
        //API函数声明，暂未用到
        //[DllImport("user32.dll", EntryPoint = "GetForegroundWindow", ExactSpelling = true)]
        //public static extern IntPtr GetForegroundWindow();

        //[DllImport("user32.dll", EntryPoint = "FindWindow", ExactSpelling = true)]
        //public static extern IntPtr FindWindow(string lpClassName,string lpWindowName);
        //保存主窗口对象
        public static MainForm mainForm = Program.theMainForm;
        //保存鼠标位置对象（mousePtr如何就表示鼠标位置？为什么它不表示其他的点呢）
        public static Point mousePtr;

        //保存crd生成的状态，0-未生成任何一个版本的crd，1-生成单个版本的crd，2-生成blocks/functions粒度下所有版本的crd
        private static int crdGenerationState = 0;
        public static int CrdGenerationState
        {
            get { return crdGenerationState; }
            set { crdGenerationState = value; }
        }
        //保存映射的状态，0-未进行任何映射，1-进行某对相邻版本间的映射，2-已映射所有相邻版本，在blocks/functions粒度下
        private static int mapState = 0;
        public static int MapState
        { 
            get { return mapState; }
            set { mapState = value; }
        }

        public static char[] charsToTrim = { ' ', '\t', '\n', '\r' };   //用作Trim()方法的参数，去掉参数串中的三种多余字符 

        public static int treeView1NodeCheckedNum;

        public static string mappingVersionRange;  //标记版本间映射的范围："ADJACENTVERSIONS"或"ALLVERSIONS"
        #endregion

        #region 类型定义
        //枚举类型定义关注的消息
        public enum WindowsMessage
        {
            //与RichTextBox同步滚动相关的消息
            //WM_VSCROLL = 0x0115,
            WM_HSCROLL = 276,
            WM_VSCROLL = 277,
            WM_SETCURSOR = 32,
            WM_MOUSEWHEEL = 522,
            WM_MOUSEMOVE = 512,
            WM_MOUSELEAVE = 675,
            WM_MOUSELAST = 521,
            WM_MOUSEHOVER = 673,
            WM_MOUSEFIRST = 512,
            WM_MOUSEACTIVATE = 33,

            WM_RBUTTONDOWN = 0x0204
        }

        public class KeyWordsCollection
        {
            //枚举类型定义各种语言共有关键字（扩大集合，包含面向对象语言的关键字）(50)
            public static List<string> _commonKeyWords = new List<string>()
            {
                #region 关键字列表
		        "abstract",
                "auto",
                "bool",
                "break",
                "case",
                "catch",
                "char",
                "class",
                "const",
                "continue",
                "default",
                "do",
                "double",
                "else",
                "enum",
                "extern",
                "false",
                "finally",
                "float",
                "for",
                "goto",
                "if",
                "int",
                "long",
                "malloc",
                "namespace",
                "new",
                "null",
                "private",
                "protected",
                "public",
                "return",
                "short",
                "signed",
                "sizeof",
                "static",
                "struct",
                "switch",
                "this",
                "throw",
                "try",
                "true",
                "typedef",
                "union",
                "unsigned",
                "using",
                "virtual",
                "void",
                "volatile",
                "while", 
	            #endregion
            };

            //定义C++语言特有关键字（不常见的未包括）（7）（暂不考虑C++语言）
            //public static List<string> _CPPKeyWords = new List<string>()
            //{
            //    #region 关键字列表
            //    "delete",
            //    "explicit",
            //    "export",
            //    "friend",
            //    "line",
            //    "operator",
            //    "template", 
            //    #endregion
            //};
            
            //定义java语言特有关键字（15）
            public static List<string> _JavaKeyWords = new List<string>()
            {
                #region 关键字列表
		        "boolean",
                "byte",
                "extends",
                "final",
                "interface",
                "implements",
                "import",
                "instanceof",
                "java", //不是关键字，但在引用的包中经常出现，不能视为标识符
                "native",
                "package",
                "strictfp",   //??
                "synchronized",
                "super",
                "transient",  //??
                "throws", 
	            #endregion
            };

            //定义C#语言特有关键字（29）
            public static List<string> _CSharpKeyWords = new List<string>()
            {
                #region 关键字列表
		        "as",
                "base",
                "checked",
                "decimal",
                "delegate",
                "event",
                "explicit",
                "fixed",
                "foreach",
                "implicit",
                "in",
                "interface",
                "internal",
                "is",
                "lock",
                "operator",
                "out",
                "override",
                "params",
                "readonly",
                "ref",
                "sbyte",
                "sealed",
                "stackalloc",
                "uint",
                "ulong",
                "unchecked",
                "unsafe",
                "unshort", 
	            #endregion
            };

            /// <summary>
            /// 判断标识符是否是关键字
            /// </summary>
            /// <param name="identifier"></param>
            /// <returns></returns>
            public static bool IsKeyWord(string identifier)
            {
                foreach (string key in _commonKeyWords)
                {
                    if (identifier == key)
                    { return true; }
                }

                if (Options.Language==ProgLanguage.Java)
                {
                    foreach (string key in _JavaKeyWords)
                    {
                        if (identifier == key)
                        { return true; }
                    } 
                }
                if (Options.Language == ProgLanguage.CSharp)
                {
                    foreach (string key in _CSharpKeyWords)
                    {
                        if (identifier == key)
                        { return true; }
                    }
                }
                return false;
            }
        }
      
        #endregion

        #region 静态方法定义
        /// <summary>
        /// 获取项目起始路径
        /// </summary>
        /// <returns></returns>
        public static string GetProjectDirectory()
        {
            //获取应用程序路径，使用下面两条语句均可
            string appDirectory = System.Environment.CurrentDirectory;
            //string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string proDirectory;
            int index = appDirectory.IndexOf(@"\Debug");
            proDirectory = appDirectory.Substring(0, index);
            index = proDirectory.IndexOf(@"\bin");
            proDirectory = proDirectory.Substring(0, index);
            return proDirectory;
        }

        /// <summary>
        /// 获得目标系统文件夹路径
        /// </summary>
        /// <returns></returns>
        //public static string GetSubSysDirectory()
        //{
        //    string proDirectory = GetProjectDirectory();
        //    string subSysDirectory = proDirectory + @"\SubjectSys";
        //    return subSysDirectory;
        //}

        /// <summary>
        /// 根据文件名，读取文件内容到ArrayList中
        /// </summary>
        /// <param name="filename">包含绝对路径的完整文件名</param>
        /// <returns></returns>
        public static List<string> GetFileContent(string fullFileName)
        {
            List<string> content = new List<string>();
            try
            {
                StreamReader reader = new StreamReader(fullFileName, Encoding.Default);
                string str = reader.ReadLine();
                while (str != null)
                {
                    content.Add(str);
                    str = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
            
            return content;
        }

        /// <summary>
        /// 处理字符串中多余的空格符，修改回车(\r\n)为换行(\n)，及删除多余的\t
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string TrimChars(string s)
        {
            //注：Unix 系统里，每行结尾只有“<换行>”，即“\n”；Windows系统里面，每行结尾是“<回车><换行>”，即“ \r\n”
            s = s.Replace("\r", "");    //修改回车(\r\n)为换行(\n)
            s = Regex.Replace(s, "\\s+", " ");  //删除多余的空格
            s = Regex.Replace(s, "\\t+", " ");  //替换一个或多个制表符为空格
            s.Trim();   //删除开头和结尾的空格
            return s;
        }

        /// <summary>
        /// 计算两个字符串的相似度。完全相同时为1，完全不同（LCS长度为0）时为0
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <returns>0~1之间的浮点数</returns>
        public static float GetStringSimilarity(string strA, string strB)
        {
            #region 使用LevenshteinDistance
            LevenshteinDistance ld = new LevenshteinDistance();
            return ld.LevenshteinDistancePercent(strA, strB);
            #endregion
        }

        /// <summary>
        /// 从一段字符串中提取出单词
        /// </summary>
        /// <param name="str">待提取的字符串</param>
        /// <returns>返回ArrayList对象的的单词列表</returns>
        //    public static ArrayList GetWords(string str)
        //    {
        //        if (str == "")
        //        { return null; }
        //        else
        //        {
        //            ArrayList words = new ArrayList();
        //            str = TrimChars(str);
        //            int indexSpace = str.IndexOf(" ");  //空格的位置
        //            if (indexSpace == -1)   //只有一个单词
        //            {
        //                words.Add(str);
        //            }
        //            else
        //            {
        //                string curWord, otherWords;
        //                otherWords = str;
        //                do
        //                {
        //                    indexSpace = otherWords.IndexOf(" ");
        //                    if (indexSpace != -1)
        //                    {
        //                        curWord = otherWords.Substring(0, indexSpace).Trim();
        //                        otherWords = otherWords.Substring(indexSpace).Trim();
        //                    }
        //                    else
        //                    { curWord = otherWords; otherWords = ""; }
        //                    words.Add(curWord);
        //                } while (otherWords != "");
        //            }
        //            return words;
        //        }
        //    }
        //} 
        #endregion
    }
    
}
