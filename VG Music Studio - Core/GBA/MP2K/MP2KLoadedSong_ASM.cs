using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class WholeNoteMark : ICommand
{
    public Color Color => Color.Azure;
    public string Label => "Time Signature";
    public string Arguments => $"N/A";
}

internal sealed class CallFlag : ICommand
{
    public Color Color => Color.Azure;
    public string Label => "Call Flag";
    public string Arguments => $"N/A";
}

internal sealed class JumpFlag : ICommand
{
    public Color Color => Color.Azure;
    public string Label => "Jump Flag";
    public string Arguments => $"N/A";
}

internal sealed class RepeatFlag : ICommand
{
    public Color Color => Color.Azure;
    public string Label => "Repeat Flag";
    public string Arguments => $"N/A";
}

public sealed class ASMSaveArgs
{
    public bool MergeRemainingDuration;
    public string? VoicegroupLabel;

    public ASMSaveArgs(bool mergeRemainingDuration, string voicegroupLabel = "")
    {
        MergeRemainingDuration = mergeRemainingDuration;
        VoicegroupLabel = voicegroupLabel;
    }
}

internal sealed partial class MP2KLoadedSong
{
    public override void OpenASM(Assembler assembler, string headerLabel)
    {
        HeaderOffset = assembler.BaseOffset;
    }
    public override void SaveAsASM(string fileName, ASMSaveArgs args)
    {
        using (var file = new StreamWriter(fileName))
        {
            string label = Assembler.FixLabel(Path.GetFileNameWithoutExtension(fileName));
            if (args.VoicegroupLabel == "")
            {
                args.VoicegroupLabel = label.Replace("mus_", "");
            }
            string prevParam1 = "";
            string prevParam2 = "";
            string prevParam3 = "";
            string prevParam4 = "";
            ICommand prevCommand = null!;
            ICommand nextCommand = null!;
            int prevRest = 0;
            SongHeader header = ((MP2KLoadedSong)MP2KEngine.MP2KInstance!.Player.LoadedSong!).Header;
            byte baseVolume = Events.SelectMany(e => e).Where(e => e.Command is VolumeCommand).Select(e => ((VolumeCommand)e.Command).Volume).Max();
            byte reverb = (byte)(header.Reverb << 1); // Reverb value begins as too high, so it needs to be left shifted to delete the 8th bit
            reverb = (byte)(reverb >> 1); // Right shifted to shift the bits back to their original location
            file.WriteLine("\t.include \"MPlayDef.s\"");
            file.WriteLine();
            file.WriteLine($"\t.equ\t{label}_grp, voicegroup_{args.VoicegroupLabel}");
            file.WriteLine($"\t.equ\t{label}_pri, {header.Priority}");
            file.WriteLine($"\t.equ\t{label}_rev, reverb_set+{reverb}");
            file.WriteLine($"\t.equ\t{label}_mvl, {baseVolume}");
            file.WriteLine($"\t.equ\t{label}_key, 0");
            file.WriteLine($"\t.equ\t{label}_tbs, 1");
            file.WriteLine($"\t.equ\t{label}_exg, 1");
            file.WriteLine($"\t.equ\t{label}_cmp, 1");
            file.WriteLine();
            file.WriteLine("\t.section .rodata");
            file.WriteLine($"\t.global\t{label}");
            file.WriteLine("\t.align\t2");

            for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
            {
                int num = trackIndex + 1;
                file.WriteLine();
                file.WriteLine($"@**************** Track {num} (Midi-Chn.{num}) ****************@");
                file.WriteLine();
                file.WriteLine($"{label}_{num}:");

                //IEnumerable<int> offsets = Events[i].Where(e => e.Command is CallCommand || e.Command is JumpCommand || e.Command is RepeatCommand)
                //    .Select(e => (int)((dynamic)e).Command.Offset).Distinct(); // Get all offsets we need labels for
                IEnumerable<SongEvent> evts = Events[trackIndex].Where(e => e.Command is CallCommand || e.Command is JumpCommand || e.Command is RepeatCommand);
                int jumps = 1;
                int repeats = 1;
                var labelsGOTO = new Dictionary<int, string>();
                var labelsPATT = new Dictionary<int, string>();
                var labelsREPT = new Dictionary<int, string>();
                foreach (SongEvent e in evts)
                {
                    switch (e.Command)
                    {
                        //case CallCommand c:
                        //    {
                        //        if (!labels.ContainsKey(c.Offset))
                        //        {
                        //            labels.Add(c.Offset, $"{label}_{num}_B{jumps++}");
                        //        }
                        //        break;
                        //    }
                        case JumpCommand j:
                            {
                                if (!labelsGOTO.ContainsKey((int)j.Pointer.Offset))
                                {
                                    labelsGOTO.Add((int)j.Pointer.Offset, $"{label}_{num}_B{jumps++}");
                                }
                                break;
                            }
                        case RepeatCommand r:
                            {
                                if (!labelsREPT.ContainsKey((int)r.Pointer.Offset))
                                {
                                    labelsREPT.Add((int)r.Pointer.Offset, $"{label}_{num}_L{repeats++}");
                                }
                                break;
                            }
                    }
                }
                //foreach (int o in offsets)
                //{
                //    labels.Add(o, $"{label}_{num}_{jumps++:D3}");
                //}
                int ticks = 0;
                bool wholeNoteMarkDisplayed = false; // So that we can have conditional statements to prevent time signature duplicates
                bool wholeNoteMarkPattDisplayed = false; // This is for conditional statements so that note commands will write note type and velocity
                bool wholeNoteMarkRestBegin = false; // When the song begins with a rest command
                bool wholeNoteMarkNoteBegin = false; // When the song begins with a note command
                int wholeNoteNum = 0; // The index number for a regular Time Signature or Pattern
                int wholeNoteGotoNum = 0; // The index number for a Goto Time Signature or Pattern
                List<SongEvent> trackEvents = Events[trackIndex];
                List<SongEvent> trackEventsEX = [];
                int ticksEX = 0;
                // bool callTimeSignatureEnabled = false;
                // bool callTimeSignatureDisplayed = false;
                // bool jumpTimeSignatureDisplayed = false;
                // bool returnTimeSignatureDisplayed = false;
                for (int i = trackEvents.Count - 1; i > -1; i--)
                {
                    SongEvent e = trackEvents[i];
                    int eOffset = (int)e.Offset;
                    switch (e.Command)
                    {
                        case RestCommand c:
                            {
                                byte amt = (byte)MP2KUtils.RestTable.BinarySearch(c.Rest);
                                int rem = c.Rest - amt;
                                if (rem is not 0)
                                {
                                    amt += (byte)rem;
                                }
                                if (((ticksEX % 96 == 0) || ((ticksEX % 96) + amt) > 96) && !wholeNoteMarkDisplayed)
                                {
                                    trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                }
                                else
                                {
                                    wholeNoteMarkDisplayed = false;
                                }
                                trackEventsEX.Insert(0, e);
                                var ex = 0;
                                if (((ticksEX % 96) + amt) > 96)
                                {
                                    ex = 96 - ((ticksEX + amt) % 96);
                                }
                                ticksEX += amt + ex; // TODO: Separate by 96 ticks
                                // returnTimeSignatureDisplayed = false;
                                // jumpTimeSignatureDisplayed = false;
                                // callTimeSignatureDisplayed = false;
                                break;
                            }
                        case EndOfTieCommand c:
                            {
                                if ((ticksEX % 96) == 0 && !wholeNoteMarkDisplayed)
                                {
                                    trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                    wholeNoteMarkDisplayed = true;
                                }
                                trackEventsEX.Insert(0, e);
                                break;
                            }
                        case CallCommand c:
                            {
                                if (!wholeNoteMarkDisplayed)
                                {
                                    trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                }
                                trackEventsEX.Insert(0, e);
                                trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                wholeNoteMarkDisplayed = true;
                                if ((ticksEX % 96) != 0)
                                {
                                    ticksEX += 96 - (ticksEX % 96);
                                }
                                // callTimeSignatureEnabled = true;
                                break;
                            }
                        case JumpCommand c:
                            {
                                if ((ticksEX % 96) == 0 && !wholeNoteMarkDisplayed)
                                {
                                    trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                    wholeNoteMarkDisplayed = true;
                                }
                                trackEventsEX.Insert(0, e);
                                break;
                            }
                        case ReturnCommand c:
                            {
                                if ((ticksEX % 96) == 0 && !wholeNoteMarkDisplayed)
                                {
                                    trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                    wholeNoteMarkDisplayed = true;
                                }
                                trackEventsEX.Insert(0, e);
                                break;
                            }
                        default:
                            {
                                trackEventsEX.Insert(0, e);
                                break;
                            }
                    }
                    foreach (SongEvent ev in evts)
                    {
                        if (ev.Command is RepeatCommand r && r.Pointer.Offset == eOffset)
                        {
                            trackEventsEX.Insert(0, new SongEvent(eOffset, new RepeatFlag()));
                            break;
                        }
                    }
                    foreach (SongEvent ev in evts)
                    {
                        if (ev.Command is CallCommand c && c.Pointer.Offset == eOffset)
                        {
                            trackEventsEX.Insert(0, new SongEvent(eOffset, new CallFlag()));
                            wholeNoteMarkDisplayed = true;
                            if ((ticksEX % 96) != 0)
                            {
                                ticksEX += 96 - (ticksEX % 96);
                            }
                            break;
                        }
                    }
                    foreach (SongEvent ev in evts)
                    {
                        if (ev.Command is JumpCommand j && j.Pointer.Offset == eOffset)
                        {
                            if (!wholeNoteMarkDisplayed)
                            {
                                trackEventsEX.Insert(0, new SongEvent(0, new WholeNoteMark()));
                                wholeNoteMarkDisplayed = true;
                            }
                            trackEventsEX.Insert(0, new SongEvent(eOffset, new JumpFlag()));
                            break;
                        }
                    }
                }
                wholeNoteMarkDisplayed = false;
                for (int i = 0; i < trackEventsEX.Count; i++)
                {
                    SongEvent e = trackEventsEX[i];
                    // prevCommand = i > 0 ? trackEvents[i - 1].Command : null!; // If the trackEvents index is more than 0, it'll apply the prevCommand, otherwise it's null
                    // nextCommand = i < trackEventsEX.Count - 1 ? trackEventsEX[i + 1].Command : null!; // If the trackEvents index is less than the number of trackEvents, it'll apply the nextCommand, otherwise it's null
                    int eOffset = (int)e.Offset;
                    // string prevLabel = "";
                    // foreach (SongEvent ev in evts)
                    // {
                    //     if (ev.Command is JumpCommand j && j.Offset == e.Offset)
                    //     {
                    //         if (prevLabel != "GOTO")
                    //         {
                    //             file.WriteLine($"{labelsGOTO[eOffset]}:");
                    //             file.WriteLine($"@ {timeSignatureNum:D3}   ----------------------------------------");
                    //             timeSignatureDisplayed = true;
                    //             timeSignatureGotoNum = timeSignatureNum;
                    //             prevParam1 = "";
                    //             prevParam2 = "";
                    //             prevParam3 = "";
                    //             prevLabel = "GOTO";
                    //         }
                    //         break;
                    //     }
                    // }
                    // foreach (SongEvent ev in evts)
                    // {
                    //     if (ev.Command is RepeatCommand r && r.Offset == e.Offset)
                    //     {
                    //         if (prevLabel != "REPT")
                    //         {
                    //             file.WriteLine($"{labelsREPT[eOffset]}:");
                    //             file.WriteLine($"@ {timeSignatureNum++:D3}   ----------------------------------------");
                    //             timeSignatureDisplayed = true;
                    //             prevParam1 = "";
                    //             prevParam2 = "";
                    //             prevParam3 = "";
                    //             prevLabel = "REPT";
                    //         }
                    //         break;
                    //     }
                    // }
                    // foreach (SongEvent ev in evts)
                    // {
                    //     if (ev.Command is CallCommand c && c.Offset == e.Offset)
                    //     {
                    //         if (prevLabel != "PATT")
                    //         {
                    //             if (!timeSignatureDisplayed)
                    //             {
                    //                 file.WriteLine($"@ {timeSignatureNum:D3}   ----------------------------------------");
                    //                 timeSignatureDisplayed = true;
                    //                 timeSignaturePattDisplayed = true;
                    //                 labelsPATT.Remove(eOffset);
                    //                 labelsPATT.Add(eOffset, $"{label}_{num}_{timeSignatureNum++:D3}");
                    //             }
                    //             else
                    //             {
                    //                 labelsPATT.Remove(eOffset);
                    //                 labelsPATT.Add(eOffset, $"{label}_{num}_{timeSignatureNum++:D3}");
                    //             }
                    //             file.WriteLine($"{labelsPATT[eOffset]}:");
                    //             prevParam1 = "";
                    //             prevParam2 = "";
                    //             prevLabel = "PATT";
                    //         }
                    //         continue;
                    //     }
                    // }
                    var rmd96 = ticks % 96;
                    var tdiv96 = ticks / 96;
                    // if (!timeSignatureDisplayed && (ticks % 96 == 0) && (ticks / 96 != 0) && (e.Command is not JumpCommand && e.Command is not RepeatCommand && e.Command is not ReturnCommand && e.Command is not EndOfTieCommand))
                    // {
                    //     file.WriteLine($"@ {timeSignatureNum++:D3}   ----------------------------------------");
                    //     timeSignatureDisplayed = true;
                    // }
                    //if (offsets.Contains(eOffset))
                    //{
                    //    file.WriteLine($"{labels[eOffset]}:");
                    //    prevParam1 = "";
                    //    prevParam2 = "";
                    //    prevParam3 = "";
                    //}
                    switch (e.Command)
                    {
                        case null:
                            continue;
                        case TempoCommand c:
                            file.WriteLine($"\t.byte\tTEMPO , {c.Tempo}*{label}_tbs/2");
                            prevParam1 = "TEMPO";
                            prevParam2 = $"{c.Tempo}*{label}_tbs/2";
                            prevParam3 = "";
                            prevParam4 = "";
                            prevCommand = c;
                            break;
                        case RestCommand c:
                            {
                                byte amt = (byte)MP2KUtils.RestTable.BinarySearch(c.Rest);
                                int rem = c.Rest - amt;
                                if (rem is not 0)
                                {
                                    amt += (byte)rem;
                                }
                                var t = ticks % 96;
                                var ta = t + amt;
                                ticks += amt; // TODO: Separate by 96 ticks
                                if (!wholeNoteMarkRestBegin && !wholeNoteMarkNoteBegin && (!wholeNoteMarkDisplayed || !wholeNoteMarkPattDisplayed))
                                {
                                    // file.WriteLine($"@ {timeSignatureNum++:D3}   ----------------------------------------");
                                    // timeSignatureDisplayed = true;
                                    wholeNoteMarkRestBegin = true;
                                    var r = 96 - (ticks % 96);
                                    ticks += r;
                                }
                                if (ta > 96)
                                {
                                    ticks += 96 - (ticks % 96);
                                }
                                file.WriteLine($"\t.byte\tW{amt:D2}");
                                //if (rem != 0)
                                //{
                                //    file.WriteLine($"\t.byte\tW{rem:D2}");
                                //}
                                if (prevCommand is not RestCommand || amt == 96)
                                {
                                    wholeNoteMarkDisplayed = false;
                                }
                                if (wholeNoteGotoNum == wholeNoteNum)
                                {
                                    wholeNoteNum++;
                                }
                                prevRest = c.Rest;
                                prevCommand = c;
                                break;
                            }
                        case NoteCommand c:
                            {
                                // Hide base note, velocity and duration
                                byte baseDur = c.Duration == -1 ? (byte)0 : (byte)MP2KUtils.RestTable.BinarySearch((byte)c.Duration);
                                int rem = c.Duration - baseDur;
                                if (rem is not 0)
                                {
                                    baseDur = (byte)(baseDur + rem);
                                    rem = c.Duration - baseDur;
                                }
                                string name = c.Duration == -1 ? "TIE" : $"N{baseDur:D2}";
                                string not = ConfigUtils.GetKeyNameASM(c.Note);
                                string vel = $"v{c.Velocity:D3}";

                                var n = "\t.byte\t\t";
                                if (name != prevParam1 || prevCommand is NoteCommand)
                                {
                                    if (name != prevParam1 || prevCommand is not NoteCommand || wholeNoteMarkPattDisplayed)
                                    {
                                        n += $"{name}   ";
                                    }
                                    if (not != prevParam2 || vel != prevParam3 || wholeNoteMarkPattDisplayed)
                                    {
                                        if (n == "\t.byte\t\t")
                                        {
                                            n += $"{name}   ";
                                        }
                                        n += $", {not} ";
                                        prevParam2 = not;
                                    }
                                }
                                else
                                {
                                    if (not != prevParam2 || vel != prevParam3)
                                    {
                                        n += $"        {not} ";
                                        prevParam2 = not;
                                    }
                                }
                                if (name == "TIE" && prevParam1 == "EOT" && !wholeNoteMarkDisplayed)
                                {
                                    file.WriteLine($"@ {wholeNoteNum++:D3}   ----------------------------------------");
                                    wholeNoteMarkDisplayed = true;
                                }
                                prevParam1 = name;
                                if (c.Duration != -1 && rem != 0)
                                {
                                    if ($"gtp{rem}" != prevParam4)
                                    {
                                        n += vel != prevParam3 ? $", {vel}, gtp{rem}" : $"      , gtp{rem}";
                                        prevParam3 = vel;
                                        prevParam4 = $"gtp{rem}";
                                    }
                                    else
                                    {
                                        n += vel != prevParam3 ? $", {vel}" : "";
                                        prevParam3 = vel;
                                        prevParam4 = "";
                                    }
                                }
                                else
                                {
                                    if (vel != prevParam3 || wholeNoteMarkPattDisplayed)
                                    {
                                        if (n == "\t.byte\t\t")
                                        {
                                            n += $"{name}   ";
                                        }
                                        n += $", {vel}";
                                    }
                                    else
                                    {
                                        n += "";
                                    }
                                    prevParam3 = vel;
                                    prevParam4 = "";
                                }

                                if (n == "\t.byte\t\t")
                                {
                                    n += $"{name}   ";
                                }
                                file.WriteLine(n);
                                prevCommand = c;
                                wholeNoteMarkPattDisplayed = false;
                                if (!wholeNoteMarkRestBegin && !wholeNoteMarkNoteBegin)
                                {
                                    wholeNoteMarkNoteBegin = true;
                                }

                                break;
                            }

                        case EndOfTieCommand c:
                            {
                                if (c.Note == -1)
                                {
                                    file.WriteLine("\t.byte\t\tEOT   ");
                                    wholeNoteMarkDisplayed = false;
                                    prevParam1 = "EOT";
                                }
                                else
                                {
                                    file.WriteLine($"\t.byte\t\tEOT   , {ConfigUtils.GetKeyNameASM(c.Note)}");
                                    wholeNoteMarkDisplayed = true;
                                    prevParam1 = "EOT";
                                    prevParam2 = $"{ConfigUtils.GetKeyNameASM(c.Note)}";
                                }
                                prevCommand = c;
                                break;
                            }
                        case VoiceCommand c:
                            {
                                file.WriteLine($"\t.byte\t\tVOICE , {c.Voice}");
                                prevParam1 = "VOICE";
                                prevCommand = c;
                                break;
                            }
                        case VolumeCommand c:
                            {
                                var v = $"\t.byte\t\t";
                                v += "VOL" != prevParam1 ? "VOL   ," : "       ";
                                double d = baseVolume / (double)0x7F;
                                int vol = (int)(c.Volume / d);
                                // If there are rounding errors, fix them (happens if baseVolume is not 127 and baseVolume is not vol.Volume)
                                if (vol * baseVolume / 0x7F == c.Volume - 1)
                                {
                                    vol++;
                                }
                                v += $" {vol}*{label}_mvl/mxv";
                                prevParam1 = "VOL";
                                prevCommand = c;
                                file.WriteLine(v);
                                break;
                            }
                        case PanpotCommand c:
                            {
                                var pan = $"\t.byte\t\t";
                                if (prevParam1 != "PAN")
                                {
                                    pan += "PAN   ";
                                    pan += $", {ConfigUtils.CenterValueString(c.Panpot)}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{ConfigUtils.CenterValueString(c.Panpot)}")
                                    {
                                        pan += "      ";
                                        pan += $"  {ConfigUtils.CenterValueString(c.Panpot)}";
                                    }
                                }
                                if (pan == $"\t.byte\t\t")
                                {
                                    pan += $"PAN   , {ConfigUtils.CenterValueString(c.Panpot)}";
                                }
                                prevParam1 = "PAN";
                                prevCommand = c;
                                file.WriteLine(pan);
                                break;
                            }
                        case PitchBendCommand c:
                            {
                                var bend = $"\t.byte\t\t";
                                if (prevParam1 != "BEND")
                                {
                                    bend += "BEND  ";
                                    bend += $", {ConfigUtils.CenterValueString(c.Bend)}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{ConfigUtils.CenterValueString(c.Bend)}")
                                    {
                                        bend += "      ";
                                        bend += $"  {ConfigUtils.CenterValueString(c.Bend)}";
                                    }
                                }
                                if (bend == $"\t.byte\t\t")
                                {
                                    bend += $"BEND  , {ConfigUtils.CenterValueString(c.Bend)}";
                                }
                                prevParam1 = "BEND";
                                prevCommand = c;
                                file.WriteLine(bend);
                                break;
                            }
                        case TuneCommand c:
                            {
                                var tune = $"\t.byte\t\t";
                                if (prevParam1 != "TUNE")
                                {
                                    tune += "TUNE  ";
                                    tune += $", {ConfigUtils.CenterValueString(c.Tune)}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{ConfigUtils.CenterValueString(c.Tune)}")
                                    {
                                        tune += "      ";
                                        tune += $"  {ConfigUtils.CenterValueString(c.Tune)}";
                                    }
                                }
                                if (tune == $"\t.byte\t\t")
                                {
                                    tune += $"TUNE  , {ConfigUtils.CenterValueString(c.Tune)}";
                                }
                                prevParam1 = "TUNE";
                                prevCommand = c;
                                file.WriteLine(tune);
                                break;
                            }
                        case PitchBendRangeCommand c:
                            {
                                var bendr = $"\t.byte\t\t";
                                if (prevParam1 != "BENDR")
                                {
                                    bendr += "BENDR ";
                                    bendr += $", {c.Range}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{c.Range}")
                                    {
                                        bendr += "      ";
                                        bendr += $"  {c.Range}";
                                    }
                                }
                                if (bendr == $"\t.byte\t\t")
                                {
                                    bendr += $"BENDR , {c.Range}";
                                }
                                prevParam1 = "BENDR";
                                prevCommand = c;
                                file.WriteLine(bendr);
                                break;
                            }
                        case LFOSpeedCommand c:
                            {
                                var lfos = $"\t.byte\t\t";
                                if (prevParam1 != "LFOS")
                                {
                                    lfos += "LFOS  ";
                                    lfos += $", {c.Speed}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{c.Speed}")
                                    {
                                        lfos += "      ";
                                        lfos += $"  {c.Speed}";
                                    }
                                }
                                if (lfos == $"\t.byte\t\t")
                                {
                                    lfos += $"LFOS  , {c.Speed}";
                                }
                                prevParam1 = "LFOS";
                                prevCommand = c;
                                file.WriteLine(lfos);
                                break;
                            }
                        case LFODelayCommand c:
                            {
                                var lfodl = $"\t.byte\t\t";
                                if (prevParam1 != "LFODL")
                                {
                                    lfodl += "LFODL ";
                                    lfodl += $", {c.Delay}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{c.Delay}")
                                    {
                                        lfodl += "      ";
                                        lfodl += $"  {c.Delay}";
                                    }
                                }
                                if (lfodl == $"\t.byte\t\t")
                                {
                                    lfodl += $"LFODL , {c.Delay}";
                                }
                                prevParam1 = "LFODL";
                                prevCommand = c;
                                file.WriteLine(lfodl);
                                break;
                            }
                        case LFODepthCommand c:
                            {
                                var mod = $"\t.byte\t\t";
                                if (prevParam1 != "MOD")
                                {
                                    mod += "MOD   ";
                                    mod += $", {c.Depth}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{c.Depth}")
                                    {
                                        mod += "      ";
                                        mod += $"  {c.Depth}";
                                    }
                                }
                                if (mod == $"\t.byte\t\t")
                                {
                                    mod += $"MOD   , {c.Depth}";
                                }
                                prevParam1 = "MOD";
                                prevCommand = c;
                                file.WriteLine(mod);
                                break;
                            }
                        case LFOTypeCommand c:
                            {
                                var modt = $"\t.byte\t\t";
                                if (prevParam1 != "MODT")
                                {
                                    modt += "MODT  ";
                                    modt += $", {c.Type}";
                                }
                                else
                                {
                                    if (prevParam2 != $"{c.Type}")
                                    {
                                        modt += "      ";
                                        modt += $"  {c.Type}";
                                    }
                                }
                                if (modt == $"\t.byte\t\t")
                                {
                                    modt += $"MODT  , {c.Type}";
                                }
                                prevParam1 = "MODT";
                                prevCommand = c;
                                file.WriteLine(modt);
                                break;
                            }
                        case PriorityCommand c:
                            {
                                file.WriteLine($"\t.byte\tPRIO , {c.Priority}");
                                prevParam1 = "PRIO";
                                prevCommand = c;
                                break;
                            }
                        case TransposeCommand c:
                            {
                                file.WriteLine($"\t.byte\tKEYSH , {label}_key+{c.Transpose}");
                                file.WriteLine($"@ {wholeNoteNum++:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                prevParam1 = "KEYSH";
                                prevParam2 = $"{label}_key+{c.Transpose}";
                                prevParam3 = "";
                                prevParam4 = "";
                                prevCommand = c;
                                break;
                            }
                        case JumpCommand c:
                            {
                                file.WriteLine("\t.byte\tGOTO");
                                file.WriteLine($"\t .word\t{labelsGOTO[(int)c.Pointer.Offset]}");
                                if (labelsGOTO.TryGetValue(eOffset, out string? value))
                                {
                                    file.WriteLine($"{value}:");
                                }
                                else
                                {
                                    file.WriteLine($"{label}_{num}_B{jumps++}:");
                                }
                                // file.WriteLine($"@ {timeSignatureNum++:D3}   ----------------------------------------");
                                // timeSignatureDisplayed = true;
                                prevParam1 = "GOTO";
                                prevParam2 = "";
                                prevParam3 = "";
                                prevParam4 = "";
                                prevCommand = c;
                                break;
                            }
                        case RepeatCommand c:
                            {
                                file.WriteLine($"\t.byte\t\tREPT  , {c.Times}");
                                file.WriteLine($"\t .word\t{labelsREPT[(int)c.Pointer.Offset]}");
                                file.WriteLine($"@ {wholeNoteNum++:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                prevParam1 = "REPT";
                                prevParam2 = $"{c.Times}";
                                prevParam3 = "";
                                prevParam4 = "";
                                prevCommand = c;
                                break;
                            }
                        case FinishCommand c:
                            {
                                if (c.Type == 0xB1)
                                {
                                    file.WriteLine("\t.byte\tFINE");
                                }
                                else
                                {
                                    file.WriteLine("\t.byte\t0xB6\t@PREV");
                                }
                                prevCommand = c;
                                break;
                            }
                        case CallCommand c:
                            {
                                file.WriteLine("\t.byte\tPATT");
                                file.WriteLine($"\t .word\t{labelsPATT[(int)c.Pointer.Offset]}");
                                //file.WriteLine($"@ {separatorNum++:D3}   ----------------------------------------");
                                //displayed = true;
                                wholeNoteMarkDisplayed = false;
                                if (wholeNoteGotoNum == wholeNoteNum)
                                {
                                    wholeNoteNum++;
                                }
                                prevParam1 = "PATT";
                                prevParam2 = "";
                                prevParam3 = "";
                                prevCommand = c;
                                break;
                            }
                        case ReturnCommand c:
                            {
                                file.WriteLine("\t.byte\tPEND");
                                wholeNoteMarkDisplayed = false;
                                prevCommand = c;
                                break;
                            }
                        case MemoryAccessCommand c:
                            {
                                file.WriteLine($"\t.byte\tMEMACC, {c.Operator}, 0x{c.MemoryAreaAddress:X2}, {c.Data}");
                                prevParam1 = "MEMACC";
                                prevParam2 = $"{c.Operator,4}";
                                prevParam3 = $"{c.MemoryAreaAddress,4}";
                                prevParam4 = $"{c.Data}";
                                prevCommand = c;
                                break;
                            }
                        case LibraryCommand c:
                            {
                                var xcmd = $"\t.byte\t\t";
                                if (prevParam1 != "XCMD")
                                {
                                    xcmd += "XCMD  ,";
                                    xcmd += $" {c.LibraryCommandType} , {c.Argument}";
                                }
                                else
                                {
                                    xcmd += "       ";
                                    xcmd += $" {c.LibraryCommandType} , {c.Argument}";
                                }
                                file.WriteLine(xcmd);
                                prevParam1 = "XCMD";
                                prevParam2 = $"{c.LibraryCommandType}";
                                prevParam3 = $"{c.Argument}";
                                prevCommand = c;
                                break;
                            }
                        case WholeNoteMark t:
                            {
                                file.WriteLine($"@ {wholeNoteNum++:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                break;
                            }
                        case JumpFlag j:
                            {
                                file.WriteLine($"{labelsGOTO[eOffset]}:");
                                // file.WriteLine($"@ {timeSignatureNum:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                wholeNoteGotoNum = wholeNoteNum;
                                prevParam1 = "";
                                prevParam2 = "";
                                prevParam3 = "";
                                prevParam4 = "";
                                break;
                            }
                        case RepeatFlag r:
                            {
                                file.WriteLine($"{labelsREPT[eOffset]}:");
                                file.WriteLine($"@ {wholeNoteNum++:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                break;
                            }
                        case CallFlag c:
                            {
                                file.WriteLine($"@ {wholeNoteNum:D3}   ----------------------------------------");
                                wholeNoteMarkDisplayed = true;
                                wholeNoteMarkPattDisplayed = true;
                                labelsPATT.Remove(eOffset);
                                labelsPATT.Add(eOffset, $"{label}_{num}_{wholeNoteNum++:D3}");
                                file.WriteLine($"{labelsPATT[eOffset]}:");
                                prevParam1 = "";
                                prevParam2 = "";
                                prevParam3 = "";
                                prevParam4 = "";
                                break;
                            }
                    }
                    // prevLabel = "";
                }
            }

            file.WriteLine();
            file.WriteLine("@******************************************************@");
            file.WriteLine("\t.align\t2");
            file.WriteLine();
            file.WriteLine($"{label}:");
            file.WriteLine($"\t.byte\t{Tracks.Length}\t@ NumTrks");
            file.WriteLine($"\t.byte\t{(this is MP2KLoadedSong mp2kSequence ? mp2kSequence.Header.NumBlocks : 0)}\t@ NumBlks");
            file.WriteLine($"\t.byte\t{label}_pri\t@ Priority");
            file.WriteLine($"\t.byte\t{label}_rev\t@ Reverb.");
            file.WriteLine();
            file.WriteLine($"\t.word\t{label}_grp");
            file.WriteLine();
            for (int i = 0; i < Tracks.Length; i++)
            {
                file.WriteLine($"\t.word\t{label}_{i + 1}");
            }

            file.WriteLine();
            file.WriteLine("\t.end");
        }
    }
}
