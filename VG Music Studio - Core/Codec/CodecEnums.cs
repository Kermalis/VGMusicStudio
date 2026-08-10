namespace Kermalis.VGMusicStudio.Core.Codec;

/* This code has been copied directly from vgmstream.h in VGMStream's repository    *
 * and modified into C# code to work with VGMS. Link to its repository can be       *
 * found here: https://github.com/vgmstream/vgmstream                               */
public enum CodecType
{
    codec_SILENCE,         /* generates silence */

    /* PCM */
    codec_PCM16LE,         /* little endian 16-bit PCM */
    codec_PCM16BE,         /* big endian 16-bit PCM */
    codec_PCM16_int,       /* 16-bit PCM with sample-level interleave (for blocks) */

    codec_PCM8,            /* 8-bit PCM */
    codec_PCM8_int,        /* 8-bit PCM with sample-level interleave (for blocks) */
    codec_PCM8_U,          /* 8-bit PCM, unsigned (0x80 = 0) */
    codec_PCM8_U_int,      /* 8-bit PCM, unsigned (0x80 = 0) with sample-level interleave (for blocks) */
    codec_PCM8_SB,         /* 8-bit PCM, sign bit (others are 2's complement) */
    codec_PCM4,            /* 4-bit PCM, signed */
    codec_PCM4_U,          /* 4-bit PCM, unsigned */

    codec_ULAW,            /* 8-bit u-Law (non-linear PCM) */
    codec_ULAW_int,        /* 8-bit u-Law (non-linear PCM) with sample-level interleave (for blocks) */
    codec_ALAW,            /* 8-bit a-Law (non-linear PCM) */

    codec_PCMFLOAT,        /* 32-bit float PCM */
    codec_PCM24LE,         /* little endian 24-bit PCM */
    codec_PCM24BE,         /* big endian 24-bit PCM */

    /* ADPCM */
    codec_CRI_ADX,         /* CRI ADX */
    codec_CRI_ADX_fixed,   /* CRI ADX, encoding type 2 with fixed coefficients */
    codec_CRI_ADX_exp,     /* CRI ADX, encoding type 4 with exponential scale */
    codec_CRI_ADX_enc_8,   /* CRI ADX, type 8 encryption (God Hand) */
    codec_CRI_ADX_enc_9,   /* CRI ADX, type 9 encryption (PSO2) */

    codec_NGC_DSP,         /* Nintendo DSP ADPCM */
    codec_NGC_DSP_subint,  /* Nintendo DSP ADPCM with frame subinterframe */
    codec_NGC_DTK,         /* Nintendo DTK ADPCM (hardware disc), also called TRK or ADP */
    codec_NGC_AFC,         /* Nintendo AFC ADPCM */
    codec_VADPCM,          /* Silicon Graphics VADPCM */

    codec_G721,            /* CCITT G.721 */

    codec_XA,              /* CD-ROM XA 4-bit */
    codec_XA8,             /* CD-ROM XA 8-bit */
    codec_XA_EA,           /* EA's Saturn XA (not to be confused with EA-XA) */
    codec_PSX,             /* Sony PS ADPCM (VAG) */
    codec_PSX_badflags,    /* Sony PS ADPCM with custom flag byte */
    codec_PSX_cfg,         /* Sony PS ADPCM with configurable frame size (int math) */
    codec_PSX_pivotal,     /* Sony PS ADPCM with configurable frame size (float math) */
    codec_HEVAG,           /* Sony PSVita ADPCM */

    codec_EA_XA,           /* Electronic Arts EA-XA ADPCM v1 (stereo) aka "EA ADPCM" */
    codec_EA_XA_int,       /* Electronic Arts EA-XA ADPCM v1 (mono/interleave) */
    codec_EA_XA_V2,        /* Electronic Arts EA-XA ADPCM v2 */
    codec_MAXIS_XA,        /* Maxis EA-XA ADPCM */
    codec_EA_XAS_V0,       /* Electronic Arts EA-XAS ADPCM v0 */
    codec_EA_XAS_V1,       /* Electronic Arts EA-XAS ADPCM v1 */

