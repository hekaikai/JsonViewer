using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace JsonViewer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string strFileName = "";
            if (args.Length > 0)
                strFileName = args[0];
            Application.Run(new FormMain(strFileName));
        }
    }
}
