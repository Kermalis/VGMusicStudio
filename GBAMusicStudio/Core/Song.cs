using GBAMusicStudio.Core.M4A;
using System;
using System.Collections.Generic;
using static GBAMusicStudio.Core.M4A.M4AStructs;

namespace GBAMusicStudio.Core
{
    internal abstract class Song
    {
        internal List<SongEvent>[] Commands;

        internal abstract byte NumTracks { get; }
        internal abstract void Load();
    }

    internal class ROMSong : Song
    {
        SongHeader header;
        readonly ushort num, table;

        internal ROMSong(ushort songNum, ushort tableNum)
        {
            num = songNum;
            table = tableNum;
        }
        internal override byte NumTracks => header.NumTracks;
        internal override void Load()
        {
            header = ROM.Instance.ReadStruct<SongHeader>(ROM.Instance.ReadPointer(ROM.Instance.Game.SongTables[table] + ((uint)8 * num)));
            Array.Resize(ref header.Tracks, NumTracks); // Not really necessary
            Commands = new List<SongEvent>[NumTracks];
            for (int i = 0; i < NumTracks; i++)
                Commands[i] = new List<SongEvent>();

            SongPlayer.VoiceTable = new VoiceTable();
            SongPlayer.VoiceTable.Load(header.VoiceTable);

            if (NumTracks == 0 || NumTracks > 16) return;

            var reader = new ROMReader();
            reader.InitReader();

            for (int i = 0; i < NumTracks; i++)
            {
                reader.SetOffset(header.Tracks[i]);

                byte cmd = 0, runCmd = 0, prevNote = 0, prevVelocity = 127;

                while (cmd != 0xB1 && cmd != 0xB2)
                {
                    uint offset = reader.Position;
                    Command command = 0;
                    var args = new int[0];

                    cmd = reader.ReadByte();
                    if (cmd >= 0xBD) // Commands that work within running status
                        runCmd = cmd;

                    #region TIE & Notes

                    if (runCmd >= 0xCF && cmd < 0x80) // Within running status
                    {
                        var o = reader.Position;
                        byte peek1 = reader.ReadByte(),
                            peek2 = reader.ReadByte();
                        reader.SetOffset(o);
                        if (peek1 >= 128) AddNoteEvent(cmd, prevVelocity, 0, runCmd, out prevNote, out prevVelocity, out command, out args);
                        else if (peek2 >= 128) AddNoteEvent(cmd, reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity, out command, out args);
                        else AddNoteEvent(cmd, reader.ReadByte(), reader.ReadByte(), runCmd, out prevNote, out prevVelocity, out command, out args);
                    }
                    else if (cmd >= 0xCF)
                    {
                        var o = reader.Position;
                        byte peek1 = reader.ReadByte(),
                            peek2 = reader.ReadByte(),
                            peek3 = reader.ReadByte();
                        reader.SetOffset(o);
                        if (peek1 >= 128) AddNoteEvent(prevNote, prevVelocity, 0, runCmd, out prevNote, out prevVelocity, out command, out args);
                        else if (peek2 >= 128) AddNoteEvent(reader.ReadByte(), prevVelocity, 0, runCmd, out prevNote, out prevVelocity, out command, out args);
                        else if (peek3 >= 128) AddNoteEvent(reader.ReadByte(), reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity, out command, out args);
                        else AddNoteEvent(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), runCmd, out prevNote, out prevVelocity, out command, out args);
                    }

                    #endregion

                    #region Rests

                    else if (cmd >= 0x80 && cmd <= 0xB0)
                    {
                        command = Command.Rest;
                        args = new int[] { RestFromCMD(0x80, cmd) };
                    }

                    #endregion

                    #region Commands

                    else if (runCmd < 0xCF && cmd < 0x80) // Commands within running status
                    {
                        switch (runCmd)
                        {
                            case 0xBD: command = Command.Voice; args = new int[] { cmd }; break; // VOICE
                            case 0xBE: command = Command.Volume; args = new int[] { cmd }; break; // VOL
                            case 0xBF: command = Command.Panpot; args = new int[] { cmd - 0x40 }; break; // PAN
                            case 0xC0: command = Command.Bend; args = new int[] { cmd - 0x40 }; break; // BEND
                            case 0xC1: command = Command.BendRange; args = new int[] { cmd }; break; // BENDR
                            case 0xC2: command = Command.LFOSpeed; args = new int[] { cmd }; break; // LFOS
                            case 0xC3: command = Command.LFODelay; args = new int[] { cmd }; break; // LFODL
                            case 0xC4: command = Command.MODDepth; args = new int[] { cmd }; break; // MOD
                            case 0xC5: command = Command.MODType; args = new int[] { cmd }; break; // MODT
                            case 0xC8: command = Command.Tune; args = new int[] { cmd - 0x40 }; break; // TUNE
                            case 0xCD: command = Command.XCMD; args = new int[] { cmd, reader.ReadByte() }; break; // XCMD
                            case 0xCE: command = Command.NoteOff; args = new int[] { cmd }; prevNote = cmd; break; // EOT
                        }
                    }
                    else if (cmd > 0xB0 && cmd < 0xCF)
                    {
                        switch (cmd)
                        {
                            case 0xB1: command = Command.Finish; break; // FINE
                            case 0xB2: command = Command.GoTo; args = new int[] { (int)reader.ReadPointer() }; break; // GOTO
                            case 0xB3: command = Command.PATT; args = new int[] { (int)reader.ReadPointer() }; break; // PATT
                            case 0xB4: command = Command.PEND; break; // PEND
                            case 0xB5: command = Command.REPT; args = new int[] { reader.ReadByte() }; break; // REPT
                            case 0xB9: command = Command.MEMACC; args = new int[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() }; break; // MEMACC
                            case 0xBA: command = Command.Priority; args = new int[] { reader.ReadByte() }; break; // PRIO
                            case 0xBB: command = Command.Tempo; args = new int[] { reader.ReadByte() * 2 }; break; // TEMPO
                            case 0xBC: command = Command.KeyShift; args = new int[] { reader.ReadSByte() }; break; // KEYSH
                                                                                                                   // Commands that work within running status:
                            case 0xBD: command = Command.Voice; args = new int[] { reader.ReadByte() }; break; // VOICE
                            case 0xBE: command = Command.Volume; args = new int[] { reader.ReadByte() }; break; // VOL
                            case 0xBF: command = Command.Panpot; args = new int[] { reader.ReadByte() - 0x40 }; break; // PAN
                            case 0xC0: command = Command.Bend; args = new int[] { reader.ReadByte() - 0x40 }; break; // BEND
                            case 0xC1: command = Command.BendRange; args = new int[] { reader.ReadByte() }; break; // BENDR
                            case 0xC2: command = Command.LFOSpeed; args = new int[] { reader.ReadByte() }; break; // LFOS
                            case 0xC3: command = Command.LFODelay; args = new int[] { reader.ReadByte() }; break; // LFODL
                            case 0xC4: command = Command.MODDepth; args = new int[] { reader.ReadByte() }; break; // MOD
                            case 0xC5: command = Command.MODType; args = new int[] { reader.ReadByte() }; break; // MODT
                            case 0xC8: command = Command.Tune; args = new int[] { reader.ReadByte() - 0x40 }; break; // TUNE
                            case 0xCD: command = Command.XCMD; args = new int[] { reader.ReadByte(), reader.ReadByte() }; break; // XCMD
                            case 0xCE: // EOT
                                int note;

                                if (reader.PeekByte() < 128) { note = reader.ReadByte(); prevNote = (byte)note; }
                                else { note = -1; }

                                command = Command.NoteOff; args = new int[] { note };
                                break;
                            default: Console.WriteLine("Invalid command: 0x{0:X} = {1}", reader.Position, cmd); break;
                        }
                    }

                    #endregion

                    Commands[i].Add(new SongEvent(offset, command, args));
                }
            }
            ;

            byte RestFromCMD(byte startCMD, byte cmd)
            {
                byte[] added = { 4, 4, 2, 2 };
                byte wait = (byte)(cmd - startCMD);
                byte add = wait > 24 ? (byte)24 : wait;
                for (int i = 24 + 1; i <= wait; i++)
                    add += added[i % 4];
                return add;
            }
            void AddNoteEvent(byte note, byte velocity, byte addedDuration, byte runCmd, out byte prevNote, out byte prevVelocity, out Command cmd, out int[] args)
            {
                prevNote = note;
                prevVelocity = velocity;
                int duration = runCmd == 0xCF ? 0xFF : (RestFromCMD(0xD0, runCmd) + addedDuration);

                cmd = Command.NoteOn;
                args = new int[] { note, velocity, duration };
            }
        }
    }
}
