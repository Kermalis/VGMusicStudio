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

using System;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Provides basic functionality for generating tick events with pulses per 
    /// quarter note resolution.
	/// </summary>
	public abstract class PpqnClock : IClock
    {
        #region PpqnClock Members

        #region Fields

        /// <summary>
        /// The default tempo in microseconds: 120bpm.
        /// </summary>
        public const int DefaultTempo = 500000;

        /// <summary>
        /// The minimum pulses per quarter note value.
        /// </summary>
        public const int PpqnMinValue = 24;

        // The number of microseconds per millisecond.
        private const int MicrosecondsPerMillisecond = 1000;

        // The pulses per quarter note value.
        private int ppqn = PpqnMinValue;

        // The tempo in microseconds.
        private int tempo = DefaultTempo;

        // The product of the timer period, the pulses per quarter note, and
        // the number of microseconds per millisecond.
        private int periodResolution;

        // The number of ticks per MIDI clock.
        private int ticksPerClock;

        // The running fractional tick count.
        private int fractionalTicks = 0;

        // The timer period.
        private readonly int timerPeriod;

        /// <summary>
        /// Indicates whether the clock is running.
        /// </summary>
        protected bool running = false;

        #endregion

        #region Construction

        /// <summary>
        /// The PpqnClock determines how many ticks and timer period is used for the PPQN format.
        /// </summary>
        /// <param name="timerPeriod">
        /// The timerPeriod integer determines the amount of time there is.
        /// </param>
        protected PpqnClock(int timerPeriod)
        {
            #region Require

            if(timerPeriod < 1)
            {
                throw new ArgumentOutOfRangeException("timerPeriod", timerPeriod,
                    "Timer period cannot be less than one.");
            }

            #endregion

            this.timerPeriod = timerPeriod;

            CalculatePeriodResolution();
            CalculateTicksPerClock();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the tempos per beat.
        /// </summary>
        protected int GetTempo()
        {
            return tempo;
        }

        /// <summary>
        /// Sets the tempos to be used per beat.
        /// </summary>
        protected void SetTempo(int tempo)
        {
            #region Require

            if(tempo < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "Tempo out of range.");
            }

            #endregion

            this.tempo = tempo;
        }

        /// <summary>
        /// Resets the amount of ticks.
        /// </summary>
        protected void Reset()
        {
            fractionalTicks = 0;
        }

        /// <summary>
        /// Generates the amount of ticks.
        /// </summary>
        protected int GenerateTicks()
        {
            int ticks = (fractionalTicks + periodResolution) / tempo;
            fractionalTicks += periodResolution - ticks * tempo;

            return ticks;
        }

        /// <summary>
        /// Calculates the amount of time that the timer will have.
        /// </summary>
        private void CalculatePeriodResolution()
        {
            periodResolution = ppqn * timerPeriod * MicrosecondsPerMillisecond;
        }

        /// <summary>
        /// Calculates the amount of ticks per clock.
        /// </summary>
        private void CalculateTicksPerClock()
        {
            ticksPerClock = ppqn / PpqnMinValue;
        }

        /// <summary>
        /// An event that handles the ticks.
        /// </summary>
        protected virtual void OnTick(EventArgs e)
        {
            EventHandler handler = Tick;

            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// An event that starts the PPQN Clock.
        /// </summary>
        protected virtual void OnStarted(EventArgs e)
        {
            EventHandler handler = Started;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// An event that stops the PPQN Clock.
        /// </summary>
        protected virtual void OnStopped(EventArgs e)
        {
            EventHandler handler = Stopped;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// An event that continues the PPQN Clock.
        /// </summary>
        protected virtual void OnContinued(EventArgs e)
        {
            EventHandler handler = Continued;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// An integer that gets and sets the PPQN Clock value.
        /// </summary>
        public int Ppqn
        {
            get
            {
                return ppqn;
            }
            set
            {
                #region Require

                if(value < PpqnMinValue)
                {
                    throw new ArgumentOutOfRangeException("Ppqn", value,
                        "Pulses per quarter note is smaller than 24.");
                }

                #endregion

                ppqn = value;

                CalculatePeriodResolution();
                CalculateTicksPerClock();
            }
        }

        /// <summary>
        /// An abstract integer that gets the amount of ticks.
        /// </summary>
        public abstract int Ticks
        {
            get;
        }

        /// <summary>
        /// An integer that determines the ticks per clock.
        /// </summary>
        /// <returns>
        /// The amount of ticks per clock.
        /// </returns>
        public int TicksPerClock
        {
            get
            {
                return ticksPerClock;
            }
        }

        #endregion

        #endregion

        #region IClock Members

        /// <summary>
        /// This event occurs when PPQN Clock generates a tick.
        /// </summary>
        public event System.EventHandler Tick;

        /// <summary>
        /// This event occurs when PPQN Clock is started and starts generating ticks.
        /// </summary>
        public event System.EventHandler Started;

        /// <summary>
        /// This event occurs when PPQN Clock continues generating ticks.
        /// </summary>
        public event System.EventHandler Continued;

        /// <summary>
        /// This event occurs when PPQN Clock has stopped.
        /// </summary>
        public event System.EventHandler Stopped;

        /// <summary>
        /// Checks if PPQN Clock is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return running;
            }
        }

        #endregion
    }
}
