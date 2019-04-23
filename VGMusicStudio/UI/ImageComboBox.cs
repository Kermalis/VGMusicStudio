using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    class ImageComboBox : ComboBox
    {
        const int imgSize = 15;
        bool open = false;

        public ImageComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDown;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();

            if (e.Index >= 0)
            {
                ImageComboBoxItem item = Items[e.Index] as ImageComboBoxItem ?? throw new InvalidCastException($"Item was not of type \"{nameof(ImageComboBoxItem)}\"");
                int indent = open ? item.IndentLevel : 0;
                e.Graphics.DrawImage(item.Image, e.Bounds.Left + (indent * imgSize), e.Bounds.Top, imgSize, imgSize);
                e.Graphics.DrawString(item.ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + (indent * imgSize) + imgSize, e.Bounds.Top);
            }

            base.OnDrawItem(e);
        }
        protected override void OnDropDown(EventArgs e)
        {
            open = true;
            base.OnDropDown(e);
        }
        protected override void OnDropDownClosed(EventArgs e)
        {
            open = false;
            base.OnDropDownClosed(e);
        }
    }
    class ImageComboBoxItem
    {
        public object Item { get; }
        public Image Image { get; }
        public int IndentLevel { get; }

        public ImageComboBoxItem(object item, Image image, int indentLevel)
        {
            Item = item;
            Image = image;
            IndentLevel = indentLevel;
        }

        public override string ToString() => Item.ToString();
    }
}
