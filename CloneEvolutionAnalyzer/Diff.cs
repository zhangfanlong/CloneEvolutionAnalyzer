using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace CloneEvolutionAnalyzer
{
    //LCS中项的结构
    public struct LCSItem
    {
        public bool isExactSame;    //标记此项是否是完全相同或者有修改
        public string lineContent; //相同的行的内容
        public int lineOfFileA;    //该行在FileA中的行号（索引号+1）
        public int lintOfFileB;    //该行在FileB中的行号（索引号+1）
    }
    /// <summary>
    /// ArrayListLCS类，完成两个ArrayList的LCS的计算
    /// </summary>
    public class ListStringLCS   //注：有时间完善计算LCS出现的误差
    {
        public List<string> strA { get; set; }
        public List<string> strB { get; set; }
        public int length;
        //pointerArray是存放标记的二维数组，U表示向上，L表示向左，Y表示向左上，\0表示空（未赋值）
        private char[,] pointerArray;
        //LengthArray是存放lcs长度的二维数组
        private int[,] lengthArray;
        public ArrayList lcs;

        /// <summary>
        /// Constructor method
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public ListStringLCS(List<string> a, List<string> b)
        {
            strA = new List<string>(a);
            strB = new List<string>(b);
        }

        /// <summary>
        /// 获得strA与strB的LCS，保存在lcs成员中，并保存LCS长度在length成员中
        /// </summary>
        public void GetLCS()
        {
            this.length = LCSLength(this.strA, this.strB);
            this.lcs = new ArrayList();
            PrintLCS(strA.Count, strB.Count, this.length);
        }

        /// <summary>
        /// 打印（记录）LCS的项，采用递归方法。使用LCS的MarkArray成员，更新lcs成员
        /// </summary>
        /// <param name="indexA"></param>
        /// <param name="indexB"></param>
        /// <param name="indexLCS">这个参数提供当前打印的LCS项的索引号</param>
        private void PrintLCS(int indexA, int indexB, int indexLCS)
        {
            if (indexA == 0 || indexB == 0)
            { return; }
            if (this.pointerArray[indexA, indexB] == 'Y')
            {
                PrintLCS(indexA - 1, indexB - 1, indexLCS - 1);
                LCSItem newItem = new LCSItem();
                //如果是完全相同的项，存入其中一个即可
                if (((string)strA[indexA - 1]).Equals((string)strB[indexB - 1]))
                {
                    newItem.isExactSame = true;
                    newItem.lineContent = (string)strA[indexA - 1];
                }
                else//修改的项，将两个连接到一起，存入。用$DIVIDER$串分隔
                {
                    newItem.isExactSame = false;
                    newItem.lineContent = (string)strA[indexA - 1] + "$DIVIDER$" + (string)strB[indexB - 1];
                }
                newItem.lineOfFileA = indexA;   //index也是从1开始，因此不需要+1
                newItem.lintOfFileB = indexB;
                //this.lcs[indexLCS] = newItem;
                this.lcs.Add(newItem);
            }
            else if (this.pointerArray[indexA, indexB] == 'U')
            { PrintLCS(indexA - 1, indexB, indexLCS); }
            else
            { PrintLCS(indexA, indexB - 1, indexLCS); }
        }

        /// <summary>
        /// 计算strA和strB的LCS长度，动态规划方法，非递归。使用并更新LCS的MarkArray和LenghthArray成员
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <returns>返回LCS长度</returns>
        private int LCSLength(List<string> strA, List<string> strB)   //注：这个方法还存在缺陷，会导致误差！
        {
            if (strA.Count == 0 || strB.Count == 0)
            { return 0; }
            //初始化两个二维数组的结构
            this.pointerArray = new char[strA.Count + 1, strB.Count + 1];
            this.lengthArray = new int[strA.Count + 1, strB.Count + 1];
            int i, j;
            //置第一行和第一列值
            for (j = 0; j < strB.Count + 1; j++)
            {
                this.lengthArray[0, j] = 0;
                this.pointerArray[0, j] = '\0';
            }
            for (i = 0; i < strA.Count + 1; i++)
            {
                this.lengthArray[i, 0] = 0;
                this.pointerArray[i, 0] = '\0';
            }
            //开始扫描
            for (i = 1; i < strA.Count + 1; i++)
            {
                string a = strA[i - 1].ToString();
                a = Global.TrimChars(a);  //处理空格，回车，制表符等字符
                for (j = 1; j < strB.Count + 1; j++)
                {
                    string b = strB[j - 1].ToString();
                    b = Global.TrimChars(b);
                    //两行完全相同，或不完全相同，但相似度高于阈值
                    if (a.Equals(b) || Global.GetStringSimilarity(a, b) >= Diff.StrSimThreshold)
                    {
                        this.pointerArray[i, j] = 'Y';
                        this.lengthArray[i, j] = this.lengthArray[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        if (this.lengthArray[i - 1, j] >= this.lengthArray[i, j - 1])
                        {
                            this.pointerArray[i, j] = 'U';
                            this.lengthArray[i, j] = this.lengthArray[i - 1, j];
                        }
                        else
                        {
                            this.pointerArray[i, j] = 'L';
                            this.lengthArray[i, j] = this.lengthArray[i, j - 1];
                        }
                    }
                }
            }
            return this.lengthArray[strA.Count, strB.Count];
        }
    }

    /// <summary>
    /// Diff类，完成两个ArrayList对象的Diff计算。其中使用到ArrayListLCS类的功能
    /// </summary>
    public class Diff
    {
        #region 非静态属性
        //public ArrayList FileA { get; set; }
        //public ArrayList FileB { get; set; }
        //public ArrayList DiffFile { get; set; }
        //public ArrayList MergeFile { get; set; }
        //public float FileSimilarity{get;set;}
        //public int confilctCount{get;set;} 
        #endregion

        #region 静态数据及函数成员
        public static float StrSimThreshold;   //定义字符串（行）相似度阈值
        public static void SetStrSimThreshode(float value) //设定相似度阈值
        { StrSimThreshold = value; }
        public static void UseDefaultStrSimTh()    //使用默认相似度阈值
        { StrSimThreshold = (float)0.5; }
        #endregion

        public enum ConfictType
        {
            MODIFIED = 0,
            ADD,
            DELETE
        }
        public class ConfictItem
        {
            //冲突的类型，如果ADD，则contentA为空；如果为DELETE，则contentB为空；如果为MODIFIED，两者都不空
            public ConfictType type;
            public List<string> contentA;
            public List<string> contentB;
        }
        public class DiffInfo : ArrayList { }   //定义DiffInfo类型
        //非静态diff方法
        //public void StartDiff()
        //{
        //}

        /// <summary>
        /// 静态diff方法
        /// </summary>
        /// <param name="fileA"></param>
        /// <param name="fileB"></param>
        /// <returns></returns>
        public static DiffInfo DiffFiles(List<string> fileA, List<string> fileB)
        {
            DiffInfo diffFile = new DiffInfo();
            ListStringLCS lcsObject = new ListStringLCS(fileA, fileB);   //创建一个LCS类的对象
            lcsObject.GetLCS();
            int lineNoA, lineNoB, prevLineNoA, prevLineNoB;
            prevLineNoA = 0;
            prevLineNoB = 0;
            lineNoA = 0;
            lineNoB = 0;
            //将LCS中的项及冲突项依次加入diffFile中
            foreach (LCSItem lcsItem in lcsObject.lcs)
            {
                int lcsIndex = lcsObject.lcs.IndexOf(lcsItem);
                lineNoA = lcsItem.lineOfFileA;
                lineNoB = lcsItem.lintOfFileB;
                if (lineNoA - prevLineNoA == 1 && lineNoB - prevLineNoB == 1)
                {
                }
                else if (lineNoA - prevLineNoA == 1 && lineNoB - prevLineNoB > 1)//增加的情况
                {
                    ConfictItem cItem = new ConfictItem();
                    cItem.type = ConfictType.ADD;
                    cItem.contentA = null;
                    cItem.contentB = new List<string>();
                    for (int i = prevLineNoB; i < lineNoB - 1; i++)    //将fileB中增加的行加入此项
                    { cItem.contentB.Add(fileB[i]); }
                    diffFile.Add(cItem);
                    //prevLineNoA = lineNoA;  //prev指针下移
                    //prevLineNoB = lineNoB;
                }
                else if (lineNoA - prevLineNoA > 1 && lineNoB - prevLineNoB == 1) //删除的情况
                {
                    ConfictItem cItem = new ConfictItem();
                    cItem.type = ConfictType.DELETE;
                    cItem.contentA = new List<string>();
                    cItem.contentB = null;
                    for (int i = prevLineNoA; i < lineNoA - 1; i++)    //将fileA中删除的行加入此项
                    { cItem.contentA.Add(fileA[i]); }
                    diffFile.Add(cItem);
                    //prevLineNoA = lineNoA;  //prev指针下移
                    //prevLineNoB = lineNoB;
                }
                //lineNoA - prevLineNoA > 1 && lineNoB - prevLineNoB > 1，同时有增加和删除
                else
                {
                    #region 按先后顺序将两边的冲突项加入diffFile
                    //下面分支的作用是将行号靠前的一边构造的冲突项先加入diffFile
                    if (prevLineNoA >= prevLineNoB)
                    {
                        //先加入左边删除项
                        ConfictItem cItem = new ConfictItem();
                        cItem.type = ConfictType.DELETE;
                        cItem.contentA = new List<string>();
                        cItem.contentB = null;
                        for (int i = prevLineNoA; i < lineNoA - 1; i++)    //将fileA中删除的行加入contentA
                        { cItem.contentA.Add(fileA[i]); }
                        diffFile.Add(cItem);
                        //后加入右边增加项
                        cItem = new ConfictItem();
                        cItem.type = ConfictType.ADD;
                        cItem.contentA = null;
                        cItem.contentB = new List<string>();
                        for (int i = prevLineNoB; i < lineNoB - 1; i++)
                        { cItem.contentB.Add(fileB[i]); }   //将fileB中增加的行加入contentB
                        diffFile.Add(cItem);
                    }
                    else
                    {
                        //先加入右边增加项
                        ConfictItem cItem = new ConfictItem();
                        cItem.type = ConfictType.ADD;
                        cItem.contentA = null;
                        cItem.contentB = new List<string>();
                        for (int i = prevLineNoB; i < lineNoB - 1; i++)
                        { cItem.contentB.Add(fileB[i]); }
                        diffFile.Add(cItem);
                        //先加入左边删除项
                        cItem = new ConfictItem();
                        cItem.type = ConfictType.DELETE;
                        cItem.contentB = null;
                        cItem.contentA = new List<string>();
                        for (int i = prevLineNoA; i < lineNoA - 1; i++)
                        { cItem.contentA.Add(fileA[i]); }
                        diffFile.Add(cItem);
                    }
                    #endregion
                    //prevLineNoA = lineNoA;  //prev指针下移
                    //prevLineNoB = lineNoB;
                }
                #region 将LCS中的项加入diffFile
                if (lcsItem.isExactSame) //完全相同的项直接加入diffFile
                {
                    diffFile.Add(lcsItem.lineContent.ToString());
                    prevLineNoA = lineNoA;  //prev指针下移
                    prevLineNoB = lineNoB;
                }
                else//有修改的项作为冲突项加入diffFile
                {
                    ConfictItem cItem = new ConfictItem();
                    cItem.type = ConfictType.MODIFIED;
                    cItem.contentA = new List<string>();
                    cItem.contentB = new List<string>();
                    do//使用循环将连续的有修改的项合并到一个冲突项
                    {
                        //分隔符前面的内容加入contentA，后面的内容加入contentB
                        int indexDiv = lcsItem.lineContent.ToString().IndexOf("$DIVIDER$");
                        cItem.contentA.Add(lcsItem.lineContent.ToString().Substring(0, indexDiv));
                        cItem.contentB.Add(lcsItem.lineContent.ToString().Substring(indexDiv + 9));
                        diffFile.Add(cItem);    //将冲突项加入diffFile
                        prevLineNoA = lineNoA;  //prev指针下移
                        prevLineNoB = lineNoB;
                    } while (lineNoA - prevLineNoA == 1 && lineNoB - prevLineNoB == 1 && !lcsItem.isExactSame);
                }
                #endregion
            }
            #region 处理剩余项
            if (lineNoA < fileA.Count)  //如果还有删除项
            {
                ConfictItem cItem = new ConfictItem();
                cItem.type = ConfictType.DELETE;
                cItem.contentA = new List<string>();
                cItem.contentB = null;
                for (int i = lineNoA; i < fileA.Count; i++)
                { cItem.contentA.Add(fileA[i]); }
                diffFile.Add(cItem);
            }
            if (lineNoB < fileB.Count)  //如果还有增加项
            {
                ConfictItem cItem = new ConfictItem();
                cItem.type = ConfictType.ADD;
                cItem.contentA = null;
                cItem.contentB = new List<string>();
                for (int i = lineNoB; i < fileB.Count; i++)
                { cItem.contentB.Add(fileB[i]); }
            }
            #endregion

            return diffFile;
        }

        //非静态Merge方法（空）
        public void StartMerge()
        {

        }

        //静态Merge方法（空）
        public static List<string> MergeFiles(List<string> fileA, List<string> fileB)
        {
            List<string> mergeFile = new List<string>();

            return mergeFile;
        }

        //非静态计算文件相似度方法（空）
        /// <summary>
        /// 非静态计算文件相似度方法
        /// </summary>
        /// <param name="fileA"></param>
        /// <param name="fileB"></param>
        public void GetFileSimilarity(List<string> fileA, List<string> fileB)
        {
        }

        /// <summary>
        /// 静态计算文件相似度方法，根据diffFile信息。因为diffFile中不提供两个文件的长度，因此用参数提供
        /// </summary>
        /// <param name="diffFile"></param>
        /// <param name="lengthA"></param>
        /// <param name="lengthB"></param>
        /// <param name="ignoreEmptyLine"></param>
        /// <returns></returns>
        public static float FileSimilarity(DiffInfo diffFile, int lengthA, int lengthB, bool ignoreEmptyLine)
        {
            int uniLineCountA = 0;
            int uniLineCountB = 0;
            int emptyLineCount = 0;
            foreach (Object cItem in diffFile)
            {
                if (cItem is ConfictItem)
                {
                    if (((ConfictItem)cItem).contentA != null)
                    {
                        foreach (string line in ((ConfictItem)cItem).contentA)
                        {
                            uniLineCountA++;    //统计FileA中冲突项的行数
                            if (line.Trim() == "")
                            { emptyLineCount++; }   //统计空行的数量
                        }
                    }
                    if (((ConfictItem)cItem).contentB != null)
                    {
                        foreach (string line in ((ConfictItem)cItem).contentB)
                        {
                            uniLineCountB++;    //统计FileB中冲突项的行数 
                            if (line.Trim() == "")
                            { emptyLineCount++; }
                        }
                    }
                }
            }
            if (ignoreEmptyLine)
            { return (float)1 - ((float)(uniLineCountA + uniLineCountB - emptyLineCount) / (float)(lengthA + lengthB - emptyLineCount)); }
            else
            { return (float)1 - ((float)(uniLineCountA + uniLineCountB) / (float)(lengthA + lengthB)); }
        }

        /// <summary>
        /// 静态方法，将Diff保存到文本文件
        /// </summary>
        /// <param name="diffFile">调用DiffFiles方法生成的diffFile</param>
        /// <param name="fullName">用户输入的diff文件的完整路径</param>
        public static void SaveDiffToText(DiffInfo diffFile, string fullName)
        {
            StreamWriter writer = new StreamWriter(fullName);
            foreach (Object item in diffFile)
            {
                if (item is string)
                { writer.WriteLine(item); }
                else
                {
                    writer.WriteLine("/****Conflict Lines****/!");
                }
            }
            writer.Close();
        }

        /// <summary>
        /// 在Diff窗口中显示Diff信息
        /// </summary>
        /// <param name="diffInfo">DiffInfo对象</param>
        public static void ShowDiffInWindow(DiffInfo diffInfo)
        { }
    }
}
