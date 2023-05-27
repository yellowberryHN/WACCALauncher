using System;
using System.Text;
using System.IO.Ports;

namespace WACCALauncher
{
    class WaccaVFD
    {
        SerialPort port;

        public WaccaVFD(string portName = "COM2")
        {
            this.port = new SerialPort(portName, 115200);
            port.Open();
            Reset();
        }

        private void VFD_Write(byte number)
        {
            VFD_Write($"{(char)number}");
        }

        private void VFD_Write(string text)
        {
            Console.WriteLine(BitConverter.ToString(Encoding.Default.GetBytes(text)));
            port.Write(text);
        }

        private void VFD_WriteShort(short x)
        {
            char hi = (char)((x & 0x100) >> 8);
            char lo = (char)(x & 0xFF);
            VFD_Write($"{hi}{lo}");
        }

        public void Write(string text)
        {
            VFD_Write(text);
        }

        public void Reset()
        {
            VFD_Write("\x1B\x0B");
        }

        public void Clear()
        {
            VFD_Write("\x1B\x0C");
        }

        public enum bright {
            BRIGHT_0 = 0,
            BRIGHT_25 = 1,
            BRIGHT_50 = 2,
            BRIGHT_75 = 3,
            BRIGHT_100 = 4
        }

        public void Brightness(bright brightness)
        {
            VFD_Write("\x1B\x20" + (char)brightness);
        }

        public void Power(bool on)
        {
            VFD_Write("\x1B\x21" + (on ? "\x01" : "\x00"));
        }

        public void CanvasShift(short left)
        {
            VFD_Write("\x1B\x22");
            VFD_WriteShort(left);
        }

        public void Cursor(short left, byte top)
        {
            VFD_Write("\x1B\x30");
            VFD_WriteShort(left);
            VFD_Write(top);
        }

        public enum lang {
            SIMP_CHINESE,
            TRAD_CHINESE,
            JAPANESE,
            KOREAN
        }

        public void Language(lang language)
        {
            VFD_Write("\x1B\x32" + (char)language);
        }

        public enum font_size
        {
            FONT_16_16,
            FONT_6_8
        }

        public void FontSize(font_size size)
        {
            VFD_Write("\x1B\x33" + (char)size);
        }

        public void CreateScrollBox(short left, byte top, short width, byte height)
        {
            VFD_Write("\x1B\x40");
            VFD_WriteShort(left);
            VFD_Write(top);
            VFD_WriteShort(width);
            VFD_Write(height);
        }

        public void ScrollSpeed(byte divisor)
        {
            VFD_Write("\x1B\x33" + (char)divisor);
        }

        public void ScrollText(string text)
        {
            if (text.Length > 255) throw new ArgumentOutOfRangeException("Text is too long.");
            VFD_Write("\x1B\x50");
            VFD_Write((byte)text.Length);
            VFD_Write(text);
        }

        public void ScrollStart()
        {
            VFD_Write("\x1B\x51");
        }
    }
}
