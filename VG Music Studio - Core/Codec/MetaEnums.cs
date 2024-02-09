namespace Kermalis.VGMusicStudio.Core.Codec;

/* This code has been copied directly from vgmstream.h in VGMStream's repository    *
 * and modified into C# code to work with VGMS. Link to its repository can be       *
 * found here: https://github.com/vgmstream/vgmstream                               */
public enum MetaType
{
    meta_SILENCE,

    meta_DSP_STD,           /* Nintendo standard GC ADPCM (DSP) header */
    meta_DSP_CSTR,          /* Star Fox Assault "Cstr" */
    meta_DSP_RS03,          /* Retro: Metroid Prime 2 "RS03" */
    meta_DSP_STM,           /* Paper Mario 2 STM */
    meta_AGSC,              /* Retro: Metroid Prime 2 title */
    meta_CSMP,              /* Retro: Metroid Prime 3 (Wii), Donkey Kong Country Returns (Wii) */
    meta_RFRM,              /* Retro: Donkey Kong Country Tropical Freeze (Wii U) */
    meta_DSP_MPDSP,         /* Monopoly Party single header stereo */
    meta_DSP_JETTERS,       /* Bomberman Jetters .dsp */
    meta_DSP_MSS,           /* Free Radical GC games */
    meta_DSP_GCM,           /* some of Traveller's Tales games */
    meta_DSP_STR,           /* Conan .str files */
    meta_DSP_SADB,          /* Procyon Studio Digtial Sound Elements DSP-ADPCM (Wii) .sad */
    meta_DSP_WSI,           /* .wsi */
    meta_IDSP_TT,           /* Traveller's Tales games */
    meta_DSP_WII_MUS,       /* .mus */
    meta_DSP_WII_WSD,       /* Phantom Brave (Wii) */
    meta_WII_NDP,           /* Vertigo (Wii) */
    meta_DSP_YGO,           /* Konami: Yu-Gi-Oh! The Falsebound Kingdom (NGC), Hikaru no Go 3 (NGC) */

    meta_STRM,              /* Nintendo/HAL Labs Nitro Soundmaker STRM */
    meta_RSTM,              /* Nintendo/HAL Labs NW4R Soundmaker RSTM (Revolution Stream, similar to STRM) */
    meta_AFC,               /* AFC */
    meta_AST,               /* AST */
    meta_RWSD,              /* Nintendo/HAL Labs NW4R Soundmaker single-stream RWSD */
    meta_RWAR,              /* Nintendo/HAL Labs NW4R Soundmaker single-stream RWAR */
    meta_RWAV,              /* Nintendo/HAL Labs NW4R Soundmaker contents of RWAR */
    meta_CWAV,              /* Nintendo/HAL Labs NW4C Soundmaker contents of CWAR */
    meta_FWAV,              /* Nintendo/HAL Labs NW4F Soundmaker contents of FWAR */
    meta_THP,               /* THP movie files */
    meta_SWAV,
    meta_NDS_RRDS,          /* Ridge Racer DS */
    meta_WII_BNS,           /* Wii BNS Banner Sound (similar to RSTM) */
    meta_WIIU_BTSND,        /* Wii U Boot Sound */

    meta_ADX_03,            /* CRI ADX "type 03" */
    meta_ADX_04,            /* CRI ADX "type 04" */
    meta_ADX_05,            /* CRI ADX "type 05" */
    meta_AIX,               /* CRI AIX */
    meta_AAX,               /* CRI AAX */
    meta_UTF_DSP,           /* CRI ADPCM_WII, like AAX with DSP */

    meta_DTK,
    meta_RSF,
    meta_HALPST,            /* HAL Labs HALPST */
    meta_GCSW,              /* GCSW (PCM) */
    meta_CAF,               /* tri-Crescendo CAF */
    meta_MYSPD,             /* U-Sing .myspd */
    meta_HIS,               /* Her Ineractive .his */
    meta_BNSF,              /* Bandai Namco Sound Format */

