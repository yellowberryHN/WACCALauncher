using System;
using System.Drawing;
using System.Windows.Forms;

namespace WACCALauncher
{
    public partial class WaccaList : ListBox
    {
        private MenuManager menuManager;

        private Brush bgBrush;
        private Brush textBrush;
        private Brush highlightBrush;

        private PointF[] upArrow { get; }
        private PointF[] downArrow { get; }

        public WaccaList()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            DrawMode = DrawMode.OwnerDrawFixed;

            BackColor = Color.Black;
            ForeColor = Color.White;
            BorderStyle = BorderStyle.None;
            ItemHeight = 40;
            Size = new Size(680, 600);
            Location = new Point(200, 240);

            upArrow = new PointF[] { 
                new PointF(Bounds.Width - 30, 30),
                new PointF(Bounds.Width - 20, 10),
                new PointF(Bounds.Width - 10, 30) 
            };

            downArrow = new PointF[] {
                new PointF(Bounds.Width - 30, Bounds.Height - 30),
                new PointF(Bounds.Width - 20, Bounds.Height - 10),
                new PointF(Bounds.Width - 10, Bounds.Height - 30)
            };

            bgBrush = new SolidBrush(BackColor);
            textBrush = new SolidBrush(ForeColor);
            highlightBrush = Brushes.Red;
        }

        // hide scrollbar, we draw our own
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style = cp.Style & ~0x200000;
                return cp;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int LB_GETTOPINDEX = 0x018E;

        private int GetTopIndex()
        {
            return SendMessage(Handle, LB_GETTOPINDEX, IntPtr.Zero, IntPtr.Zero);
        }

        private int GetBottomIndex()
        {
            int topIdx = GetTopIndex();

            int itemsVisible = ClientSize.Height / 40;
            int bottomIndex = topIdx + itemsVisible - 1;

            return Math.Min(bottomIndex, Items.Count - 1);
        }

        private bool AtTop
        {
            get => GetTopIndex() == 0;
        }

        private bool AtBottom
        {
            get => GetBottomIndex() == Items.Count - 1;
        }

        private bool Scrollable
        {
            get => !(AtTop && AtBottom);
        }

        internal void AssignMenuManager(MenuManager manager)
        {
            menuManager = manager;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (Items.Count < 1 || e.Index < 0) return;

            var item = Items[e.Index];
            var bounds = e.Bounds;

            e.Graphics.FillRectangle(bgBrush, bounds);

            var text = item.ToString().ToUpper();
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            if (Scrollable)
            {
                bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width - 40, bounds.Height);
            }

            e.Graphics.DrawString(text, Font, selected ? highlightBrush : textBrush, bounds, new StringFormat() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

            if (!AtTop && GetTopIndex() == e.Index)
            {
                e.Graphics.FillPolygon(textBrush, upArrow);
            }
            if (!AtBottom && GetBottomIndex() == e.Index)
            {
                e.Graphics.FillPolygon(textBrush, downArrow);
            }

            base.OnDrawItem(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Region iRegion = new Region(e.ClipRectangle);
            e.Graphics.FillRegion(bgBrush, iRegion);
            if (Items.Count > 0)
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    Rectangle irect = GetItemRectangle(i);
                    if (e.ClipRectangle.IntersectsWith(irect))
                    {
                        OnDrawItem(new DrawItemEventArgs(
                            e.Graphics,
                            Font,
                            irect, i,
                            SelectedIndex == i ? DrawItemState.Selected : DrawItemState.Default,
                            ForeColor,
                            BackColor
                        ));
                        iRegion.Complement(irect);
                    }
                }
            }

            base.OnPaint(e);
        }

        protected override void OnSelectedValueChanged(EventArgs e)
        {
            base.OnSelectedValueChanged(e);
        }
    }
}
