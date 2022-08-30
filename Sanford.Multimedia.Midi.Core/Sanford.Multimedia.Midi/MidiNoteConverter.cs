#region License

/* Copyright (c) 2006 Leslie Sanford
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

using System;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Converts a MIDI note number to its corresponding frequency.
	/// </summary>
	public sealed class MidiNoteConverter
	{
        /// <summary>
        /// The minimum value a note ID can have.
        /// </summary>
        public const int NoteIDMinValue = 0;

        /// <summary>
        /// The maximum value a note ID can have.
        /// </summary>
        public const int NoteIDMaxValue = 127;

        // Table for holding frequency values.
        private readonly static double[] NoteToFrequencyTable = new double[NoteIDMaxValue + 1];

        static MidiNoteConverter()
        {
            // The number of notes per octave.
            int notesPerOctave = 12;            

            // Reference frequency used for calculations.
            double referenceFrequency = 440;

            // The note ID of the reference frequency.
            int referenceNoteID = 69;

            double exponent;

            // Fill table with the frequencies of all MIDI notes.
            for(int i = 0; i < NoteToFrequencyTable.Length; i++)
            {
                exponent = (double)(i - referenceNoteID) / notesPerOctave;

                NoteToFrequencyTable[i] = referenceFrequency * Math.Pow(2.0, exponent);
            }
        }

        // Prevents instances of this class from being created - no need for
        // an instance to be created since this class only has static methods.
        private MidiNoteConverter()
		{
		}

        /// <summary>
        /// Converts the specified note to a frequency.
        /// </summary>
        /// <param name="noteID">
        /// The ID of the note to convert.
        /// </param>
        /// <returns>
        /// The frequency of the specified note.
        /// </returns>
        public static double NoteToFrequency(int noteID)
        {
            #region Require

            if(noteID < NoteIDMinValue || noteID > NoteIDMaxValue)
            {
                throw new ArgumentOutOfRangeException("Note ID out of range.");
            }

            #endregion

            return NoteToFrequencyTable[noteID];
        }

        /// <summary>
        /// Converts the specified frequency to a note.
        /// </summary>
        /// <param name="frequency">
        /// The frequency to convert.
        /// </param>
        /// <returns>
        /// The ID of the note closest to the specified frequency.
        /// </returns>
        public static int FrequencyToNote(double frequency)
        {
            int noteID = 0;
            bool found = false;

            // Search for the note with a frequency near the specified frequency.
            for(int i = 0; i < NoteIDMaxValue && !found; i++)
            {
                noteID = i;

                // If the specified frequency is less than the frequency of 
                // the next note.
                if(frequency < NoteToFrequency(noteID + 1))
                {
                    // Indicate that the note ID for the specified frequency 
                    // has been found.
                    found = true;
                }
            }

            // If the note is not the first or last note, narrow the results.
            if(noteID > 0 && noteID < NoteIDMaxValue)
            {
                // Get the frequency of the previous note.
                double previousFrequncy = NoteToFrequency(noteID - 1);
                // Get the frequency of the next note.
                double nextFrequency = NoteToFrequency(noteID + 1);

                // If the next note is closer in frequency than the previous note.
                if(nextFrequency - frequency < frequency - previousFrequncy)
                {
                    // Move to the next note.
                    noteID++;
                }
            }

            return noteID;
        }
    }
}
