using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.UI
{
    internal class ImageComboBox : ComboBox
    {
        private const int _imgSize = 15;
        private bool _open = false;

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
                int indent = _open ? item.IndentLevel : 0;
                e.Graphics.DrawImage(item.Image, e.Bounds.Left + (indent * _imgSize), e.Bounds.Top, _imgSize, _imgSize);
                e.Graphics.DrawString(item.ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds.Left + (indent * _imgSize) + _imgSize, e.Bounds.Top);
            }

            base.OnDrawItem(e);
        }
        protected override void OnDropDown(EventArgs e)
        {
            _open = true;
            base.OnDropDown(e);
        }
        protected override void OnDropDownClosed(EventArgs e)
        {
            _open = false;
            base.OnDropDownClosed(e);
        }
    }
    internal class ImageComboBoxItem
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

        public override string ToString()
        {
            return Item.ToString();
        }
    }
}