    meta_XA,                /* CD-ROM XA */
    meta_ADS,
    meta_NPS,
    meta_RXWS,
    meta_RAW_INT,
    meta_EXST,
    meta_SVAG_KCET,
    meta_PS_HEADERLESS,     /* headerless PS-ADPCM */
    meta_MIB_MIH,
    meta_PS2_MIC,           /* KOEI MIC File */
    meta_PS2_VAGi,          /* VAGi Interleaved File */
    meta_PS2_VAGp,          /* VAGp Mono File */
    meta_PS2_pGAV,          /* VAGp with Little Endian Header */
    meta_PS2_VAGp_AAAP,     /* Acclaim Austin Audio VAG header */
    meta_SEB,
    meta_STR_WAV,           /* Blitz Games STR+WAV files */
    meta_ILD,
    meta_PS2_PNB,           /* PsychoNauts Bgm File */
    meta_VPK,               /* VPK Audio File */
    meta_PS2_BMDX,          /* Beatmania thing */
    meta_PS2_IVB,           /* Langrisser 3 IVB */
    meta_PS2_SND,           /* some Might & Magics SSND header */
    meta_SVS,               /* Square SVS */
    meta_XSS,               /* Dino Crisis 3 */
    meta_SL3,               /* Test Drive Unlimited */
    meta_HGC1,              /* Knights of the Temple 2 */
    meta_AUS,               /* Various Capcom games */
    meta_RWS,               /* RenderWare games (only when using RW Audio middleware) */
    meta_FSB1,              /* FMOD Sample Bank, version 1 */
    meta_FSB2,              /* FMOD Sample Bank, version 2 */
    meta_FSB3,              /* FMOD Sample Bank, version 3.0/3.1 */
    meta_FSB4,              /* FMOD Sample Bank, version 4 */
    meta_FSB5,              /* FMOD Sample Bank, version 5 */
    meta_RWX,               /* Air Force Delta Storm (XBOX) */
    meta_XWB,               /* Microsoft XACT framework (Xbox, X360, Windows) */
    meta_PS2_XA30,          /* Driver - Parallel Lines (PS2) */
    meta_MUSC,              /* Krome PS2 games */
    meta_MUSX,
    meta_LEG,               /* Legaia 2 [no header_id] */
    meta_FILP,              /* Resident Evil - Dead Aim */
    meta_IKM,
    meta_STER,
    meta_BG00,              /* Ibara, Mushihimesama */
    meta_PS2_RSTM,          /* Midnight Club 3 */
    meta_PS2_KCES,          /* Dance Dance Revolution */
    meta_HXD,
    meta_VSV,
    meta_SCD_PCM,           /* Lunar - Eternal Blue */
    meta_PS2_PCM,           /* Konami KCEJ East: Ephemeral Fantasia, Yu-Gi-Oh! The Duelists of the Roses, 7 Blades */
    meta_PS2_RKV,           /* Legacy of Kain - Blood Omen 2 (PS2) */
    meta_PS2_VAS,           /* Pro Baseball Spirits 5 */
    meta_PS2_ENTH,          /* Enthusia */
    meta_SDT,               /* Baldur's Gate - Dark Alliance */
    meta_NGC_TYDSP,         /* Ty - The Tasmanian Tiger */
    meta_DC_STR,            /* SEGA Stream Asset Builder */
    meta_DC_STR_V2,         /* variant of SEGA Stream Asset Builder */
    meta_NGC_BH2PCM,        /* Bio Hazard 2 */
    meta_SAP,
    meta_DC_IDVI,           /* Eldorado Gate */
    meta_KRAW,              /* Geometry Wars - Galaxies */
    meta_PS2_OMU,           /* PS2 Int file with Header */
    meta_PS2_XA2,           /* XG3 Extreme-G Racing */
    meta_NUB,
    meta_IDSP_NL,           /* Mario Strikers Charged (Wii) */
    meta_IDSP_IE,           /* Defencer (GC) */
    meta_SPT_SPD,           /* Various (SPT+SPT DSP) */
    meta_ISH_ISD,           /* Various (ISH+ISD DSP) */
    meta_GSP_GSB,           /* Tecmo games (Super Swing Golf 1 & 2, Quamtum Theory) */
    meta_YDSP,              /* WWE Day of Reckoning */
    meta_FFCC_STR,          /* Final Fantasy: Crystal Chronicles */
    meta_UBI_JADE,          /* Beyond Good & Evil, Rayman Raving Rabbids */
    meta_GCA,               /* Metal Slug Anthology */
    meta_NGC_SSM,           /* Golden Gashbell Full Power */
    meta_PS2_JOE,           /* Wall-E / Pixar games */
    meta_NGC_YMF,           /* WWE WrestleMania X8 */
    meta_SADL,
    meta_PS2_CCC,           /* Tokyo Xtreme Racer DRIFT 2 */
    meta_FAG,               /* Jackie Chan - Stuntmaster */
    meta_PS2_MIHB,          /* Merged MIH+MIB */
    meta_NGC_PDT,           /* Mario Party 6 */
    meta_DC_ASD,            /* Miss Moonligh */
    meta_NAOMI_SPSD,        /* Guilty Gear X */
    meta_RSD,
    meta_PS2_ASS,           /* ASS */
    meta_SEG,               /* Eragon */
    meta_NDS_STRM_FFTA2,    /* Final Fantasy Tactics A2 */
    meta_KNON,
    meta_ZWDSP,             /* Zack and Wiki */
    meta_VGS,               /* Guitar Hero Encore - Rocks the 80s */
    meta_DCS_WAV,
    meta_SMP,
    meta_WII_SNG,           /* Excite Trucks */
    meta_MUL,
    meta_SAT_BAKA,          /* Crypt Killer */
    meta_VSF,
    meta_PS2_VSF_TTA,       /* Tiny Toon Adventures: Defenders of the Universe */
    meta_ADS_MIDWAY,
    meta_PS2_SPS,           /* Ape Escape 2 */
    meta_PS2_XA2_RRP,       /* RC Revenge Pro */
    meta_NGC_DSP_KONAMI,    /* Konami DSP header, found in various games */
    meta_UBI_CKD,           /* Ubisoft CKD RIFF header (Rayman Origins Wii) */
    meta_RAW_WAVM,
    meta_WVS,
    meta_XBOX_MATX,         /* XBOX MATX */
    meta_XMU,
    meta_XVAS,
    meta_EA_SCHL,           /* Electronic Arts SCHl with variable header */
    meta_EA_SCHL_fixed,     /* Electronic Arts SCHl with fixed header */
    meta_EA_BNK,            /* Electronic Arts BNK */
    meta_EA_1SNH,           /* Electronic Arts 1SNh/EACS */
    meta_EA_EACS,
    meta_RAW_PCM,
    meta_GENH,              /* generic header */
    meta_AIFC,              /* Audio Interchange File Format AIFF-C */
    meta_AIFF,              /* Audio Interchange File Format */
    meta_STR_SNDS,          /* .str with SNDS blocks and SHDR header */
    meta_WS_AUD,            /* Westwood Studios .aud */
    meta_WS_AUD_old,        /* Westwood Studios .aud, old style */
    meta_RIFF_WAVE,         /* RIFF, for WAVs */
    meta_RIFF_WAVE_POS,     /* .wav + .pos for looping (Ys Complete PC) */
    meta_RIFF_WAVE_labl,    /* RIFF w/ loop Markers in LIST-adtl-labl */
    meta_RIFF_WAVE_smpl,    /* RIFF w/ loop data in smpl chunk */
    meta_RIFF_WAVE_wsmp,    /* RIFF w/ loop data in wsmp chunk */
    meta_RIFF_WAVE_MWV,     /* .mwv RIFF w/ loop data in ctrl chunk pflt */
    meta_RIFX_WAVE,         /* RIFX, for big-endian WAVs */
    meta_RIFX_WAVE_smpl,    /* RIFX w/ loop data in smpl chunk */
    meta_XNB,               /* XNA Game Studio 4.0 */
    meta_PC_MXST,           /* Lego Island MxSt */
    meta_SAB,               /* Worms 4 Mayhem SAB+SOB file */
    meta_NWA,               /* Visual Art's NWA */
    meta_NWA_NWAINFOINI,    /* Visual Art's NWA w/ NWAINFO.INI for looping */
    meta_NWA_GAMEEXEINI,    /* Visual Art's NWA w/ Gameexe.ini for looping */
    meta_SAT_DVI,           /* Konami KCE Nagoya DVI (SAT games) */
    meta_DC_KCEY,           /* Konami KCE Yokohama KCEYCOMP (DC games) */
    meta_ACM,               /* InterPlay ACM header */
    meta_MUS_ACM,           /* MUS playlist of InterPlay ACM files */
    meta_DEC,               /* Falcom PC games (Xanadu Next, Gurumin) */
    meta_VS,                /* Men in Black .vs */
    meta_FFXI_BGW,          /* FFXI (PC) BGW */
    meta_FFXI_SPW,          /* FFXI (PC) SPW */
    meta_STS,
    meta_PS2_P2BT,          /* Pop'n'Music 7 Audio File */
    meta_PS2_GBTS,          /* Pop'n'Music 9 Audio File */
    meta_NGC_DSP_IADP,      /* Gamecube Interleave DSP */
    meta_PS2_TK5,           /* Tekken 5 Stream Files */
    meta_PS2_MCG,           /* Gunvari MCG Files (was name .GCM on disk) */
    meta_ZSD,               /* Dragon Booster ZSD */
    meta_REDSPARK,          /* "RedSpark" RSD (MadWorld) */
    meta_IVAUD,             /* .ivaud GTA IV */
    meta_NDS_HWAS,          /* Spider-Man 3, Tony Hawk's Downhill Jam, possibly more... */
    meta_NGC_LPS,           /* Rave Master (Groove Adventure Rave)(GC) */
    meta_NAOMI_ADPCM,       /* NAOMI/NAOMI2 ARcade games */
    meta_SD9,               /* beatmaniaIIDX16 - EMPRESS (Arcade) */
    meta_2DX9,              /* beatmaniaIIDX16 - EMPRESS (Arcade) */
    meta_PS2_VGV,           /* Rune: Viking Warlord */
    meta_GCUB,
    meta_MAXIS_XA,          /* Sim City 3000 (PC) */
    meta_NGC_SCK_DSP,       /* Scorpion King (NGC) */
    meta_CAFF,              /* iPhone .caf */
    meta_EXAKT_SC,          /* Activision EXAKT .SC (PS2) */
    meta_WII_WAS,           /* DiRT 2 (WII) */
    meta_PONA_3DO,          /* Policenauts (3DO) */
    meta_PONA_PSX,          /* Policenauts (PSX) */
    meta_XBOX_HLWAV,        /* Half Life 2 (XBOX) */
    meta_AST_MV,
    meta_AST_MMV,
    meta_DMSG,              /* Nightcaster II - Equinox (XBOX) */
    meta_NGC_DSP_AAAP,      /* Turok: Evolution (NGC), Vexx (NGC) */
    meta_PS2_WB,            /* Shooting Love. ~TRIZEAL~ */
    meta_S14,               /* raw Siren 14, 24kbit mono */
    meta_SSS,               /* raw Siren 14, 48kbit stereo */
    meta_PS2_GCM,           /* NamCollection */
    meta_PS2_SMPL,          /* Homura */
    meta_PS2_MSA,           /* Psyvariar -Complete Edition- */
    meta_PS2_VOI,           /* RAW Danger (Zettaizetsumei Toshi 2 - Itetsuita Kiokutachi) [PS2] */
    meta_P3D,               /* Prototype P3D */
    meta_PS2_TK1,           /* Tekken (NamCollection) */
    meta_NGC_RKV,           /* Legacy of Kain - Blood Omen 2 (GC) */
    meta_DSP_DDSP,          /* Various (2 dsp files stuck together */
    meta_NGC_DSP_MPDS,      /* Big Air Freestyle, Terminator 3 */
    meta_DSP_STR_IG,        /* Micro Machines, Superman Superman: Shadow of Apokolis */
    meta_EA_SWVR,           /* Future Cop L.A.P.D., Freekstyle */
    meta_PS2_B1S,           /* 7 Wonders of the ancient world */
    meta_PS2_WAD,           /* The golden Compass */
    meta_DSP_XIII,          /* XIII, possibly more (Ubisoft header???) */
    meta_DSP_CABELAS,       /* Cabelas games */
    meta_PS2_ADM,           /* Dragon Quest V (PS2) */
    meta_LPCM_SHADE,
    meta_DSP_BDSP,          /* Ah! My Goddess */
    meta_PS2_VMS,           /* Autobahn Raser - Police Madness */
    meta_XAU,               /* XPEC Entertainment (Beat Down (PS2 Xbox), Spectral Force Chronicle (PS2)) */
    meta_GH3_BAR,           /* Guitar Hero III Mobile .bar */
    meta_FFW,               /* Freedom Fighters [NGC] */
    meta_DSP_DSPW,          /* Sengoku Basara 3 [WII] */
    meta_PS2_JSTM,          /* Tantei Jinguji Saburo - Kind of Blue (PS2) */
    meta_SQEX_SCD,          /* Square-Enix SCD */
    meta_NGC_NST_DSP,       /* Animaniacs [NGC] */
    meta_BAF,               /* Bizarre Creations (Blur, James Bond) */
    meta_XVAG,              /* Ratchet & Clank Future: Quest for Booty (PS3) */
    meta_CPS,
    meta_MSF,
    meta_PS3_PAST,          /* Bakugan Battle Brawlers (PS3) */
    meta_SGXD,              /* Sony: Folklore, Genji, Tokyo Jungle (PS3), Brave Story, Kurohyo (PSP) */
    meta_WII_RAS,           /* Donkey Kong Country Returns (Wii) */
    meta_SPM,
    meta_VGS_PS,
    meta_PS2_IAB,           /* Ueki no Housoku - Taosu ze Robert Juudan!! (PS2) */
    meta_VS_STR,            /* The Bouncer */
    meta_LSF_N1NJ4N,        /* .lsf n1nj4n Fastlane Street Racing (iPhone) */
    meta_XWAV,
    meta_RAW_SNDS,
    meta_PS2_WMUS,          /* The Warriors (PS2) */
    meta_HYPERSCAN_KVAG,    /* Hyperscan KVAG/BVG */
    meta_IOS_PSND,          /* Crash Bandicoot Nitro Kart 2 (iOS) */
    meta_BOS_ADP,
    meta_QD_ADP,
    meta_EB_SFX,            /* Excitebots .sfx */
    meta_EB_SF0,            /* Excitebots .sf0 */
    meta_MTAF,
    meta_PS2_VAG1,          /* Metal Gear Solid 3 VAG1 */
    meta_PS2_VAG2,          /* Metal Gear Solid 3 VAG2 */
    meta_ALP,
    meta_WPD,               /* Shuffle! (PC) */
    meta_MN_STR,            /* Mini Ninjas (PC/PS3/WII) */
    meta_MSS,               /* Guerilla: ShellShock Nam '67 (PS2/Xbox), Killzone (PS2) */
    meta_PS2_HSF,           /* Lowrider (PS2) */
    meta_IVAG,
    meta_PS2_2PFS,          /* Konami: Mahoromatic: Moetto - KiraKira Maid-San, GANTZ (PS2) */
    meta_PS2_VBK,           /* Disney's Stitch - Experiment 626 */
    meta_OTM,               /* Otomedius (Arcade) */
    meta_CSTM,              /* Nintendo/HAL Labs NW4C Soundmaker CSTM (Century Stream) */
    meta_FSTM,              /* Nintendo/HAL Labs NW4F Soundmaker FSTM (caFe? Stream) */
    meta_IDSP_NAMCO,
    meta_KT_WIIBGM,         /* Koei Tecmo WiiBGM */
    meta_KTSS,              /* Koei Tecmo Nintendo Stream (KNS) */
    meta_MCA,               /* Capcom MCA "MADP" */
    meta_XB3D_ADX,          /* Xenoblade Chronicles 3D ADX */
    meta_HCA,               /* CRI HCA */
    meta_SVAG_SNK,
    meta_PS2_VDS_VDM,       /* Graffiti Kingdom */
    meta_FFMPEG,
    meta_FFMPEG_faulty,
    meta_CXS,
    meta_AKB,
    meta_PASX,
    meta_XMA_RIFF,
    meta_ASTB,
    meta_WWISE_RIFF,        /* Audiokinetic Wwise RIFF/RIFX */
    meta_UBI_RAKI,          /* Ubisoft RAKI header (Rayman Legends, Just Dance 2017) */
    meta_SXD,               /* Sony SXD (Gravity Rush, Freedom Wars PSV) */
    meta_OGL,               /* Shin'en Wii/WiiU (Jett Rocket (Wii), FAST Racing NEO (WiiU)) */
    meta_MC3,               /* Paradigm games (T3 PS2, MX Rider PS2, MI: Operation Surma PS2) */
    meta_GHS,
    meta_AAC_TRIACE,
    meta_MTA2,
    meta_NGC_ULW,           /* Burnout 1 (GC only) */
    meta_XA_XA30,
    meta_XA_04SW,
    meta_TXTH,              /* generic text header */
    meta_SK_AUD,            /* Silicon Knights .AUD (Eternal Darkness GC) */
    meta_AHX,
    meta_STM,               /* Angel Studios/Rockstar San Diego Games */
    meta_BINK,              /* RAD Game Tools BINK audio/video */
    meta_EA_SNU,            /* Electronic Arts SNU (Dead Space) */
    meta_AWC,               /* Rockstar AWC (GTA5, RDR) */
    meta_OPUS,              /* Nintendo Opus [Lego City Undercover (Switch)] */
    meta_RAW_AL,
    meta_PC_AST,            /* Dead Rising (PC) */
    meta_NAAC,              /* Namco AAC (3DS) */
    meta_UBI_SB,            /* Ubisoft banks */
    meta_EZW,               /* EZ2DJ (Arcade) EZWAV */
    meta_VXN,               /* Gameloft mobile games */
    meta_EA_SNR_SNS,        /* Electronic Arts SNR+SNS (Burnout Paradise) */
    meta_EA_SPS,            /* Electronic Arts SPS (Burnout Crash) */
    meta_VID1,
    meta_PC_FLX,            /* Ultima IX PC */
    meta_MOGG,              /* Harmonix Music Systems MOGG Vorbis */
    meta_OGG_VORBIS,        /* Ogg Vorbis */
    meta_OGG_SLI,           /* Ogg Vorbis file w/ companion .sli for looping */
    meta_OPUS_SLI,          /* Ogg Opus file w/ companion .sli for looping */
    meta_OGG_SFL,           /* Ogg Vorbis file w/ .sfl (RIFF SFPL) for looping */
    meta_OGG_KOVS,          /* Ogg Vorbis with header and encryption (Koei Tecmo Games) */
    meta_OGG_encrypted,     /* Ogg Vorbis with encryption */
    meta_KMA9,              /* Koei Tecmo [Nobunaga no Yabou - Souzou (Vita)] */
    meta_XWC,               /* Starbreeze games */
    meta_SQEX_SAB,          /* Square-Enix newest middleware (sound) */
    meta_SQEX_MAB,          /* Square-Enix newest middleware (music) */
    meta_WAF,               /* KID WAF [Ever 17 (PC)] */
    meta_WAVE,              /* EngineBlack games [Mighty Switch Force! (3DS)] */
    meta_WAVE_segmented,    /* EngineBlack games, segmented [Shantae and the Pirate's Curse (PC)] */
    meta_SMV,               /* Cho Aniki Zero (PSP) */
    meta_NXAP,              /* Nex Entertainment games [Time Crisis 4 (PS3), Time Crisis Razing Storm (PS3)] */
    meta_EA_WVE_AU00,       /* Electronic Arts PS movies [Future Cop - L.A.P.D. (PS), Supercross 2000 (PS)] */
    meta_EA_WVE_AD10,       /* Electronic Arts PS movies [Wing Commander 3/4 (PS)] */
    meta_STHD,              /* STHD .stx [Kakuto Chojin (Xbox)] */
    meta_MP4,               /* MP4/AAC */
    meta_PCM_SRE,           /* .PCM+SRE [Viewtiful Joe (PS2)] */
    meta_DSP_MCADPCM,       /* Skyrim (Switch) */
    meta_UBI_LYN,           /* Ubisoft LyN engine [The Adventures of Tintin (multi)] */
    meta_MSB_MSH,           /* sfx companion of MIH+MIB */
    meta_TXTP,              /* generic text playlist */
    meta_SMC_SMH,           /* Wangan Midnight (System 246) */
    meta_PPST,              /* PPST [Parappa the Rapper (PSP)] */
    meta_SPS_N1,
    meta_UBI_BAO,           /* Ubisoft BAO */
    meta_DSP_SWITCH_AUDIO,  /* Gal Gun 2 (Switch) */
    meta_H4M,               /* Hudson HVQM4 video [Resident Evil 0 (GC), Tales of Symphonia (GC)] */
    meta_ASF,               /* Argonaut ASF [Croc 2 (PC)] */
    meta_XMD,               /* Konami XMD [Silent Hill 4 (Xbox), Castlevania: Curse of Darkness (Xbox)] */
    meta_CKS,               /* Cricket Audio stream [Part Time UFO (Android), Mega Man 1-6 (Android)] */
    meta_CKB,               /* Cricket Audio bank [Fire Emblem Heroes (Android), Mega Man 1-6 (Android)] */
    meta_WV6,               /* Gorilla Systems PC games */
    meta_WAVEBATCH,         /* Firebrand Games */
    meta_HD3_BD3,           /* Sony PS3 bank */
    meta_BNK_SONY,          /* Sony Scream Tool bank */
    meta_SSCF,
    meta_DSP_VAG,           /* Penny-Punching Princess (Switch) sfx */
    meta_DSP_ITL,           /* Charinko Hero (GC) */
    meta_A2M,               /* Scooby-Doo! Unmasked (PS2) */
    meta_AHV,               /* Headhunter (PS2) */
    meta_MSV,
    meta_SDF,
    meta_SVG,               /* Hunter - The Reckoning - Wayward (PS2) */
    meta_VIS,               /* AirForce Delta Strike (PS2) */
    meta_VAI,               /* Ratatouille (GC) */
    meta_AIF_ASOBO,         /* Ratatouille (PC) */
    meta_AO,                /* Cloudphobia (PC) */
    meta_APC,               /* MegaRace 3 (PC) */
    meta_WV2,               /* Slave Zero (PC) */
    meta_XAU_KONAMI,        /* Yu-Gi-Oh - The Dawn of Destiny (Xbox) */
    meta_DERF,              /* Stupid Invaders (PC) */
    meta_SADF,
    meta_UTK,
    meta_NXA,
    meta_ADPCM_CAPCOM,
    meta_UE4OPUS,
    meta_XWMA,
    meta_VA3,               /* DDR Supernova 2 AC */
    meta_XOPUS,
    meta_VS_SQUARE,
    meta_NWAV,
    meta_XPCM,
    meta_MSF_TAMASOFT,
    meta_XPS_DAT,
    meta_ZSND,
    meta_DSP_ADPY,
    meta_DSP_ADPX,
    meta_OGG_OPUS,
    meta_IMC,
    meta_GIN,
    meta_DSF,
    meta_208,
    meta_DSP_DS2,
    meta_MUS_VC,
    meta_STRM_ABYLIGHT,
    meta_MSF_KONAMI,
    meta_XWMA_KONAMI,
    meta_9TAV,
    meta_BWAV,
    meta_RAD,
    meta_SMACKER,
    meta_MZRT,
    meta_XAVS,
    meta_PSF,
    meta_DSP_ITL_i,
    meta_IMA,
    meta_XWV_VALVE,
    meta_UBI_HX,
    meta_BMP_KONAMI,
    meta_ISB,
    meta_XSSB,
    meta_XMA_UE3,
    meta_FWSE,
    meta_FDA,
    meta_TGC,
    meta_KWB,
    meta_LRMD,
    meta_WWISE_FX,
    meta_DIVA,
    meta_IMUSE,
    meta_KTSR,
    meta_KAT,
    meta_PCM_SUCCESS,
    meta_ADP_KONAMI,
    meta_SDRH,
    meta_WADY,
    meta_DSP_SQEX,
    meta_DSP_WIIVOICE,
    meta_SBK,
    meta_DSP_WIIADPCM,
    meta_DSP_CWAC,
    meta_COMPRESSWAVE,
    meta_KTAC,
    meta_MJB_MJH,
    meta_BSNF,
    meta_TAC,
    meta_IDSP_TOSE,
    meta_DSP_KWA,
    meta_OGV_3RDEYE,
    meta_PIFF_TPCM,
    meta_WXD_WXH,
    meta_BNK_RELIC,
    meta_XSH_XSD_XSS,
    meta_PSB,
    meta_LOPU_FB,
    meta_LPCM_FB,
    meta_WBK,
    meta_WBK_NSLB,
    meta_DSP_APEX,
    meta_MPEG,
    meta_SSPF,
    meta_S3V,
    meta_ESF,
    meta_ADM3,
    meta_TT_AD,
    meta_SNDZ,
    meta_VAB,
    meta_BIGRP,
}