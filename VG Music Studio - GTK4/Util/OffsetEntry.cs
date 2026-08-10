
using System;

namespace Kermalis.VGMusicStudio.GTK4.Util;

[GObject.Subclass<Gtk.Box>]
internal partial class OffsetEntry
{
    private Gtk.Label _hexLabel;
    // internal Gtk.Entry Entry;
    internal HexSpinButton Entry;

    [GObject.Subclass<Gtk.SpinButton>]
    internal partial class HexSpinButton
    {
        partial void Initialize()
        {
            Text_ = $"{Text_:X7}";
        }
    }
    internal static OffsetEntry Initialize(int offset = 0)
    {
        OffsetEntry args = NewWithProperties([new GObject.ConstructArgument("orientation", new GObject.Value(0)), new GObject.ConstructArgument("halign", new GObject.Value(Gtk.Align.Center)), new GObject.ConstructArgument("spacing", new GObject.Value(3))]);
        args._hexLabel = Gtk.Label.New("0x");
        args.Entry = HexSpinButton.NewWithProperties([]);
        args.Entry.Text_ = $"{offset:X7}";
        args.Entry.OnValueChanged += args.OffsetChanged;
        args.Entry.OnChangeValue += args.OffsetChanged;
        args.Append(args._hexLabel);
        args.Append(args.Entry);
        return args;
    }

    private void OffsetChanged(Gtk.Editable sender, EventArgs args)
    {
        Entry.OnChanged -= OffsetChanged;
        var text = sender.GetText();
        for (int i = 0; i < text.Length; i++)
        {
            bool isValidHexValue = false;
            if (char.IsDigit(text[i]))
            {
                isValidHexValue = true;
            }
            else
            {
                switch (text[i])
                {
                    case 'A':
                    case 'a':
                        isValidHexValue = true;
                        break;
                    case 'B':
                    case 'b':
                        isValidHexValue = true;
                        break;
                    case 'C':
                    case 'c':
                        isValidHexValue = true;
                        break;
                    case 'D':
                    case 'd':
                        isValidHexValue = true;
                        break;
                    case 'E':
                    case 'e':
                        isValidHexValue = true;
                        break;
                    case 'F':
                    case 'f':
                        isValidHexValue = true;
                        break;
                }
            }

            if (!isValidHexValue)
            {
                text.Remove(i, 1);
            }

            Entry.Text_ = $"{text:X7}";
        }
        Entry.OnChanged += OffsetChanged;
    }

    internal void SetValue(long offset)
    {
        Entry.Text_ = $"{offset:X7}";
    }
}