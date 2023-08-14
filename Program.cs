using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace WACCALauncher
{
    internal static class Program
    {
        private static readonly RegistryKey WinVer = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        private static readonly int BuildNumber = int.Parse(WinVer.GetValue("CurrentBuild").ToString());
        private static readonly int ReleaseId = int.Parse(WinVer.GetValue("ReleaseId").ToString());

        public static Screen CurrentScreen = Screen.PrimaryScreen;

        public static bool IsCorrectRes()
        {
            return CurrentScreen.Bounds.Width  == 1080 
                && CurrentScreen.Bounds.Height == 1920;
        }

        public static bool IsCorrectVer()
        {
            // ensures Enterprise 2016 LTSB is used
            return BuildNumber == 14393 && ReleaseId == 1607;
        }

        public static bool IsWACCA()
        {
            return IsCorrectRes() && IsCorrectVer();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // for testing
            if(Screen.AllScreens.Length > 1)
                CurrentScreen = Screen.AllScreens[1];

            // when running as system shell, we must set our working directory manually
            if(Directory.GetCurrentDirectory() == Environment.GetFolderPath(Environment.SpecialFolder.System))
                Directory.SetCurrentDirectory(Path.GetPathRoot(Assembly.GetExecutingAssembly().Location));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
