using System;
using System.Text;
using System.IO.Ports;
using System.Security.Policy;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Globalization;

namespace WACCA
{
    //
    // Summary:
    //     Represents an ordered pair of integer x- and y-coordinates that defines a point
    //     in a two-dimensional plane.
    [Serializable]
    [TypeConverter(typeof(PointConverter))]
    [ComVisible(true)]
    public struct VFDPoint
    {
        public static readonly VFDPoint Empty;

        private short x;

        private byte y;


        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                if (x == 0)
                {
                    return y == 0;
                }

                return false;
            }
        }

        public short X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public byte Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public VFDPoint(short x, byte y)
        {
            this.x = x;
            this.y = y;
        }

        public VFDPoint(Size sz)
        {
            x = (short)sz.Width;
            y = (byte)sz.Height;
        }

        public static explicit operator Size(VFDPoint p)
        {
            return new Size(p.X, p.Y);
        }

        public static VFDPoint operator +(VFDPoint pt, Size sz)
        {
            return Add(pt, sz);
        }

        public static VFDPoint operator -(VFDPoint pt, Size sz)
        {
            return Subtract(pt, sz);
        }

        public static bool operator ==(VFDPoint left, VFDPoint right)
        {
            if (left.X == right.X)
            {
                return left.Y == right.Y;
            }

            return false;
        }

        public static bool operator !=(VFDPoint left, VFDPoint right)
        {
            return !(left == right);
        }

        public static VFDPoint Add(VFDPoint pt, Size sz)
        {
            return new VFDPoint((short)(pt.X + sz.Width), (byte)(pt.Y + sz.Height));
        }

        public static VFDPoint Subtract(VFDPoint pt, Size sz)
        {
            return new VFDPoint((short)(pt.X - sz.Width), (byte)(pt.Y - sz.Height));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFDPoint))
            {
                return false;
            }

            VFDPoint point = (VFDPoint)obj;
            if (point.X == X)
            {
                return point.Y == Y;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return x ^ y;
        }

        public void Offset(short dx, byte dy)
        {
            X += dx;
            Y += dy;
        }

        public void Offset(VFDPoint p)
        {
            Offset(p.X, p.Y);
        }

        public override string ToString()
        {
            return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) + "}";
        }
    }

    class VFD
    {
        SerialPort port;
        public Lang language { get; private set; } = Lang.SIMP_CHINESE;
        public Font font { get; private set; } = Font._16_16;
        public Bright brightness { get; private set; } = Bright._100;
        public bool power { get; private set; } = false;

        /// <summary>
        /// Establish a connection to a VFD, and prepare it for use.
        /// </summary>
        /// <param name="portName">The port that the VFD connected to</param>
        public VFD(string portName = "COM2")
        {
            port = new SerialPort(portName, 115200);
            port.Open();
            Reset();
        }

        private void VFD_Write(byte number)
        {
            Console.WriteLine(BitConverter.ToString(new byte[] { number }));
            port.Write(new byte[] { number }, 0, 1);
        }

        private void VFD_Write(byte[] bytes)
        {
            Console.WriteLine(BitConverter.ToString(bytes));
            port.Write(bytes, 0, bytes.Length);
        }

        private void VFD_Write(string text)
        {
            // Get correct encoding for current language
            int codeNumber;
            switch(language)
            {
                case Lang.SIMP_CHINESE:
                    codeNumber = 936; // GB2312
                    break;
                case Lang.TRAD_CHINESE:
                    codeNumber = 950; // Big5
                    break;
                case Lang.JAPANESE:
                    codeNumber = 932; // Shift-JIS
                    break;
                case Lang.KOREAN:
                    codeNumber = 949; // KSC5601
                    break;
                default:
                    codeNumber = 932;
                    break;
            }

            // Convert Unicode string to encoded bytes
            Encoding unicodeEncoding = Encoding.Unicode;
            Encoding correctEncoding = Encoding.GetEncoding(codeNumber);
            byte[] unicodeBytes = unicodeEncoding.GetBytes(text);
            byte[] encodedBytes = Encoding.Convert(unicodeEncoding, correctEncoding, unicodeBytes);

            Console.WriteLine(BitConverter.ToString(correctEncoding.GetBytes(text)));
            port.Write(encodedBytes, 0, encodedBytes.Length);
        }

        /*
        #define LEFT_HI(x) (((x) & 0x100) >> 8)
        #define LEFT_LO(x) ((x) & 0xFF)

        #define FTB_PORT_WRITE_LEFT(x) {FTB_PORT.write(LEFT_HI(x)); FTB_PORT.write(LEFT_LO(x));}
        */

        private void VFD_WriteShort(short x)
        {
            byte hi = (byte)(((x) & 0x100) >> 8);
            byte lo = (byte)((x) & 0xFF);
            VFD_Write(new byte[] {hi, lo});
        }

        public void Write(string text)
        {
            VFD_Write(text);
        }

        public void Reset()
        {
            VFD_Write(new byte[] { 0x1B, 0x0B });
        }

        public void Clear()
        {
            VFD_Write(new byte[] { 0x1B, 0x0C });
        }

        public void TestPayload() // fucked
        {
            VFD_Write(Encoding.ASCII.GetString(new byte[] { 0x1b, 0x0c, 0x1b, 0x52, 0x1b, 0x40, 0x00, 0x00, 0x00, 0x00, 0x9f, 0x02, 0x1b, 0x41, 0x00, 0x1b, 0x50, 0x50, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x7c, 0x1b, 0x51}));
        }

        public enum Bright {
            _0 = 0,
            _25 = 1,
            _50 = 2,
            _75 = 3,
            _100 = 4
        }

        public void Brightness(Bright brightness)
        {
            VFD_Write(new byte[] { 0x1B, 0x20, (byte)brightness });
        }

        public void PowerOn()
        {
            Power(true);
        }

        public void PowerOff()
        {
            Power(false);
        }

        private void Power(bool on)
        {
            VFD_Write(new byte[] { 0x1B, 0x21, (byte)(on ? 0x01 : 0x00) });
        }

        public void CanvasShift(short left)
        {
            VFD_Write(new byte[] { 0x1B, 0x22 });
            VFD_WriteShort(left);
        }

        public void Cursor(short left, byte top)
        {
            VFD_Write(new byte[] { 0x1B, 0x30 });
            VFD_WriteShort(left);
            VFD_Write(top);
        }

        public enum Lang {
            SIMP_CHINESE,
            TRAD_CHINESE,
            JAPANESE,
            KOREAN
        }

        public void Language(Lang lang)
        {
            language = lang;
            VFD_Write(new byte[] { 0x1B, 0x32, (byte)language });
        }

        public enum Font
        {
            _16_16,
            _6_8
        }

        public void FontSize(Font size)
        {
            // 3
            font = size;
            VFD_Write(new byte[] { 0x1B, 0x33, (byte)size });
        }

        public void CreateScrollBox(short left, byte top, short width, byte height)
        {
            VFD_Write(new byte[] { 0x1B, 0x40 });
            VFD_WriteShort(left);
            VFD_Write(top);
            VFD_WriteShort(width);
            VFD_Write(height);
        }

        public void ScrollSpeed(byte divisor)
        {
            VFD_Write(new byte[] { 0x1B, 0x41, (byte)divisor });
        }

        public void ScrollText(string text)
        {
            if (text.Length >= 0x100) throw new ArgumentOutOfRangeException("Text is too long.");
            VFD_Write(new byte[] { 0x1B, 0x50, (byte)text.Length });
            VFD_Write(text);
        }

        public void ScrollStart()
        {
            VFD_Write(new byte[] { 0x1B, 0x51 });
        }

        public void ScrollStop()
        {
            VFD_Write(new byte[] { 0x1B, 0x52 });
        }

        public enum BlinkMode
        {
            Off = 0,
            Invert = 1,
            All = 2
        }

        public void BlinkSet(BlinkMode blink, byte interval)
        {
            VFD_Write(new byte[] { 0x1B, 0x23, (byte)blink, interval });
        }

        public void ClearLine(byte line)
        {
            Cursor(0, line);
            VFD_Write("".PadLeft(20));
            Cursor(0, line);
        }

        public void DrawBitmap(Bitmap bmp, Point origin)
        {
            if (bmp.PixelFormat != PixelFormat.Format1bppIndexed)
                throw new ArgumentException("Provided bitmap is not monochrome");

            // We have to do it this way because of a GDI+ bug
            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            RotateNoneFlipYMono(bmp);

            Rectangle bounds = new Rectangle(new Point(), bmp.Size);

            var data = bmp.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);

            VFD_Write("\x1B\x2E");
            VFD_WriteShort((short)origin.X);
            VFD_Write((byte)origin.Y);
            VFD_WriteShort((short)bmp.Height); // Inverted because image was flipped
            VFD_Write((byte)((bmp.Width / 8)-1));

            int bytes = ( bmp.Width * bmp.Height ) / 8;

            // Create a byte array to hold the pixel data
            byte[] pixelData = new byte[bytes];

            // Copy the data from the pointer to the byte array
            Marshal.Copy(data.Scan0, pixelData, 0, bytes);

            VFD_Write(pixelData);

            bmp.UnlockBits(data);
        }

        private static void RotateNoneFlipYMono(Bitmap bmp)
        {
            if (bmp == null || bmp.PixelFormat != PixelFormat.Format1bppIndexed)
                throw new ArgumentException("Provided bitmap is not monochrome");

            var height = bmp.Height;
            var width = bmp.Width;
            // width in dwords
            var stride = (width + 31) >> 5;
            // total image size
            var size = stride * height;
            // alloc storage for pixels
            var bytes = new int[size];

            // get image pixels
            var rect = new Rectangle(Point.Empty, bmp.Size);
            var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            Marshal.Copy(bd.Scan0, bytes, 0, size);

            // flip by swapping dwords
            int halfSize = size >> 1;
            for (int y1 = 0, y2 = size - stride; y1 < halfSize; y1 += stride, y2 -= stride)
            {
                int end = y1 + stride;
                for (int x1 = y1, x2 = y2; x1 < end; x1++, x2++)
                {
                    bytes[x1] ^= bytes[x2];
                    bytes[x2] ^= bytes[x1];
                    bytes[x1] ^= bytes[x2];
                }
            }

            // copy pixels back
            Marshal.Copy(bytes, 0, bd.Scan0, size);
            bmp.UnlockBits(bd);
        }
    }
}