    codec_IMA,             /* IMA ADPCM (stereo or mono, low nibble first) */
    codec_IMA_int,         /* IMA ADPCM (mono/interleave, low nibble first) */
    codec_DVI_IMA,         /* DVI IMA ADPCM (stereo or mono, high nibble first) */
    codec_DVI_IMA_int,     /* DVI IMA ADPCM (mono/interleave, high nibble first) */
    codec_NW_IMA,
    codec_SNDS_IMA,        /* Heavy Iron Studios .snds IMA ADPCM */
    codec_QD_IMA,
    codec_WV6_IMA,         /* Gorilla Systems WV6 4-bit IMA ADPCM */
    codec_HV_IMA,          /* High Voltage 4-bit IMA ADPCM */
    codec_FFTA2_IMA,       /* Final Fantasy Tactics A2 4-bit IMA ADPCM */
    codec_BLITZ_IMA,       /* Blitz Games 4-bit IMA ADPCM */

    codec_MS_IMA,          /* Microsoft IMA ADPCM */
    codec_MS_IMA_mono,     /* Microsoft IMA ADPCM (mono/interleave) */
    codec_XBOX_IMA,        /* XBOX IMA ADPCM */
    codec_XBOX_IMA_mch,    /* XBOX IMA ADPCM (multichannel) */
    codec_XBOX_IMA_int,    /* XBOX IMA ADPCM (mono/interleave) */
    codec_NDS_IMA,         /* IMA ADPCM w/ NDS layout */
    codec_DAT4_IMA,        /* Eurocom 'DAT4' IMA ADPCM */
    codec_RAD_IMA,         /* Radical IMA ADPCM */
    codec_RAD_IMA_mono,    /* Radical IMA ADPCM (mono/interleave) */
    codec_APPLE_IMA4,      /* Apple Quicktime IMA4 */
    codec_FSB_IMA,         /* FMOD's FSB multichannel IMA ADPCM */
    codec_WWISE_IMA,       /* Audiokinetic Wwise IMA ADPCM */
    codec_REF_IMA,         /* Reflections IMA ADPCM */
    codec_AWC_IMA,         /* Rockstar AWC IMA ADPCM */
    codec_UBI_IMA,         /* Ubisoft IMA ADPCM */
    codec_UBI_SCE_IMA,     /* Ubisoft SCE IMA ADPCM */
    codec_H4M_IMA,         /* H4M IMA ADPCM (stereo or mono, high nibble first) */
    codec_MTF_IMA,         /* Capcom MT Framework IMA ADPCM */
    codec_CD_IMA,          /* Crystal Dynamics IMA ADPCM */

    codec_MSADPCM,         /* Microsoft ADPCM (stereo/mono) */
    codec_MSADPCM_int,     /* Microsoft ADPCM (mono) */
    codec_MSADPCM_ck,      /* Microsoft ADPCM (Cricket Audio variation) */
    codec_WS,              /* Westwood Studios VBR ADPCM */

    codec_AICA,            /* Yamaha AICA ADPCM (stereo) */
    codec_AICA_int,        /* Yamaha AICA ADPCM (mono/interleave) */
    codec_CP_YM,           /* Capcom's Yamaha ADPCM (stereo/mono) */
    codec_ASKA,            /* Aska ADPCM */
    codec_NXAP,            /* NXAP ADPCM */

    codec_TGC,             /* Tiger Game.com 4-bit ADPCM */

