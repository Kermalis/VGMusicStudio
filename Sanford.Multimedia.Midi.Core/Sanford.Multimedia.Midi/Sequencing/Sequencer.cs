using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// This sequencer class allows for the sequencing of sequences.
    /// </summary>
    public class Sequencer : IComponent
    {
        private Sequence sequence = null;

        private List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>();

        private MessageDispatcher dispatcher = new MessageDispatcher();

        private ChannelChaser chaser = new ChannelChaser();

        private ChannelStopper stopper = new ChannelStopper();

        private MidiInternalClock clock = new MidiInternalClock();

        private int tracksPlayingCount;

        private readonly object lockObject = new object();

        private bool playing = false;

        private bool disposed = false;

        private ISite site = null;

        #region Events

        /// <summary>
        /// Handles the event when the sequencer has finished playing the sequence.
        /// </summary>
        public event EventHandler PlayingCompleted;

        /// <summary>
        /// Handles the event when a channel message is displayed when a sequence is played.
        /// </summary>
        public event EventHandler<ChannelMessageEventArgs> ChannelMessagePlayed
        {
            add
            {
                dispatcher.ChannelMessageDispatched += value;
            }
            remove
            {
                dispatcher.ChannelMessageDispatched -= value;
            }
        }

        /// <summary>
        /// Handles the event when a system ex message is displayed when a sequence is played.
        /// </summary>
        public event EventHandler<SysExMessageEventArgs> SysExMessagePlayed
        {
            add
            {
                dispatcher.SysExMessageDispatched += value;
            }
            remove
            {
                dispatcher.SysExMessageDispatched -= value;
            }
        }

        /// <summary>
        /// Handles the event when a metadata message is displayed when a sequence is played.
        /// </summary>
        public event EventHandler<MetaMessageEventArgs> MetaMessagePlayed
        {
            add
            {
                dispatcher.MetaMessageDispatched += value;
            }
            remove
            {
                dispatcher.MetaMessageDispatched -= value;
            }
        }

        /// <summary>
        /// Handles the chased event in the sequencer.
        /// </summary>
        public event EventHandler<ChasedEventArgs> Chased
        {
            add
            {
                chaser.Chased += value;
            }
            remove
            {
                chaser.Chased -= value;
            }
        }

        /// <summary>
        /// Handles the event when sequencer stops playing.
        /// </summary>
        public event EventHandler<StoppedEventArgs> Stopped
        {
            add
            {
                stopper.Stopped += value;
            }
            remove
            {
                stopper.Stopped -= value;
            }
        }

        #endregion

        /// <summary>
        /// The main sequencer function.
        /// </summary>
        public Sequencer()
        {
            dispatcher.MetaMessageDispatched += delegate(object sender, MetaMessageEventArgs e)
            {
                if(e.Message.MetaType == MetaType.EndOfTrack)
                {
                    tracksPlayingCount--;

                    if(tracksPlayingCount == 0)
                    {
                        Stop();

                        OnPlayingCompleted(EventArgs.Empty);
                    }
                }
                else
                {
                    clock.Process(e.Message);
                }
            };

            dispatcher.ChannelMessageDispatched += delegate(object sender, ChannelMessageEventArgs e)
            {
                stopper.Process(e.Message);
            };

            clock.Tick += delegate(object sender, EventArgs e)
            {
                lock(lockObject)
                {
                    if(!playing)
                    {
                        return;
                    }

                    foreach(IEnumerator<int> enumerator in enumerators)
                    {
                        enumerator.MoveNext();
                    }
                }
            };
        }

        /// <summary>
        /// The function in which checks if the sequencer has been disposed.
        /// </summary>
        ~Sequencer()
        {
            Dispose(false);
        }

        /// <summary>
        /// The method for disposing the sequencer when the application is closed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Stop();

                    clock.Dispose();

                    disposed = true;

                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Starts the sequencer.
        /// </summary>
        public void Start()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion           

            lock(lockObject)
            {
                Stop();

                Position = 0;

                Continue();
            }
        }

        /// <summary>
        /// Continues the sequencer.
        /// </summary>
        public void Continue()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            #region Guard

            if(Sequence == null)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                Stop();

                enumerators.Clear();

                foreach(Track t in Sequence)
                {
                    enumerators.Add(t.TickIterator(Position, chaser, dispatcher).GetEnumerator());
                }

                tracksPlayingCount = Sequence.Count;

                playing = true;
                clock.Ppqn = sequence.Division;
                clock.Continue();
            }
        }

        /// <summary>
        /// Stops the sequencer.
        /// </summary>
        public void Stop()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                #region Guard

                if(!playing)
                {
                    return;
                }

                #endregion

                playing = false;
                clock.Stop();
                stopper.AllSoundOff();
            }
        }

        /// <summary>
        /// Handles the event for when the sequencer is finished playing.
        /// </summary>
        protected virtual void OnPlayingCompleted(EventArgs e)
        {
            EventHandler handler = PlayingCompleted;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handles the event for when the sequencer is disposed.
        /// </summary>
        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// The sequencer's playing position of the sequence.
        /// </summary>
        public int Position
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                #endregion

                return clock.Ticks;
            }
            set
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                else if(value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                #endregion

                bool wasPlaying;

                lock(lockObject)
                {
                    wasPlaying = playing;

                    Stop();

                    clock.SetTicks(value);
                }

                lock(lockObject)
                {
                    if(wasPlaying)
                    {
                        Continue();
                    }
                }
            }
        }

        /// <summary>
        /// The loaded sequence that represents a series of tracks.
        /// </summary>
        public Sequence Sequence
        {
            get
            {
                return sequence;
            }
            set
            {
                #region Require

                if(value == null)
                {
                    throw new ArgumentNullException();
                }
                else if(value.SequenceType == SequenceType.Smpte)
                {
                    throw new NotSupportedException();
                }

                #endregion

                lock(lockObject)
                {
                    Stop();
                    sequence = value;
                }
            }
        }

        #region IComponent Members

        /// <summary>
        /// Handles the disposed event.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Gets the site and sets the site with a value.
        /// </summary>
        public ISite Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// The dispose function for when the application is closed.
        /// </summary>
        public void Dispose()
        {
            #region Guard

            if(disposed)
            {
                return;
            }

            #endregion

            Dispose(true);
        }

        #endregion
    }
}
