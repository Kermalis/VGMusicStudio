using GBAMusicStudio.Util;
using System;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    class ValueTextBox : ThemedTextBox
    {
        bool hex = false;
        public bool Hexadecimal
        {
            get => hex;
            set
            {
                hex = value;
                OnTextChanged(EventArgs.Empty);
                SelectionStart = Text.Length;
            }
        }
        long max = long.MaxValue;
        public long Maximum
        {
            get => max;
            set
            {
                max = value;
                OnTextChanged(EventArgs.Empty);
            }
        }
        long min = 0;
        public long Minimum
        {
            get => min;
            set
            {
                min = value;
                OnTextChanged(EventArgs.Empty);
            }
        }
        public long Value
        {
            get
            {
                if (TextLength > 0)
                {
                    if (Utils.TryParseValue(Text, out long l))
                    {
                        return l;
                    }
                }
                return min;
            }
            set
            {
                int i = SelectionStart;
                Text = Hexadecimal ? ("0x" + value.ToString("X")) : value.ToString();
                SelectionStart = i;
                OnValueChanged(EventArgs.Empty);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NOTIFY = 0x0282;
            if (m.Msg == WM_NOTIFY && m.WParam == new IntPtr(0xB))
            {
                if (Hexadecimal && SelectionStart < 2)
                {
                    SelectionStart = 2;
                }
            }
            base.WndProc(ref m);
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true; // Don't pay attention to this event unless:

            if ((char.IsControl(e.KeyChar) && !(Hexadecimal && SelectionStart <= 2 && SelectionLength == 0 && e.KeyChar == (char)Keys.Back)) || // Backspace isn't used on the "0x" prefix
                char.IsDigit(e.KeyChar) || // It is a digit
                (e.KeyChar >= 'a' && e.KeyChar <= 'f') || // It is a letter that shows in hex
                (e.KeyChar >= 'A' && e.KeyChar <= 'F'))
            {
                e.Handled = false;
            }
            base.OnKeyPress(e);
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Value = Value.Clamp(min, max);
        }

        EventHandler onValueChanged = null;
        public event EventHandler ValueChanged
        {
            add => onValueChanged += value;
            remove => onValueChanged -= value;
        }
        protected virtual void OnValueChanged(EventArgs e)
        {
            onValueChanged?.Invoke(this, e);
        }
    }
}