    codec_PSX_DSE_SQUARESOFT,  /* SquareSoft Digital Sound Elements 16-bit PCM (For PSX) */
    codec_PS2_DSE_PROCYON,     /* Procyon Studio Digital Sound Elements ADPCM (PS2 Version, encoded with VAG-ADPCM) */
    codec_NDS_DSE_PROCYON,     /* Procyon Studio Digital Sound Elements ADPCM (NDS Version, encoded with IMA-ADPCM) */
    codec_WII_DSE_PROCYON,     /* Procyon Studio Digital Sound Elements ADPCM (Wii Version, encoded with DSP-ADPCM) */
    codec_L5_555,          /* Level-5 0x555 ADPCM */
    codec_LSF,             /* lsf ADPCM (Fastlane Street Racing iPhone)*/
    codec_MTAF,            /* Konami MTAF ADPCM */
    codec_MTA2,            /* Konami MTA2 ADPCM */
    codec_MC3,             /* Paradigm MC3 3-bit ADPCM */
    codec_FADPCM,          /* FMOD FADPCM 4-bit ADPCM */
    codec_ASF,             /* Argonaut ASF 4-bit ADPCM */
    codec_DSA,             /* Ocean DSA 4-bit ADPCM */
    codec_XMD,             /* Konami XMD 4-bit ADPCM */
    codec_TANTALUS,        /* Tantalus 4-bit ADPCM */
    codec_PCFX,            /* PC-FX 4-bit ADPCM */
    codec_OKI16,           /* OKI 4-bit ADPCM with 16-bit output and modified expand */
    codec_OKI4S,           /* OKI 4-bit ADPCM with 16-bit output and cuadruple step */
    codec_PTADPCM,         /* Platinum 4-bit ADPCM */
    codec_IMUSE,           /* LucasArts iMUSE Variable ADPCM */
    codec_COMPRESSWAVE,    /* CompressWave Huffman ADPCM */

    /* others */
    codec_SDX2,            /* SDX2 2:1 Squareroot-Delta-Exact compression DPCM */
    codec_SDX2_int,        /* SDX2 2:1 Squareroot-Delta-Exact compression with sample-level interleave */
    codec_CBD2,            /* CBD2 2:1 Cuberoot-Delta-Exact compression DPCM */
    codec_CBD2_int,        /* CBD2 2:1 Cuberoot-Delta-Exact compression, with sample-level interleave */
    codec_SASSC,           /* Activision EXAKT SASSC 8-bit DPCM */
    codec_DERF,            /* DERF 8-bit DPCM */
    codec_WADY,            /* WADY 8-bit DPCM */
    codec_NWA,             /* VisualArt's NWA DPCM */
    codec_ACM,             /* InterPlay ACM */
    codec_CIRCUS_ADPCM,    /* Circus 8-bit ADPCM */
    codec_UBI_ADPCM,       /* Ubisoft 4/6-bit ADPCM */

    codec_EA_MT,           /* Electronic Arts MicroTalk (linear-predictive speech codec) */
    codec_CIRCUS_VQ,       /* Circus VQ */
    codec_RELIC,           /* Relic Codec (DCT-based) */
    codec_CRI_HCA,         /* CRI High Compression Audio (MDCT-based) */
    codec_TAC,             /* tri-Ace Codec (MDCT-based) */
    codec_ICE_RANGE,       /* Inti Creates "range" codec */
    codec_ICE_DCT,         /* Inti Creates "DCT" codec */


    codec_OGG_VORBIS,      /* Xiph Vorbis with Ogg layer (MDCT-based) */
    codec_VORBIS_custom,   /* Xiph Vorbis with custom layer (MDCT-based) */


    codec_MPEG_custom,     /* MPEG audio with custom features (MDCT-based) */
    codec_MPEG_ealayer3,   /* EALayer3, custom MPEG frames */
    codec_MPEG_layer1,     /* MP1 MPEG audio (MDCT-based) */
    codec_MPEG_layer2,     /* MP2 MPEG audio (MDCT-based) */
    codec_MPEG_layer3,     /* MP3 MPEG audio (MDCT-based) */


    codec_G7221C,          /* ITU G.722.1 annex C (Polycom Siren 14) */


    codec_G719,            /* ITU G.719 annex B (Polycom Siren 22) */


    codec_MP4_AAC,         /* AAC (MDCT-based) */


    codec_ATRAC9,          /* Sony ATRAC9 (MDCT-based) */


    codec_CELT_FSB,        /* Custom Xiph CELT (MDCT-based) */


    codec_SPEEX,           /* Custom Speex (CELP-based) */


    codec_FFmpeg,          /* Formats handled by FFmpeg (ATRAC3, XMA, AC3, etc) */
}