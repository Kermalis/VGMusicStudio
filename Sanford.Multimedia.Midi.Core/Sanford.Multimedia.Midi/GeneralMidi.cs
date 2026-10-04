#region License

/* Copyright (c) 2005 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Defines constants representing the General MIDI instrument set.
    /// </summary>
    public enum GeneralMidiInstrument
    {
        /// <summary>
        /// Instrument sample: Acoustic Grand Piano.
        /// </summary>
        AcousticGrandPiano,

        /// <summary>
        /// Instrument sample: Bright Acoustic Piano.
        /// </summary>
        BrightAcousticPiano,

        /// <summary>
        /// Instrument sample: Electric Grand Piano.
        /// </summary>
        ElectricGrandPiano,

        /// <summary>
        /// Instrument sample: Honky Tonk Piano.
        /// </summary>
        HonkyTonkPiano,

        /// <summary>
        /// Instrument sample: Electric Piano 1.
        /// </summary>
        ElectricPiano1,

        /// <summary>
        /// Instrument sample: Electric Piano 2.
        /// </summary>
        ElectricPiano2,

        /// <summary>
        /// Instrument sample: Harpsichord.
        /// </summary>
        Harpsichord,

        /// <summary>
        /// Instrument sample: Clavinet.
        /// </summary>
        Clavinet,

        /// <summary>
        /// Instrument sample: Celesta.
        /// </summary>
        Celesta,

        /// <summary>
        /// Instrument sample: Glockenspiel.
        /// </summary>
        Glockenspiel,

        /// <summary>
        /// Instrument sample: Music Box.
        /// </summary>
        MusicBox,

        /// <summary>
        /// Instrument sample: Vibraphone.
        /// </summary>
        Vibraphone,

        /// <summary>
        /// Instrument sample: Marimba.
        /// </summary>
        Marimba,

        /// <summary>
        /// Instrument sample: Xylophone.
        /// </summary>
        Xylophone,

        /// <summary>
        /// Instrument sample: Tubular Bells.
        /// </summary>
        TubularBells,

        /// <summary>
        /// Instrument sample: Dulcimer.
        /// </summary>
        Dulcimer,

        /// <summary>
        /// Instrument sample: Drawbar Organ.
        /// </summary>
        DrawbarOrgan,

        /// <summary>
        /// Instrument sample: Percussive Organ.
        /// </summary>
        PercussiveOrgan,

        /// <summary>
        /// Instrument sample: Rock Organ.
        /// </summary>
        RockOrgan,

        /// <summary>
        /// Instrument sample: Church Organ.
        /// </summary>
        ChurchOrgan,

        /// <summary>
        /// Instrument sample: Reed Organ.
        /// </summary>
        ReedOrgan,

        /// <summary>
        /// Instrument sample: Accordion.
        /// </summary>
        Accordion,

        /// <summary>
        /// Instrument sample: Harmonica.
        /// </summary>
        Harmonica,

        /// <summary>
        /// Instrument sample: Tango Accordion.
        /// </summary>
        TangoAccordion,

        /// <summary>
        /// Instrument sample: Acoustic Guitar Nylon.
        /// </summary>
        AcousticGuitarNylon,

        /// <summary>
        /// Instrument sample: Acoustic Guitar Steel.
        /// </summary>
        AcousticGuitarSteel,

        /// <summary>
        /// Instrument sample: Electric Guitar Jazz.
        /// </summary>
        ElectricGuitarJazz,

        /// <summary>
        /// Instrument sample: Electric Guitar Clean.
        /// </summary>
        ElectricGuitarClean,

        /// <summary>
        /// Instrument sample: Electric Guitar Muted.
        /// </summary>
        ElectricGuitarMuted,

        /// <summary>
        /// Instrument sample: Overdriven Guitar.
        /// </summary>
        OverdrivenGuitar,

        /// <summary>
        /// Instrument sample: Distortion Guitar.
        /// </summary>
        DistortionGuitar,

        /// <summary>
        /// Instrument sample: Guitar Harmonics.
        /// </summary>
        GuitarHarmonics,

        /// <summary>
        /// Instrument sample: Acoustic Bass.
        /// </summary>
        AcousticBass,

        /// <summary>
        /// Instrument sample: Electric Bass Finger.
        /// </summary>
        ElectricBassFinger,

        /// <summary>
        /// Instrument sample: Electric Bass Pick.
        /// </summary>
        ElectricBassPick,

        /// <summary>
        /// Instrument sample: Fretless Bass.
        /// </summary>
        FretlessBass,

        /// <summary>
        /// Instrument sample: Slap Bass 1.
        /// </summary>
        SlapBass1,

        /// <summary>
        /// Instrument sample: Slap Bass 2.
        /// </summary>
        SlapBass2,

        /// <summary>
        /// Instrument sample: Synth Bass 1.
        /// </summary>
        SynthBass1,

        /// <summary>
        /// Instrument sample: Synth Bass 2.
        /// </summary>
        SynthBass2,

        /// <summary>
        /// Instrument sample: Violin.
        /// </summary>
        Violin,

        /// <summary>
        /// Instrument sample: Viola.
        /// </summary>
        Viola,

        /// <summary>
        /// Instrument sample: Cello.
        /// </summary>
        Cello,

        /// <summary>
        /// Instrument sample: Contrabass.
        /// </summary>
        Contrabass,

        /// <summary>
        /// Instrument sample: Tremolo Strings.
        /// </summary>
        TremoloStrings,

        /// <summary>
        /// Instrument sample: Pizzicato Strings.
        /// </summary>
        PizzicatoStrings,

        /// <summary>
        /// Instrument sample: Orchestral Harp.
        /// </summary>
        OrchestralHarp,

        /// <summary>
        /// Instrument sample: Timpani.
        /// </summary>
        Timpani,

        /// <summary>
        /// Instrument sample: String Ensemble 1.
        /// </summary>
        StringEnsemble1,

        /// <summary>
        /// Instrument sample: String Ensemble 2.
        /// </summary>
        StringEnsemble2,

        /// <summary>
        /// Instrument sample: Synth Strings 1.
        /// </summary>
        SynthStrings1,

        /// <summary>
        /// Instrument sample: Synth Strings 2.
        /// </summary>
        SynthStrings2,

        /// <summary>
        /// Instrument sample: Aah (Choir).
        /// </summary>
        ChoirAahs,

        /// <summary>
        /// Instrument sample: Ooh (Voice).
        /// </summary>
        VoiceOohs,

        /// <summary>
        /// Instrument sample: Synth Voice.
        /// </summary>
        SynthVoice,

        /// <summary>
        /// Instrument sample: Orchestra Hit.
        /// </summary>
        OrchestraHit,

        /// <summary>
        /// Instrument sample: Trumpet.
        /// </summary>
        Trumpet,

        /// <summary>
        /// Instrument sample: Trombone.
        /// </summary>
        Trombone,

        /// <summary>
        /// Instrument sample: Tuba.
        /// </summary>
        Tuba,

        /// <summary>
        /// Instrument sample: Muted Trumpet.
        /// </summary>
        MutedTrumpet,

        /// <summary>
        /// Instrument sample: French Horn.
        /// </summary>
        FrenchHorn,

        /// <summary>
        /// Instrument sample: Brass Section.
        /// </summary>
        BrassSection,

        /// <summary>
        /// Instrument sample: Synth Brass 1.
        /// </summary>
        SynthBrass1,

        /// <summary>
        /// Instrument sample: Synth Brass 2.
        /// </summary>
        SynthBrass2,

        /// <summary>
        /// Instrument sample: Soprano Saxophone.
        /// </summary>
        SopranoSax,

        /// <summary>
        /// Instrument sample: Alto Saxophone.
        /// </summary>
        AltoSax,

        /// <summary>
        /// Instrument sample: Tenor Saxophone.
        /// </summary>
        TenorSax,

        /// <summary>
        /// Instrument sample: Baritone Saxophone.
        /// </summary>
        BaritoneSax,

        /// <summary>
        /// Instrument sample: Oboe.
        /// </summary>
        Oboe,

        /// <summary>
        /// Instrument sample: English Horn.
        /// </summary>
        EnglishHorn,

        /// <summary>
        /// Instrument sample: Bassoon.
        /// </summary>
        Bassoon,

        /// <summary>
        /// Instrument sample: Clarinet.
        /// </summary>
        Clarinet,

        /// <summary>
        /// Instrument sample: Piccolo.
        /// </summary>
        Piccolo,

        /// <summary>
        /// Instrument sample: Flute.
        /// </summary>
        Flute,

        /// <summary>
        /// Instrument sample: Recorder.
        /// </summary>
        Recorder,

        /// <summary>
        /// Instrument sample: Pan Flute.
        /// </summary>
        PanFlute,

        /// <summary>
        /// Instrument sample: Blown Bottle.
        /// </summary>
        BlownBottle,

        /// <summary>
        /// Instrument sample: Shakuhachi.
        /// </summary>
        Shakuhachi,

        /// <summary>
        /// Instrument sample: Whistle.
        /// </summary>
        Whistle,

        /// <summary>
        /// Instrument sample: Ocarina.
        /// </summary>
        Ocarina,

        /// <summary>
        /// Instrument sample: Lead 1 (Square).
        /// </summary>
        Lead1Square,

        /// <summary>
        /// Instrument sample: Lead 2 (Sawtooth).
        /// </summary>
        Lead2Sawtooth,

        /// <summary>
        /// Instrument sample: Lead 3 (Calliope).
        /// </summary>
        Lead3Calliope,

        /// <summary>
        /// Instrument sample: Lead 4 (Chiff).
        /// </summary>
        Lead4Chiff,

        /// <summary>
        /// Instrument sample: Lead 5 (Charang).
        /// </summary>
        Lead5Charang,

        /// <summary>
        /// Instrument sample: Lead 6 (Voice).
        /// </summary>
        Lead6Voice,

        /// <summary>
        /// Instrument sample: Lead 7 (Fifths).
        /// </summary>
        Lead7Fifths,

        /// <summary>
        /// Instrument sample: Lead 8 (Bass And Lead).
        /// </summary>
        Lead8BassAndLead,

        /// <summary>
        /// Instrument sample: Pad 1 (New Age).
        /// </summary>
        Pad1NewAge,

        /// <summary>
        /// Instrument sample: Pad 2 (Warm).
        /// </summary>
        Pad2Warm,

        /// <summary>
        /// Instrument sample: Pad 3 (Polysynth).
        /// </summary>
        Pad3Polysynth,

        /// <summary>
        /// Instrument sample: Pad 4 (Choir).
        /// </summary>
        Pad4Choir,

        /// <summary>
        /// Instrument sample: Pad 5 (Bowed).
        /// </summary>
        Pad5Bowed,

        /// <summary>
        /// Instrument sample: Pad 6 (Metallic).
        /// </summary>
        Pad6Metallic,

        /// <summary>
        /// Instrument sample: Pad 7 (Halo).
        /// </summary>
        Pad7Halo,

        /// <summary>
        /// Instrument sample: Pad 8 (Sweep).
        /// </summary>
        Pad8Sweep,

        /// <summary>
        /// Instrument sample: Fx 1 (Rain).
        /// </summary>
        Fx1Rain,

        /// <summary>
        /// Instrument sample: Fx 2 (Soundtrack).
        /// </summary>
        Fx2Soundtrack,

        /// <summary>
        /// Instrument sample: Fx 3 (Crystal).
        /// </summary>
        Fx3Crystal,

        /// <summary>
        /// Instrument sample: Fx 4 (Atmosphere).
        /// </summary>
        Fx4Atmosphere,

        /// <summary>
        /// Instrument sample: Fx 5 (Brightness).
        /// </summary>
        Fx5Brightness,

        /// <summary>
        /// Instrument sample: Fx 6 (Goblins).
        /// </summary>
        Fx6Goblins,

        /// <summary>
        /// Instrument sample: Fx 7 (Echoes).
        /// </summary>
        Fx7Echoes,

        /// <summary>
        /// Instrument sample: Fx 8 (Sci-Fi).
        /// </summary>
        Fx8SciFi,

        /// <summary>
        /// Instrument sample: Sitar.
        /// </summary>
        Sitar,

        /// <summary>
        /// Instrument sample: Banjo.
        /// </summary>
        Banjo,

        /// <summary>
        /// Instrument sample: Shamisen.
        /// </summary>
        Shamisen,

        /// <summary>
        /// Instrument sample: Koto.
        /// </summary>
        Koto,

        /// <summary>
        /// Instrument sample: Kalimba.
        /// </summary>
        Kalimba,

        /// <summary>
        /// Instrument sample: Bag Pipe.
        /// </summary>
        BagPipe,

        /// <summary>
        /// Instrument sample: Fiddle.
        /// </summary>
        Fiddle,

        /// <summary>
        /// Instrument sample: Shanai.
        /// </summary>
        Shanai,

        /// <summary>
        /// Instrument sample: Tinkle Bell.
        /// </summary>
        TinkleBell,

        /// <summary>
        /// Instrument sample: Agogo.
        /// </summary>
        Agogo,

        /// <summary>
        /// Instrument sample: Steel Drums.
        /// </summary>
        SteelDrums,

        /// <summary>
        /// Instrument sample: Woodblock.
        /// </summary>
        Woodblock,

        /// <summary>
        /// Instrument sample: Taiko Drum.
        /// </summary>
        TaikoDrum,

        /// <summary>
        /// Instrument sample: Melodic Tom.
        /// </summary>
        MelodicTom,

        /// <summary>
        /// Instrument sample: Synth Drum.
        /// </summary>
        SynthDrum,

        /// <summary>
        /// Instrument sample: Reverse Cymbal.
        /// </summary>
        ReverseCymbal,

        /// <summary>
        /// Instrument sample: Guitar Fret Noise.
        /// </summary>
        GuitarFretNoise,

        /// <summary>
        /// Instrument sample: Breath Noise.
        /// </summary>
        BreathNoise,

        /// <summary>
        /// Instrument sample: Seashore.
        /// </summary>
        Seashore,

        /// <summary>
        /// Instrument sample: Bird Tweet.
        /// </summary>
        BirdTweet,

        /// <summary>
        /// Instrument sample: Telephone Ring.
        /// </summary>
        TelephoneRing,

        /// <summary>
        /// Instrument sample: Helicopter.
        /// </summary>
        Helicopter,

        /// <summary>
        /// Instrument sample: Applause.
        /// </summary>
        Applause,

        /// <summary>
        /// Instrument sample: Gunshot.
        /// </summary>
        Gunshot
    }
}