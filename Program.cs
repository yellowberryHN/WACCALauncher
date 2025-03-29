using Microsoft.Win32;
using System;
using System.IO;
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

        public static bool IsCorrectRes(Screen selected = null)
        {
            var screen = selected ?? CurrentScreen;

            // 1080p vertical resolution
            var correctPixelRes = screen.Bounds.Width == 1080
                && screen.Bounds.Height == 1920;

            // 9:16 aspect ratio check
            var correctAspect = (screen.Bounds.Width / screen.Bounds.Height) == 0.5625;

            return correctPixelRes || correctAspect;
        }

        public static bool IsRecommendedWinVer()
        {
            // Enterprise 2016 LTSB is recommended, for various reasons
            return BuildNumber == 14393 && ReleaseId == 1607;
        }

        public static bool IsRecommendedEnv()
        {
            return IsCorrectRes() && IsRecommendedWinVer();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // if we have multiple screens, try to start on one with proper resolution, if any
            if (Screen.AllScreens.Length > 1)
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (IsCorrectRes(screen))
                    {
                        CurrentScreen = screen;
                    }
                }
            }

            // when running as system shell, we must set our working directory manually
            var whereAmI = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (Directory.GetCurrentDirectory() != whereAmI)
                Directory.SetCurrentDirectory(whereAmI);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
