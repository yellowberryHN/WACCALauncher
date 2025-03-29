using System;
using System.Drawing;
using System.Drawing.Text;

namespace WACCALauncher
{
    internal static class FontLoader
    {
        public static Font loadedFont;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private static readonly PrivateFontCollection fontCollection = new PrivateFontCollection();

        public static Font LoadFont()
        {
            if (loadedFont != null) return loadedFont;
            var fontData = Properties.Resources.menufont;
            var fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fontCollection.AddMemoryFont(fontPtr, Properties.Resources.menufont.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.menufont.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);

            loadedFont = new Font(fontCollection.Families[0], 22.5F);
            return loadedFont;
        }
    }
}
