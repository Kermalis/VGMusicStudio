using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MP2KEngine : Engine
{
    public static MP2KEngine? MP2KInstance { get; private set; }

    public override MP2KConfig Config { get; }
    public override MP2KMixer Mixer { get; }
    public override MP2KPlayer Player { get; }
    internal MP2KContext Context { get; }

    public override bool IsFileSystemFormat { get; } = false;

    private ICommand[]? _allowedCommands;

    public MP2KEngine(byte[] rom, bool mainPlaylistFirst = true)
    {
        if (rom.Length > GBAUtils.CARTRIDGE_CAPACITY)
        {
            throw new InvalidDataException($"The ROM is too large. Maximum size is 0x{GBAUtils.CARTRIDGE_CAPACITY:X7} bytes.");
        }

        Config = new MP2KConfig(rom, mainPlaylistFirst);
        Context = new(Config);
        Mixer = new MP2KMixer(Config);
        Player = new MP2KPlayer(Config, Context, Mixer);

        MP2KInstance = this;
        Instance = this;
    }

    public override ICommand[] GetCommands()
    {
        var types = new List<Type>();
        types.AddRange([
                    typeof(TempoCommand), typeof(RestCommand), typeof(NoteCommand), typeof(EndOfTieCommand),
                    typeof(VoiceCommand), typeof(VolumeCommand), typeof(PanpotCommand), typeof(PitchBendCommand),
                    typeof(TuneCommand), typeof(PitchBendRangeCommand), typeof(LFOSpeedCommand), typeof(LFODelayCommand),
                    typeof(LFODepthCommand), typeof(LFOTypeCommand), typeof(PriorityCommand), typeof(TransposeCommand),
                    typeof(JumpCommand), typeof(CallCommand), typeof(ReturnCommand), typeof(FinishCommand),
                    typeof(RepeatCommand), typeof(MemoryAccessCommand), typeof(LibraryCommand)
                ]);

        _allowedCommands = new ICommand[types.Count];
        int i = 0;
        foreach (Type type in types)
        {
            _allowedCommands[i++] = (ICommand)Activator.CreateInstance(type)!;
        }

        return _allowedCommands;
    }

    public override void Reload()
    {
        var config = Config;
        Dispose();
        _ = new MP2KEngine(config.ROM, false);
    }
    public override void Dispose()
    {
        base.Dispose();
        MP2KInstance = null;
    }
}
