using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CurrentSensorV3
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CurrentSensorConsole());
            //Application.Run(new TestGUI());
            //Application.Run(new FormList());
            //Application.Run(new TestPaintWav());
        }
    }
}
