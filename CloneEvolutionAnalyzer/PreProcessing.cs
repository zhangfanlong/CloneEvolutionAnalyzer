using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CloneEvolutionAnalyzer
{
    /// <summary>
    /// 预处理类，完成忽略注释等功能
    /// </summary>
    public class PreProcessing
    {
        /// <summary>
        /// 去除代码中的注释部分（用""代替，不改变行结构）
        /// </summary>
        /// <param name="codeFragment"></param>
        /// <returns>处理后的代码</returns>
        public static List<string> IngoreComments(List<string> codeFragment)
        {
            List<string> newCodeFragment = new List<string>(codeFragment);
            bool flag = false;  //标记当前代码是否处于"/*"与"*/"之间
            int index;
            for (int i = 0; i < codeFragment.Count; i++)
            {
                if (codeFragment[i].IndexOf("//") != -1)
                {
                    index = codeFragment[i].IndexOf("//");
                    if (index > 0)  //前面有其他字符
                    {
                        string prevStr = codeFragment[i].Substring(0, index);   //截取"//"前的字符串
                        MatchCollection matches = Regex.Matches(prevStr, "\""); //查找"的个数
                        if (matches.Count % 2 == 0) //如果"//"前有偶数个",则它是注释，否则，它是字符串常量的一部分
                        {
                            newCodeFragment[i] = Regex.Replace(codeFragment[i], "//.*", "");
                            continue;
                        }
                    }
                    else//前面没有其他字符
                    {
                        newCodeFragment[i] = Regex.Replace(codeFragment[i], "//.*", "");
                        continue;
                    }
                }
                if (codeFragment[i].IndexOf("/*") != -1)
                {
                    index = codeFragment[i].IndexOf("/*");
                    if (index > 0)
                    {
                        string prevStr = codeFragment[i].Substring(0, index);   //截取"/*"前的字符串
                        MatchCollection matches = Regex.Matches(prevStr, "\""); //查找"的个数
                        if (matches.Count % 2 == 0) //如果"/*"前有偶数个",则它是注释，否则，它是字符串常量的一部分
                        {
                            if (codeFragment[i].IndexOf("*/") != -1 && codeFragment[i].IndexOf("*/") > index) //如果本行有*/，且在/*之后
                            { newCodeFragment[i] = Regex.Replace(codeFragment[i], "/\\*.*\\*/", ""); }
                            else
                            {
                                flag = true;
                                newCodeFragment[i] = Regex.Replace(codeFragment[i], "/\\*.*", "");   //怎样使第一个*不被认为是匹配数量约束，使用\\*匹配*？？
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (codeFragment[i].IndexOf("*/") != -1 && codeFragment[i].IndexOf("*/") > index)
                        { newCodeFragment[i] = Regex.Replace(codeFragment[i], "/\\*.*\\*/", ""); }
                        else
                        {
                            flag = true;
                            newCodeFragment[i] = Regex.Replace(codeFragment[i], "/*.*", "");
                            continue;
                        }
                    }
                }
                if (flag && codeFragment[i].IndexOf("*/") != -1)
                {
                    flag = false;
                    newCodeFragment[i] = Regex.Replace(codeFragment[i], ".*\\*/", "");   //替换"*/"前面的字符为空
                    continue;
                }
                if (flag)
                {
                    newCodeFragment[i] = "";
                }
            }
            return newCodeFragment;
        }

        /// <summary>
        /// 去除代码中的import行（针对java代码，在必要时使用，如计算Halstead度量，使用此功能）
        /// </summary>
        /// <param name="codeFragment"></param>
        /// <returns>处理后的代码段</returns>
        public static List<string> IgnoreImportLines(List<string> codeFragment)
        {
            List<string> newCodeFragment = new List<string>(codeFragment);
            for (int i = 0; i < codeFragment.Count; i++)
            {
                //if (codeFragment[i].IndexOf("\bimport\b") != -1)   //IndexOf的参数可以使正则表达式么？？
                MatchCollection matches1 = Regex.Matches(codeFragment[i], @"\bimport\b");
                if (matches1.Count > 0)   //根据编程习惯，认为一行最多有一个import语句，因此不再做行的分段检测
                {
                    string prevStr = codeFragment[i].Substring(0, codeFragment[i].IndexOf("import "));  //如何定义import单词的左边界？？
                    MatchCollection matches = Regex.Matches(prevStr, "\"");
                    if (matches.Count % 2 == 0)
                    {
                        newCodeFragment[i] = Regex.Replace(codeFragment[i], @"\bimport.*;", "");
                    }
                }
            }
            return newCodeFragment;
        }

        /// <summary>
        /// 去除代码中的using行（针对C#代码，在必要时，如计算Halstead度量，使用此功能）
        /// </summary>
        /// <param name="codeFragment"></param>
        /// <returns>处理后的代码段</returns>
        public static List<string> IgnoreUsingLines(List<string> codeFragment)
        {
            List<string> newCodeFragment = new List<string>(codeFragment);
            for (int i = 0; i < codeFragment.Count; i++)
            {
                if (codeFragment[i].IndexOf("\busing\b") != -1)
                {
                    string prevStr = codeFragment[i].Substring(0, codeFragment[i].IndexOf("\busing\b"));
                    MatchCollection matches = Regex.Matches(prevStr, "\"");
                    if (matches.Count % 2 == 0)
                    {
                        newCodeFragment[i] = "";
                    }
                }
            }
            return newCodeFragment;
        }
        //added by edward
        //for extracting Halstead metrics
        //disgusting...
        public static List<string> IgnoreString(List<string> codeFragment)
        {
            List<string> newCodeFragment = new List<string>(codeFragment);
            for (int i = 0; i < codeFragment.Count; i++)
            {
                while (newCodeFragment[i].IndexOf("\"") != -1)
                {
                    int index = newCodeFragment[i].IndexOf("\"");
                    string prevStr = newCodeFragment[i].Substring(0, newCodeFragment[i].IndexOf("\""));
                    MatchCollection matches = Regex.Matches(prevStr, "\'");
                    if (matches.Count % 2 == 0)
                    {
                        int next_index = newCodeFragment[i].IndexOf("\"", index + 1);
                        if (next_index == -1)
                        {
                            newCodeFragment[i] = prevStr;
                            break;
                        }
                        int new_next_index = 0;
                        int num_of_backlash = 0;
                        for (int j = next_index - 1; j != index; j--)
                        {
                            if (newCodeFragment[i][j] != '\\')
                                break;
                            num_of_backlash++;
                        }
                        if (num_of_backlash % 2 == 1)
                        {
                            new_next_index = newCodeFragment[i].IndexOf("\"", next_index + 1);
                            next_index = new_next_index;
                        }
                        string leftStr = newCodeFragment[i].Substring(next_index + 1);
                        newCodeFragment[i] = prevStr + leftStr;
                    }
                    else
                        break;
                }
            }
            return newCodeFragment;
        }
    }
}
