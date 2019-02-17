namespace Kermalis.GBAMusicStudio.Core
{
    // Used everywhere
    interface IOffset
    {
        int GetOffset();
        void SetOffset(int newOffset);
    }

    // Used in the VoiceTableEditor. GetName() is also used for the UI
    interface IVoiceTableInfo : IOffset
    {
        string GetName();
    }
    interface IVoice : IVoiceTableInfo
    {
        sbyte GetRootNote();
    }

    // Used for song events
    interface ICommand
    {
        string Name { get; }
        string Arguments { get; }
    }

    // Used in the SoundMixer
    interface IWrappedSample : IOffset
    {
        WrappedSample GetSample();
    }
}
