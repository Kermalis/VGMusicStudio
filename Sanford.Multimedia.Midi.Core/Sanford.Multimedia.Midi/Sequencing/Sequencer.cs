using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sanford.Multimedia.Midi
{
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

        public event EventHandler PlayingCompleted;

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

        ~Sequencer()
        {
            Dispose(false);
        }

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

        protected virtual void OnPlayingCompleted(EventArgs e)
        {
            EventHandler handler = PlayingCompleted;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if(handler != null)
            {
                handler(this, e);
            }
        }

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

        public event EventHandler Disposed;

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
