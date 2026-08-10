using Gtk;
using Cairo;
using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Core.Util;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.GTK4;

[GObject.Subclass<DrawingArea>]
internal partial class SequencedAudio_Piano
{
    private SongState.Track[]? Tracks;
    private bool[]? EnabledTracks;
    private HSLColor[] Colors;
    private double R;
    private double G;
    private double B;

    partial void Initialize()
    {
        Colors = new HSLColor[SongState.MAX_TRACKS];
        HeightRequest = 60;
        WidthRequest = 600;
        SetHexpand(true);
        SetVexpand(true);
        ConfigureTimer();
        SetDrawFunc(DrawPiano);
    }
    private void ConfigureTimer()
    {
        var timer = GLib.Timer.New(); // Creates a new timer variable
        var context = GLib.MainContext.GetThreadDefault(); // Reads the main context default thread
        var source = GLib.Functions.TimeoutSourceNew(1); // Creates and configures the timeout interval at 1 microsecond, so it updates in real time
        source.SetCallback(PianoTimerCallback); // Sets the callback for the timer interval to be used on
        var microsec = new CULong(source.Attach(context)); // Configures the microseconds based on attaching the GLib MainContext thread
        // timer.Elapsed(ref microsec); // Adds the pointer to the configured microseconds source
        GLib.Internal.Timer.Elapsed(timer.Handle, ref microsec); // GLib.Timer.Elapsed was removed in GirCore 0.6.3, so we're using this workaround instead
        timer.Start(); // Starts the timer
    }

    private void DrawPiano(DrawingArea da, Context cr, int width, int height)
    {
        cr.Save(); // Save context before drawing

        cr.SetSourceRgba(0, 0, 0, 0.7); // Set piano background color to black with slight transparency
        cr.Rectangle(0, 0, width, height); // Draw the rectangle for the background
        cr.Fill(); // Fill the rectangle background with the applied colors

        DrawPianoKeys(cr); // Now we draw the piano keys

        cr.Restore();
    }

    private void DrawPianoKeys(Context cr)
    {
        if (EnabledTracks is not null && Tracks is not null)
        {
            InitPianoTracks();
        }

        // 75 keys total. 40 white keys consisting of 5 white key sets with 7 keys each
        // and 1 white key set of 5, plus 35 darker white keys with 5 sets with 7 keys each.
        bool isDarker = false; // Must be set to false, so that the lighter key group is drawn first
        int k = 0; // And this key index starting at 0, important for highlighting keys when used
        var kGrp = 0; // To count the key groups for adding the text to the white key groups
        for (int i = 0; i < 75; kGrp++) // All 75 keys accounted for
        {
            for (int l = 0; l < 7; l++, i++) // With each key group consisting of 7 keys (except for the last one, which is 5 keys)
            {
                if (i >= 75) break; // So that if it's more or equal to 75, we'll break out of the loop
                DrawPianoKeyWhite(cr, k, kGrp, i * 8, isDarker); // This will draw a white piano key
                if ((k % 12) != 4 && (k % 12) != 11) k += 2; // So that way, every key that's not a 4th or 11th key within 12 keys, the key index (k) increments by 2
                else k += 1; // Otherwise if it's a 4th or 11th key in a remainder of 12, it'll increment by 1
            }
            if (!isDarker) isDarker = true; // Then we change it to true, once the lighter key group is drawn
            else isDarker = false; // Otherwise if the darker key group is drawn, we set this to false
        }

        // 53 keys total, 21 gaps (adding up to 74). 11 sets of black keys in twos, 10 sets of black keys in threes, and 1 single black key at the end
        kGrp = 2; // The first key group only has two keys, so the index is set to 2
        k = 1; // For the black keys, it needs to start at 1, since the first black key is piano key number 2
        for (int i = 0; i < 74; i++) // We include the gaps in there, so it's 74 total
        {
            for (int ki = 0; ki < kGrp; i++, ki++) // So that when each key has been made in the group, it will exit the loop and then repeat
            {
                DrawPianoKeyBlack(cr, k, i * 8); // This will draw a black piano key
                if ((k % 12) != 3 && (k % 12) != 10) k += 2; // If it's not a 3rd or 10th key within a set of 12 keys, the key index (k) will increment by 2
                else k += 3; // Otherwise if it's a 3rd or 10th key in a remainder of 12, the key index increments by 3
            }
            if (kGrp is 2 && i is not 72) kGrp++; // That way, if the key group is two and the index is not 72, kGrp will increment to 3
            else if (i is 72) kGrp = 1; // If the index is 72, the key group will be reduced to 1
            else kGrp--; // Otherwise if it's neither of the above, the key group will be decremented to 2
        }
    }

