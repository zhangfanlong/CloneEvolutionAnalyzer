using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CloneEvolutionAnalyzer
{
    //HalsteadMetric类。注：为了区分操作符（包含关键字）与运算符，用OPERATOR表示操作符，用Operator表示运算符
    public class HalsteadMetric
    {

        #region 静态成员定义
        //定义各种语言共有关键字（具有操作符的意义的关键字）
        public static List<string> _commonKeyword;  //使用当所有项类型相同时泛型集合类List<T>,优于非泛型集合类ArrayList
        public static void InitCommonKeywordCollection()
        {
            _commonKeyword = new List<string>();
            _commonKeyword.Add("break");
            _commonKeyword.Add("case");
            _commonKeyword.Add("catch");
            _commonKeyword.Add("continue");
            _commonKeyword.Add("do");
            _commonKeyword.Add("else");
            _commonKeyword.Add("finally");
            _commonKeyword.Add("for");
            _commonKeyword.Add("goto");
            _commonKeyword.Add("if");
            _commonKeyword.Add("return");
            _commonKeyword.Add("sizeof");
            _commonKeyword.Add("switch");
            _commonKeyword.Add("try");
            _commonKeyword.Add("while");
        }
        //定义某种语言特有关键字(考虑是否有必要？)
        public static List<string> _specialKeyword;
        public static void InitSpecialKeywordCollection()
        {
            _specialKeyword = new List<string>();
            _specialKeyword.Add("typedef"); //C&C++特有
            _specialKeyword.Add("new");    //C++&Java&C#特有
        }
        //定义各种语言共有运算符
        public static List<string> _commonOperator;
        public static void InitCommonOperatorCollection()
        {
            _commonOperator = new List<string>();
            #region 按类别排序（只做参考）
            ////赋值运算符
            //_commonOperator.Add("=");
            //_commonOperator.Add("+=");
            //_commonOperator.Add("-=");
            //_commonOperator.Add("*=");
            //_commonOperator.Add("/=");
            //_commonOperator.Add("%=");
            //_commonOperator.Add(">>=");
            //_commonOperator.Add("<<=");
            //_commonOperator.Add("&=");
            //_commonOperator.Add("|=");
            //_commonOperator.Add("^=");
            ////算术运算符
            //_commonOperator.Add("+");
            //_commonOperator.Add("-");
            //_commonOperator.Add("*");
            //_commonOperator.Add("/");
            //_commonOperator.Add("%");
            //_commonOperator.Add("++");
            //_commonOperator.Add("--");
            ////关系运算符
            //_commonOperator.Add("==");
            //_commonOperator.Add("!=");
            //_commonOperator.Add("<");
            //_commonOperator.Add(">");
            //_commonOperator.Add("<=");
            //_commonOperator.Add(">=");
            ////逻辑运算符
            //_commonOperator.Add("&&");
            //_commonOperator.Add("||");
            //_commonOperator.Add("!");
            ////条件运算符??如何表示有间隔的运算符，用正则表达式？？
            //_commonOperator.Add("?:");
            ////位运算符
            //_commonOperator.Add("&");
            //_commonOperator.Add("|");
            //_commonOperator.Add("^");
            //_commonOperator.Add("~");
            //_commonOperator.Add("<<");
            //_commonOperator.Add(">>");
            ////其他运算符
            //_commonOperator.Add(".");
            //_commonOperator.Add("()");
            //_commonOperator.Add("[]");
            #endregion

            #region 按检测需要排序
            //包含"="的运算符(0-14)
            _commonOperator.Add("=");
            _commonOperator.Add("==");
            _commonOperator.Add("+=");
            _commonOperator.Add("-=");
            _commonOperator.Add("*=");
            _commonOperator.Add("/=");
            _commonOperator.Add("%=");
            _commonOperator.Add("&=");
            _commonOperator.Add("|=");
            _commonOperator.Add("^=");
            _commonOperator.Add("!=");
            _commonOperator.Add("<<="); //与"<="的顺序不可换
            _commonOperator.Add("<=");
            _commonOperator.Add(">>="); //与">="的顺序不可换
            _commonOperator.Add(">=");
            //包含"<"(15,16)
            _commonOperator.Add("<");
            _commonOperator.Add("<<");
            //包含">"(17,18)
            _commonOperator.Add(">");
            _commonOperator.Add(">>");
            //包含"+"(19,20)
            _commonOperator.Add("+");
            _commonOperator.Add("++");
            //包含"-"(21,22)
            _commonOperator.Add("-");
            _commonOperator.Add("--");
            //包含"&"(23,24)
            _commonOperator.Add("&");
            _commonOperator.Add("&&");
            //包含"|"(15,26)
            _commonOperator.Add("|");
            _commonOperator.Add("||");
            //单个字符的运算符(27-33)
            _commonOperator.Add("*");
            _commonOperator.Add("/");
            _commonOperator.Add("%");
            _commonOperator.Add("!");
            _commonOperator.Add("^");
            _commonOperator.Add("~");
            _commonOperator.Add(".");
            //中间有字符的运算符(34-36)
            _commonOperator.Add("?:");
            _commonOperator.Add("()");
            _commonOperator.Add("[]");
            #endregion
        }
        //定义某种语言特有运算符
        public static List<string> _specialOperator;
        public static void InitSpecialOperatorCollection()
        {
            _specialOperator = new List<string>();
            //_specialOperator.Add("*");  //C&C++特有
            _specialOperator.Add("->"); //C&C++特有
            _specialOperator.Add(">>>");    //Java特有,0填充的右移？
            _specialOperator.Add("instanceof"); //Java特有,对象运算符
            //_specialOperator.Add("?");  //C#特有，非空类型标记（算运算符么？？），暂不考虑
            //_specialOperator.Add("??"); //C#特有，？？？，暂不考虑
            _specialOperator.Add("is"); //C#特有
            _specialOperator.Add("as"); //C#特有
            //_specialOperator.Add("::"); //C++,Java,C#特有,要考虑么？？？，暂不考虑
        }
        //统一初始化上述成员
        public static void InitHalsteadParam()
        {
            InitCommonKeywordCollection();
            InitSpecialKeywordCollection();
            InitCommonOperatorCollection();
            InitSpecialOperatorCollection();
        }
        #endregion

        #region 非静态数据及属性成员定义
        //唯一操作符数量，即操作符种类
        private int uniOPERATORCount;
        public int UniOPERATORCount
        {
            get { return uniOPERATORCount; }
        }
        //唯一操作数数量
        private int uniOperandCount;
        public int UniOperandCount
        {
            get { return uniOperandCount; }
        }
        //操作符总量
        private int totalOPERATORCount;
        public int TotalOPERATORCount
        {
            get { return totalOPERATORCount; }
        }
        //操作数总量
        private int totalOperandCount;
        public int TotalOperandCount
        {
            get { return totalOperandCount; }
        } 
        #endregion

        public HalsteadMetric()
        {
            uniOperandCount = 0;
            uniOPERATORCount = 0;
            totalOperandCount = 0;
            totalOPERATORCount = 0;
        }

        /// <summary>
        /// 获取代码段的Halstead度量，包括操作符及操作数的信息
        /// </summary>
        /// <param name="codeFragment"></param>
        public void GetFromCode(List<string> codeFragment)
        {
            List<string> newCodeFragment = PreProcessing.IngoreComments(codeFragment);
            if (Options.Language == ProgLanguage.Java)
            { newCodeFragment = PreProcessing.IgnoreImportLines(newCodeFragment); }
            if (Options.Language == ProgLanguage.CSharp)
            { newCodeFragment = PreProcessing.IgnoreUsingLines(newCodeFragment); }
            newCodeFragment = PreProcessing.IgnoreString(newCodeFragment);               //added by edward...for extracting Halstead metrics
            GetOPERATORCountFromCode(newCodeFragment);
            GetOperandCountFromCode(newCodeFragment);
        }

        /// <summary>
        /// 计算代码段的操作符信息
        /// </summary>
        /// <param name="codeFragment">以string泛型类表示的源代码片段</param>
        private void GetOPERATORCountFromCode(List<string> codeFragment)
        {
            //用到的数组（统计各种关键字和运算符出现的次数）
            int[] comKeyCounter = new int[_commonKeyword.Count];
            int[] specKeyCounter = new int[_specialKeyword.Count];
            int[] comOptorCounter = new int[_commonOperator.Count];
            int[] specOptorCounter = new int[_specialOperator.Count];

            foreach (string line in codeFragment)
            {
                int keyIndexHead, keyIndexTail;  //关键字起止位置
                string curStr;  //用来对行进行分割检查
                string leftStr; //用来对行进行分割检查

                #region 统计关键字（包括common和special）
                foreach (string cKey in _commonKeyword)
                {
                    leftStr = line;
                    //分段检测，在一行中是否多次出现
                    while (leftStr.IndexOf(cKey) != -1)
                    {
                        keyIndexHead = leftStr.IndexOf(cKey);
                        keyIndexTail = keyIndexHead + cKey.Length - 1;

                        curStr = leftStr.Substring(0, keyIndexTail + 1);
                        leftStr = leftStr.Substring(keyIndexTail + 1);
                        //如果真的包含关键字
                        //string regexKey=".\\W"+cKey+"\\W.";
                        //if(Regex.IsMatch(leftStr,regexKey)
                        if ((keyIndexHead == 0 || !Regex.IsMatch(curStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                            (keyIndexTail == curStr.Length - 1 || !Regex.IsMatch(curStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                        {
                            comKeyCounter[_commonKeyword.IndexOf(cKey)]++;
                        }
                    }
                }
                foreach (string sKey in _specialKeyword)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(sKey) != -1)
                    {
                        keyIndexHead = leftStr.IndexOf(sKey);
                        keyIndexTail = keyIndexHead + sKey.Length - 1;

                        curStr = leftStr.Substring(0, keyIndexTail + 1);
                        leftStr = leftStr.Substring(keyIndexTail + 1);
                        //如果真的包含关键字
                        if ((keyIndexHead == 0 || !Regex.IsMatch(curStr[keyIndexHead - 1].ToString(), @"[A-Za-z0-9_]")) &&
                            (keyIndexTail == curStr.Length - 1 || !Regex.IsMatch(curStr[keyIndexTail + 1].ToString(), @"[A-Za-z0-9_]")))
                        {
                            specKeyCounter[_specialKeyword.IndexOf(sKey)]++;
                        }
                    }
                }
                #endregion

                int opIndex;    //记录当前处理的操作符的位置

                #region 检测包含"="的运算符系列
                if (line.IndexOf("=") != -1)
                {
                    leftStr = line;
                    bool flag;
                    while (leftStr.IndexOf("=") != -1) //以"="为边界，分段检测，是否多次出现
                    {
                        flag = false;
                        opIndex = leftStr.IndexOf("=");
                        curStr = leftStr.Substring(0, opIndex + 1);    //当前处理的部分 // opIndex + 2 => opIndex + 1
                        leftStr = leftStr.Substring(opIndex + 1);  //剩余部分（+2的原因） // opIndex + 2 => opIndex + 1
                        for (int i = 1; i <= 14; i++)
                        {
                            if (curStr.IndexOf(_commonOperator[i]) != -1)
                            {
                                comOptorCounter[i]++;
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)  //如果只是包含"="
                        {
                            comOptorCounter[_commonOperator.IndexOf("=")]++;
                        }
                    }
                }
                #endregion

                #region 检测包含"<",">"的系列
                //检测"<"
                if (line.IndexOf("<") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("<") != -1)
                    {
                        opIndex = leftStr.IndexOf("<");
                        curStr = leftStr.Substring(0, opIndex + 3);
                        leftStr = leftStr.Substring(opIndex + 3);
                        if (curStr.IndexOf("<<") != -1 && curStr.IndexOf("<<=") == -1)  //排除"<<="
                        { comOptorCounter[_commonOperator.IndexOf("<<")]++; }
                        else
                        {
                            if (curStr.IndexOf("<=") == -1 && curStr.IndexOf("<<=") == -1) //排除"<="和"<<="
                            { comOptorCounter[_commonOperator.IndexOf("<")]++; }
                        }
                    }
                }
                //检测">"
                if (line.IndexOf(">") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(">") != -1)
                    {
                        opIndex = leftStr.IndexOf(">");
                        if (opIndex == (leftStr.Length - 1))
                            break;
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf(">>") != -1 && curStr.IndexOf(">>=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf(">>")]++; }
                        else
                        {
                            if (curStr.IndexOf(">=") == -1 && curStr.IndexOf(">>=") == -1) //排除">="和">>="，因为已经处理过
                            { comOptorCounter[_commonOperator.IndexOf(">")]++; }
                        }
                    }
                }
                #endregion

                #region 检测"+","-","&","|"系列
                //包含"+"的情况
                if (line.IndexOf("+") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("+") != -1)
                    {
                        opIndex = leftStr.IndexOf("+");
                        curStr = leftStr.Substring(0, opIndex + 1);
                        leftStr = leftStr.Substring(opIndex + 1);
                        if (curStr.IndexOf("++") != -1)
                        { comOptorCounter[_commonOperator.IndexOf("++")]++; }
                        else
                        {
                            if (curStr.IndexOf("+=") == -1)
                            { comOptorCounter[_commonOperator.IndexOf("+")]++; }
                        }
                    }
                }
                //包含"-"的情况
                if (line.IndexOf("-") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("-") != -1)
                    {
                        opIndex = leftStr.IndexOf("-");
                        if (leftStr.Length == opIndex + 1)
                        {
                            comOptorCounter[_commonOperator.IndexOf("-")]++;
                            break;
                        }
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("--") != -1)
                        { comOptorCounter[_commonOperator.IndexOf("--")]++; }
                        else
                        {
                            if (curStr.IndexOf("-=") == -1)
                            { comOptorCounter[_commonOperator.IndexOf("-")]++; }
                        }
                    }
                }
                //包含"&"的情况
                if (line.IndexOf("&") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("&") != -1)
                    {
                        opIndex = leftStr.IndexOf("&");
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("&&") != -1)
                        { comOptorCounter[_commonOperator.IndexOf("&&")]++; }
                        else
                        {
                            if (curStr.IndexOf("&=") == -1)
                            { comOptorCounter[_commonOperator.IndexOf("&")]++; }
                        }
                    }
                }
                //包含"|"的情况
                if (line.IndexOf("|") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("|") != -1)
                    {
                        opIndex = leftStr.IndexOf("|");
                        curStr = leftStr.Substring(0, opIndex + 1);//altered by edward
                        leftStr = leftStr.Substring(opIndex + 1);
                        if (curStr.IndexOf("||") != -1)
                        { comOptorCounter[_commonOperator.IndexOf("||")]++; }
                        else
                        {
                            if (curStr.IndexOf("|=") == -1)
                            { comOptorCounter[_commonOperator.IndexOf("|")]++; }
                        }
                    }
                }
                #endregion

                #region 检测"*","/","%","!","^","~"系列
                //包含"*"
                if (line.IndexOf("*") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("*") != -1)
                    {
                        opIndex = leftStr.IndexOf("*");
                        if (opIndex == (leftStr.Length - 1))
                            break;
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("*=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("*")]++; }
                    }
                }
                //包含"/"
                if (line.IndexOf("/") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("/") != -1)
                    {
                        opIndex = leftStr.IndexOf("/");
                        if (opIndex == (leftStr.Length - 1))
                            break;
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("/=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("/")]++; }
                    }
                }
                //包含"%"
                if (line.IndexOf("%") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("%") != -1)
                    {
                        opIndex = leftStr.IndexOf("%");
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("%=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("%")]++; }
                    }
                }
                //包含"!"
                if (line.IndexOf("!") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("!") != -1)
                    {
                        opIndex = leftStr.IndexOf("!");
                        curStr = leftStr.Substring(0, opIndex + 1);
                        leftStr = leftStr.Substring(opIndex + 1);
                        if (curStr.IndexOf("!=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("!")]++; }
                    }
                }
                //包含"^"
                if (line.IndexOf("^") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("^") != -1)
                    {
                        opIndex = leftStr.IndexOf("^");
                        curStr = leftStr.Substring(0, opIndex + 1);
                        leftStr = leftStr.Substring(opIndex + 1);
                        if (curStr.IndexOf("^=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("^")]++; }
                    }
                }
                //包含"~"
                if (line.IndexOf("~") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("~") != -1)
                    {
                        opIndex = leftStr.IndexOf("~");
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        if (curStr.IndexOf("~=") == -1)
                        { comOptorCounter[_commonOperator.IndexOf("~")]++; }
                    }
                }
                #endregion

                #region 检测"."
                if (line.IndexOf(".") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(".") != -1)
                    {
                        opIndex = leftStr.IndexOf(".");
                        if (leftStr.Length == opIndex + 1)
                        {
                            comOptorCounter[_commonOperator.IndexOf(".")]++;
                            break;
                        }
                        curStr = leftStr.Substring(0, opIndex + 2);//altered by edward
                        leftStr = leftStr.Substring(opIndex + 1);
                        //检查"."前后是否都是数字
                        if (!Regex.IsMatch(curStr.Substring(opIndex - 1, 3), @"\d\.\d"))
                        { comOptorCounter[_commonOperator.IndexOf(".")]++; }
                    }
                }
                #endregion

                #region 检测"?:","()","[]"
                //检测"?:"
                if (line.IndexOf("?") != -1 && line.IndexOf(":") != -1)
                {
                    leftStr = line;
                    int index1, index2;  //分别保存"?"和":"的位置
                    while (leftStr.IndexOf("?") != -1 && leftStr.IndexOf(":") != -1)
                    {
                        index1 = leftStr.IndexOf("?");
                        index2 = leftStr.IndexOf(":");
                        if (leftStr.Length == index2 + 1)
                        {
                            comOptorCounter[_commonOperator.IndexOf("?:")]++;
                            break;
                        }
                        else if (index1 > index2)
                        {
                            comOptorCounter[_commonOperator.IndexOf("?:")]++;
                            break;
                        }
                        curStr = leftStr.Substring(0, index2 + 2);  //以":"进行分割
                        leftStr = leftStr.Substring(index2 + 2);
                        string str = curStr.Substring(index1, index2 - index1);
                        if (str.IndexOf(";") == -1) //只要"?"和":"中间没有";"（是否准确？？）
                        { comOptorCounter[_commonOperator.IndexOf("?:")]++; }   // "?;"=>"?;"
                    }
                }
                //检测"()"，考虑到（）嵌套等情况
                if (line.IndexOf("(") != -1 && line.IndexOf(")") != -1)
                {
                    int indexFirstL, indexLastR;  //获得第一个"("和最后一个")"的位置
                    indexFirstL = line.IndexOf("(");
                    indexLastR = line.LastIndexOf(")");
                    string str = line.Substring(indexFirstL, indexLastR - indexFirstL + 1); //截取中间的字符串
                    int lCount, coupleCount;    //记录未配对的左括号及括号对的数量，默认值都为0
                    lCount = 0;
                    coupleCount = 0;
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] == '(')
                        { lCount++; }
                        else if (str[i] == ')')
                        { lCount--; coupleCount++; }
                        else
                        { continue; }
                    }
                    comOptorCounter[_commonOperator.IndexOf("()")] += coupleCount;
                }

                //检测"[]"
                if (line.IndexOf("[") != -1 && line.IndexOf("]") != -1)
                {
                    int indexFirstL, indexLastR;  //获得第一个"("和最后一个")"的位置
                    indexFirstL = line.IndexOf("[");
                    indexLastR = line.LastIndexOf("]");
                    string str = line.Substring(indexFirstL, indexLastR - indexFirstL + 1); //截取中间的字符串
                    int lCount, coupleCount;    //记录未配对的左括号及括号对的数量，默认值都为0
                    lCount = 0;
                    coupleCount = 0;
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (str[i] == '[')
                        { lCount++; }
                        else if (str[i] == ']')
                        { lCount--; coupleCount++; }
                        else
                        { continue; }
                    }
                    comOptorCounter[_commonOperator.IndexOf("[]")] += coupleCount;
                }
                /*if (line.IndexOf("[") != -1 && line.IndexOf("]") != -1)
                {
                    leftStr = line;
                    int indexL, indexR;  //分别保存"?"和":"的位置
                    while (leftStr.IndexOf("[") != -1 && leftStr.IndexOf("]") != -1)
                    {
                        indexL = leftStr.IndexOf("[");
                        indexR = leftStr.IndexOf("]");
                        curStr = leftStr.Substring(0, indexR + 1);  //以":"进行分割
                        leftStr = leftStr.Substring(indexR + 1);
                        string str = curStr.Substring(indexL, indexR - indexL);
                        if (str.IndexOf(";") == -1) //只要"["和"]"中间没有";"（是否准确？？）
                        { comOptorCounter[_commonOperator.IndexOf("[]")]++; }
                    }
                }*/
                #endregion

                #region 检测"->","instanceof","is","as"
                //检测"->"
                if (line.IndexOf("->") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf("->") != -1)
                    {
                        opIndex = leftStr.IndexOf("<");
                        curStr = leftStr.Substring(0, opIndex + 2);
                        leftStr = leftStr.Substring(opIndex + 2);
                        specOptorCounter[_specialOperator.IndexOf("->")]++;
                    }
                }
                //检测"instanceof"
                if (line.IndexOf(" instanceof ") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(" instanceof ") != -1)
                    {
                        opIndex = leftStr.IndexOf(" instanceof ");
                        curStr = leftStr.Substring(0, opIndex + 12);
                        leftStr = leftStr.Substring(opIndex + 12);
                        specOptorCounter[_specialOperator.IndexOf("instanceof")]++;
                    }
                }
                //检测"is"
                if (line.IndexOf(" is ") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(" is ") != -1)
                    {
                        opIndex = leftStr.IndexOf(" is ");
                        curStr = leftStr.Substring(0, opIndex + 4);
                        leftStr = leftStr.Substring(opIndex + 4);
                        specOptorCounter[_specialOperator.IndexOf("is")]++;
                    }
                }
                //检测"as"
                if (line.IndexOf(" as ") != -1)
                {
                    leftStr = line;
                    while (leftStr.IndexOf(" as ") != -1)
                    {
                        opIndex = leftStr.IndexOf(" as ");
                        curStr = leftStr.Substring(0, opIndex + 4);
                        leftStr = leftStr.Substring(opIndex + 4);
                        specOptorCounter[_specialOperator.IndexOf("as")]++;
                    }
                }
                #endregion
            }

            #region 统计操作符种类及数量
            foreach (int keyCount in comKeyCounter)
            {
                if (keyCount > 0)
                {
                    this.uniOPERATORCount++;    //统计操作符种类
                    this.totalOPERATORCount += keyCount;    //统计操作符总量
                }
            }
            foreach (int keyCount in specKeyCounter)
            {
                if (keyCount > 0)
                {
                    this.uniOPERATORCount++;
                    this.totalOPERATORCount += keyCount;
                }
            }
            foreach (int optorCount in comOptorCounter)
            {
                if (optorCount > 0)
                {
                    this.uniOPERATORCount++;
                    this.totalOPERATORCount += optorCount;
                }
            }
            foreach (int optorCount in specOptorCounter)
            {
                if (optorCount > 0)
                {
                    this.uniOPERATORCount++;
                    this.totalOPERATORCount += optorCount;
                }
            }
            #endregion
        }

        /// <summary>
        /// 统计代码段的操作数信息
        /// </summary>
        /// <param name="codeFragment">以string泛型类表示的源代码片段</param>
        private void GetOperandCountFromCode(List<string> codeFragment)
        {
            //定义标识符正则表达式,\b匹配单词边界（除字母数字下划线外的所有字符）
            string regexOperand = @"\b[a-zA-Z_][a-zA-Z0-9_]*\b";
            List<string> operandList = new List<string>();  //用于存放操作数的表
            List<int> operandCounter = new List<int>(); //用来保存操作数出现次数的表
            bool flag;
            //使用Regex类的Matches方法，逐行查找操作数
            foreach (string line in codeFragment)
            {
                MatchCollection matches = Regex.Matches(line, regexOperand);
                if (matches.Count != 0)
                {
                    for (int i = 0; i < matches.Count; i++)
                    {
                        //检查标识符是否是关键字
                        if (Global.KeyWordsCollection.IsKeyWord(matches[i].Value))
                        { continue; }
                        else//如果不是关键字
                        {
                            flag = false;   //flag标记是否是已存在的操作数
                            int j = -1;
                            foreach (string curOp in operandList)
                            {
                                j++;
                                if (matches[i].Value == curOp)  //如果该操作数已存在
                                {
                                    flag = true;
                                    operandCounter[j]++;    //对应计数器+1
                                    break;
                                }
                            }
                            if (!flag)  //如果是新出现的操作数
                            {
                                operandList.Add(matches[i].Value);
                                operandCounter.Add(1);
                            }
                        }
                    }
                }
            }
            this.uniOperandCount = operandList.Count;   //唯一操作数数量
            foreach (int count in operandCounter)   //统计操作数总量
            {
                this.totalOperandCount += count;
            }
        }
    }
}
