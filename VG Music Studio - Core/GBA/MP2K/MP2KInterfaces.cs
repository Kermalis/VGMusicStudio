using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal interface ISequenceCommand : ICommand
{
    SequenceCommand SequenceType { get; }
}

internal interface IRunCommand : ICommand
{
    bool IsRepeated { get; }
}

internal interface IVoice : IVoiceInfo
{
    VoiceEntry VoiceEntry { get; }
    sbyte RootNote { get; }
}

// Used in the Voice Group Editor
internal interface ISample : IOffset
{
    MP2KSample Sample { get; }
}
