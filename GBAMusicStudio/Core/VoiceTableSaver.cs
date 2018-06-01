using GBAMusicStudio.Util;
using SoundFont;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using static GBAMusicStudio.Core.M4A.M4AStructs;

namespace GBAMusicStudio.Core
{
    class VoiceTableSaver
    {
        static readonly string[] instrumentNames = {
            "Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honky-tonk Piano", "Rhodes Piano", "Chorused Piano",
            "Harpsichord",  "Clavinet", "Celesta", "Glockenspiel", "Music Box", "Vibraphone", "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
            "Hammond Organ", "Percussive Organ", "Rock Organ", "Church Organ", "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
            "Acoustic Guitar (Nylon)", "Acoustic Guitar (Steel)", "Electric Guitar (Jazz)", "Electric Guitar (Clean)", "Electric Guitar (Muted)",
            "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics", "Acoustic Bass", "Electric Bass (Finger)", "Electric Bass (Pick)",
            "Fretless Bass", "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2", "Violin", "Viola", "Cello", "Contrabass",
            "Tremelo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani", "String Ensemble 1", "String Ensemble 2", "SynthStrings 1",
            "SynthStrings 2", "Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit", "Trumpet", "Trombone", "Tuba", "Muted Trumpet",
            "French Horn", "Brass Section", "Synth Brass 1", "Synth Brass 2", "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
            "Oboe", "English Horn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "Pan Flute", "Bottle Blow", "Shakuhachi", "Whistle",
            "Ocarina", "Lead 1 (Square)", "Lead 2 (Sawtooth)", "Lead 3 (Calliope Lead)", "Lead 4 (Chiff Lead)", "Lead 5 (Charang)",
            "Lead 6 (Voice)", "Lead 7 (Fifths)", "Lead 8 (Bass + Lead)", "Pad 1 (New Age)", "Pad 2 (Warm)", "Pad 3 (Polysynth)", "Pad 4 (Choir)",
            "Pad 5 (Bowed)", "Pad 6 (Metallic)", "Pad 7 (Halo)", "Pad 8 (Sweep)", "FX 1 (Rain)", "FX 2 (Soundtrack)", "FX 3 (Crystal)",
            "FX 4 (Atmosphere)", "FX 5 (Brightness)", "FX 6 (Goblins)", "FX 7 (Echoes)", "FX 8 (Sci-Fi)", "Sitar", "Banjo", "Shamisen", "Koto",
            "Kalimba", "Bagpipe", "Fiddle", "Shanai", "Tinkle Bell", "Agogo", "Steel Drums", "Woodblock", "Taiko Drum", "Melodic Tom",
            "Synth Drum", "Reverse Cymbal", "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet", "Telephone Ring", "Helicopter",
            "Applause", "Gunshot" };
        SF2 sf2;

        internal VoiceTableSaver()
        {
            sf2 = new SF2("", "", "", 0, 0, "", ROM.Instance.Game.Creator, "", "GBA Music Studio by Kermalis");

            foreach (var instrument in MusicPlayer.Sounds)
                if (instrument.Key < MusicPlayer.NOISE1_ID)
                    AddSample(instrument.Value, string.Format("Sample 0x{0:X}", instrument.Key));

            sf2.Save(string.Format("{0}.sf2", ROM.Instance.Game.Name).ToSafeFileName());
        }

        // Add a new sample and create corresponding header
        void AddSample(FMOD.Sound sound, string name)
        {
            // Get properties
            sound.getLength(out uint length, FMOD.TIMEUNIT.PCMBYTES);
            sound.getLoopPoints(out uint loop_start, FMOD.TIMEUNIT.PCMBYTES, out uint loop_end, FMOD.TIMEUNIT.PCMBYTES);
            sound.getLoopCount(out int loopCount);
            sound.getDefaults(out float frequency, out int priority);

            // Get sample data
            sound.@lock(0, length, out IntPtr snd, out IntPtr idc, out uint len, out uint idc2);
            var pcm8 = new byte[len];
            Marshal.Copy(snd, pcm8, 0, (int)len);
            sound.unlock(snd, idc, len, idc2);
            short[] pcm16 = pcm8.Select(i => (short)(i << 8)).ToArray();

            // Add to file
            sf2.AddSample(pcm16, name, loopCount == -1, loop_start, (uint)frequency, 60, 0);
        }
    }
}