    private void DrawText(Context cr, int keyIndex, int keyGroup, double pos, float areaWidth, float areaHeight)
    {
        float smallAdj = -0.5f; // Small workaround to ensure the text remains in center of key
        if (keyGroup > 0) smallAdj = 0.5f; // If key group is more than 0, make it 0.5f
        cr.SetSourceRgb(0, 0, 0); // Set the font color to black
        cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal); // We're using Sans as the font, with no slant or weight
        cr.SetFontSize(areaWidth * 4); // Setting it to be large enough to fit in the keys
        cr.MoveTo((pos * areaWidth) + areaWidth + smallAdj, areaHeight - 5); // Move the font to the bottom of the keys
        cr.ShowText(ConfigUtils.GetKeyName(keyIndex)); // Set the text so it shows as the actual piano key note
    }
    private void DrawPianoKeyWhite(Context cr, int keyIndex, int keyGroup, double pos, bool isDarker)
    {
        var width = GetWidth() / 599f; // Piano key width
        var height = (float)GetHeight();
        if (isDarker) R = G = B = 1f / 2f; // If it's the darker key group, make sure all RGB channels are half each
        else R = G = B = 1; // Otherwise, set them to 1.0 (maximum value)
        cr.Save(); // Save the context before we start drawing
        CheckPianoTrack(keyIndex); // Check to see if the piano key is being pressed, and set it to that highlighted color
        cr.SetSourceRgb(R, G, B); // Then apply the color values
        cr.Rectangle(pos * width, 0, 7 * width, height); // Create the key as a rectangle, positioned right after each one
        cr.Fill(); // Fill in the rectangle with the applied color values
        if (keyIndex % 12 == 0) DrawText(cr, keyIndex, keyGroup, pos, width, height);
        cr.Restore(); // This will restore the context, to prepare for the next drawing
    }

    private void DrawPianoKeyBlack(Context cr, int keyIndex, double pos)
    {
        R = G = B = 0; // All black keys are set to 0.0 (minimum value)
        cr.Save(); // Save the context before we start drawing
        CheckPianoTrack(keyIndex); // Check to see if the piano key is being pressed, and set it to that highlighted color
        cr.SetSourceRgb(R, G, B); // Then apply the color values
        cr.Rectangle((pos + 5.1) * (GetWidth() / 599f), 0, 5 * (GetWidth() / 599f), GetHeight() / 1.5); // Create the key as a smaller rectangle
        cr.Fill(); // Fill in the rectangle with the applied color values
        cr.Restore(); // This will restore the context, to prepare for the next drawing
    }

    private void InitPianoTracks()
    {
        for (int i = SongState.MAX_TRACKS - 1; i >= 0; i--)
        {
            if (!EnabledTracks![i])
            {
                continue;
            }

            SongState.Track track = Tracks![i];
            for (int nk = 0; nk < SongState.MAX_KEYS; nk++)
            {
                byte k = track.Keys[nk];
                if (k == byte.MaxValue)
                {
                    break;
                }

                Colors[i] = new HSLColor(GlobalConfig.Instance.Colors[track.Voice]);
            }
        }
    }
    private void CheckPianoTrack(int keyIndex)
    {
        for (int ti = 0; ti < Colors.Length; ti++)
        {
            if (Colors[ti].R is not 0f &&
                Colors[ti].G is not 0f &&
                Colors[ti].B is not 0f)
            {
                for (int i = 0; i < Tracks![ti].Keys.Length; i++)
                {
                    if (EnabledTracks![ti])
                    {
                        if (Tracks[ti].Keys[i] != byte.MaxValue)
                        {
                            if (Tracks[ti].Keys[i] == keyIndex)
                            {
                                R = Colors[ti].R;
                                G = Colors[ti].G;
                                B = Colors[ti].B;
                            }
                        }
                    }
                }
            }
        }
    }
    protected bool PianoTimerCallback()
    {
        // Redraws the piano on every interval
        QueueDraw();  // This function redraws the piano graphics
        return true;  // Returns the boolean as true, so the callback can start again on next interval
    }

    internal void UpdateKeys(SongState.Track[] tracks, bool[] enabledTracks)
    {
        Tracks = tracks;
        EnabledTracks = enabledTracks;
    }
}