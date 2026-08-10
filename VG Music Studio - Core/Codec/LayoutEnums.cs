namespace Kermalis.VGMusicStudio.Core.Codec;

/* This code has been copied directly from vgmstream.h in VGMStream's repository    *
 * and modified into C# code to work with VGMS. Link to its repository can be       *
 * found here: https://github.com/vgmstream/vgmstream                               */
public enum LayoutType
{
    /* generic */
    layout_none,            /* straight data */

    /* interleave */
    layout_interleave,      /* equal interleave throughout the stream */

    /* headered blocks */
    layout_blocked_ast,
    layout_blocked_halpst,
    layout_blocked_xa,
    layout_blocked_ea_schl,
    layout_blocked_ea_1snh,
    layout_blocked_caf,
    layout_blocked_wsi,
    layout_blocked_str_snds,
    layout_blocked_ws_aud,
    layout_blocked_matx,
    layout_blocked_dec,
    layout_blocked_xvas,
    layout_blocked_vs,
    layout_blocked_mul,
    layout_blocked_gsb,
    layout_blocked_thp,
    layout_blocked_filp,
    layout_blocked_ea_swvr,
    layout_blocked_adm,
    layout_blocked_bdsp,
    layout_blocked_mxch,
    layout_blocked_ivaud,   /* GTA IV .ivaud blocks */
    layout_blocked_ps2_iab,
    layout_blocked_vs_str,
    layout_blocked_rws,
    layout_blocked_hwas,
    layout_blocked_ea_sns,  /* newest Electronic Arts blocks, found in SNS/SNU/SPS/etc formats */
    layout_blocked_awc,     /* Rockstar AWC */
    layout_blocked_vgs,     /* Guitar Hero II (PS2) */
    layout_blocked_xwav,
    layout_blocked_xvag_subsong, /* XVAG subsongs [God of War III (PS4)] */
    layout_blocked_ea_wve_au00, /* EA WVE au00 blocks */
    layout_blocked_ea_wve_ad10, /* EA WVE Ad10 blocks */
    layout_blocked_sthd, /* Dream Factory STHD */
    layout_blocked_h4m, /* H4M video */
    layout_blocked_xa_aiff, /* XA in AIFF files [Crusader: No Remorse (SAT), Road Rash (3DO)] */
    layout_blocked_vs_square,
    layout_blocked_vid1,
    layout_blocked_ubi_sce,
    layout_blocked_tt_ad,

    /* otherwise odd */
    layout_segmented,       /* song divided in segments (song sections) */
    layout_layered,         /* song divided in layers (song channels) */
}