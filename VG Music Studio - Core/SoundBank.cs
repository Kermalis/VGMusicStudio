using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core;

public abstract class SoundBank : IOffset, IEnumerable<IVoiceInfo>
{
    public virtual int Offset { get; set; }

    public abstract IVoiceInfo ElementAt(int selectedIndex);
    public abstract IEnumerator<IVoiceInfo> GetEnumerator();
    public abstract object GetSubVoiceEntry(IVoiceInfo voiceInfo);
    public abstract string GetBytesToString(int voiceIndex);
    public abstract bool IsTableAddress(int voiceIndex);
    public abstract bool HasADSR(int voiceIndex);
    public abstract bool IsValidVoiceAddress(int voiceIndex);
    public abstract int GetVoiceAddress(int voiceIndex);
    public abstract bool IsValidADSR(int voiceIndex);
    public abstract void GetADSRValues(int voiceIndex, out byte attackValue, out byte decayValue, out byte sustainValue, out byte releaseValue);
    public abstract void SetADSRValues(int voiceIndex, in byte attackValue, in byte decayValue, in byte sustainValue, in byte releaseValue);
    public abstract void SetAddressPointer(int voiceIndex, int address);
    public abstract bool IsPSGInstrument(int voiceIndex);
    protected abstract void Load();
    public abstract SoundBank LoadFromAddress(int tableOffset);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<IVoiceInfo> IEnumerable<IVoiceInfo>.GetEnumerator() => (IEnumerator<IVoiceInfo>)GetEnumerator();
}