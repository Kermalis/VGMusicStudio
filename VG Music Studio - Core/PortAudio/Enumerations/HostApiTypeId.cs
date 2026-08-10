namespace PortAudio;

/// <summary>
/// Unchanging unique identifiers for each supported host API. This type
/// is used in the PaHostApiInfo structure. The values are guaranteed to be
/// unique and to never change, thus allowing code to be written that
/// conditionally uses host API specific extensions.
///
/// New type ids will be allocated when support for a host API reaches
/// "public alpha" status, prior to that developers should use the
/// paInDevelopment type id.
///
/// @see PaHostApiInfo
/// </summary>
public enum HostApiTypeId
{
    paInDevelopment=0, /* use while developing support for a new host API */
    paDirectSound=1,
    paMME=2,
    paASIO=3,
    paSoundManager=4,
    paCoreAudio=5,
    paOSS=7,
    paALSA=8,
    paAL=9,
    paBeOS=10,
    paWDMKS=11,
    paJACK=12,
    paWASAPI=13,
    paAudioScienceHPI=14,
    paAudioIO=15,
    paPulseAudio=16,
    paSndio=17
}