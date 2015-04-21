using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CloneEvolutionAnalyzer
{
    static class Program
    {
        public static MainForm theMainForm; //主窗口对象，其副本mainForm在GlobalOperation类中保存
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]   
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            theMainForm = new MainForm();
            Application.Run(theMainForm);
        }
    }
}
