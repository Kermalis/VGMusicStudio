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
using System.Runtime.InteropServices;

namespace Sanford.Multimedia.Midi
   {
   public partial class InputDevice
      {
      // Represents the method that handles messages from Windows.
      private delegate void MidiInProc(IntPtr handle, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

      #region Win32 MIDI Input Functions and Constants

      [DllImport("winmm.dll")]
      private static extern int midiInOpen(out IntPtr handle, int deviceID,
          MidiInProc proc, IntPtr instance, int flags);

      [DllImport("winmm.dll")]
      private static extern int midiInClose(IntPtr handle);

      [DllImport("winmm.dll")]
      private static extern int midiInStart(IntPtr handle);

      [DllImport("winmm.dll")]
      private static extern int midiInStop(IntPtr handle);

      [DllImport("winmm.dll")]
      private static extern int midiInReset(IntPtr handle);

      [DllImport("winmm.dll")]
      private static extern int midiInPrepareHeader(IntPtr handle,
          IntPtr headerPtr, int sizeOfMidiHeader);

      [DllImport("winmm.dll")]
      private static extern int midiInUnprepareHeader(IntPtr handle,
          IntPtr headerPtr, int sizeOfMidiHeader);

      [DllImport("winmm.dll")]
      private static extern int midiInAddBuffer(IntPtr handle,
          IntPtr headerPtr, int sizeOfMidiHeader);

      [DllImport("winmm.dll")]
      private static extern int midiInGetDevCaps(IntPtr deviceID,
          ref MidiInCaps caps, int sizeOfMidiInCaps);

      [DllImport("winmm.dll")]
      private static extern int midiInGetNumDevs();

      private const int MIDI_IO_STATUS = 0x00000020;

      private const int MIM_OPEN = 0x3C1;
      private const int MIM_CLOSE = 0x3C2;
      private const int MIM_DATA = 0x3C3;
      private const int MIM_LONGDATA = 0x3C4;
      private const int MIM_ERROR = 0x3C5;
      private const int MIM_LONGERROR = 0x3C6;
      private const int MIM_MOREDATA = 0x3CC;
      private const int MHDR_DONE = 0x00000001;

      #endregion
      }
   }
