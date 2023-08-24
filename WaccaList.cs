using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WACCALauncher
{
    public partial class WaccaList : System.Windows.Forms.ListBox
    {
        private MenuManager menuManager;

        public WaccaList()
        {
            BackColor = Color.Black;
            BorderStyle = BorderStyle.None;
            DrawMode = DrawMode.OwnerDrawVariable;
            ItemHeight = 40;
            Size = new System.Drawing.Size(680, 600);
            Location = new System.Drawing.Point(200, 240);
        }

        public void AssignMenuManager(MenuManager manager)
        {
            menuManager = manager;
            Items.Clear();
            Items.AddRange(menuManager.GetCurrentMenu().ToArray());
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);

            if (Items.Count < 1) return;

            var text = Items[e.Index].ToString().ToUpper();
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            e.Graphics.DrawString(text, Font, selected ? Brushes.Red : Brushes.White, e.Bounds);
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            Items.Clear();
            Items.AddRange(menuManager.GetCurrentMenu().ToArray());
            SelectedIndex = 0;
            base.OnInvalidated(e);
        }
    }
}
