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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Represents a collection of Tracks.
    /// </summary>
    public sealed class Sequence : IComponent, ICollection<Track>
    {
        #region Sequence Members

        #region Fields

        // The collection of Tracks for the Sequence.
        private List<Track> tracks = new List<Track>();

        // The Sequence's MIDI file properties.
        private MidiFileProperties properties = new MidiFileProperties();

        private BackgroundWorker loadWorker = new BackgroundWorker();

        private BackgroundWorker saveWorker = new BackgroundWorker();

        private ISite site = null;

        private bool disposed = false;

        #endregion

        #region Events

        public event EventHandler<AsyncCompletedEventArgs> LoadCompleted;

        public event ProgressChangedEventHandler LoadProgressChanged;

        public event EventHandler<AsyncCompletedEventArgs> SaveCompleted;

        public event ProgressChangedEventHandler SaveProgressChanged;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Sequence class.
        /// </summary>
        public Sequence()
        {
            InitializeBackgroundWorkers();
        }        

        /// <summary>
        /// Initializes a new instance of the Sequence class with the specified division.
        /// </summary>
        /// <param name="division">
        /// The Sequence's division value.
        /// </param>
        public Sequence(int division)
        {
            properties.Division = division;
            properties.Format = 1;

            InitializeBackgroundWorkers();
        }

        /// <summary>
        /// Initializes a new instance of the Sequence class with the specified
        /// file name of the MIDI file to load.
        /// </summary>
        /// <param name="fileName">
        /// The name of the MIDI file to load.
        /// </param>
        public Sequence(string fileName)
        {
            InitializeBackgroundWorkers();

            Load(fileName);
        }
        
        /// <summary>
        /// Initializes a new instance of the Sequence class with the specified
        /// file stream of the MIDI file to load.
        /// </summary>
        /// <param name="fileStream">
        /// The stream of the MIDI file to load.
        /// </param>
        public Sequence(Stream fileStream)
        {
            InitializeBackgroundWorkers();

            Load(fileStream);
        }
        
        private void InitializeBackgroundWorkers()
        {
            loadWorker.DoWork += new DoWorkEventHandler(LoadDoWork);
            loadWorker.ProgressChanged += new ProgressChangedEventHandler(OnLoadProgressChanged);
            loadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnLoadCompleted);
            loadWorker.WorkerReportsProgress = true;

            saveWorker.DoWork += new DoWorkEventHandler(SaveDoWork);
            saveWorker.ProgressChanged += new ProgressChangedEventHandler(OnSaveProgressChanged);
            saveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnSaveCompleted);
            saveWorker.WorkerReportsProgress = true;
        }        

        #endregion

        #region Methods

        /// <summary>
        /// Loads a MIDI file into the Sequence.
        /// </summary>
        /// <param name="fileName">
        /// The MIDI file's name.
        /// </param>
        public void Load(string fileName)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }
            else if(IsBusy)
            {
                throw new InvalidOperationException();
            }
            else if(fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion                        

            FileStream stream = new FileStream(fileName, FileMode.Open,
                FileAccess.Read, FileShare.Read);

            using(stream)
            {
                MidiFileProperties newProperties = new MidiFileProperties();
                TrackReader reader = new TrackReader();
                List<Track> newTracks = new List<Track>();

                newProperties.Read(stream);

                for(int i = 0; i < newProperties.TrackCount; i++)
                {
                    reader.Read(stream);
                    newTracks.Add(reader.Track);
                }

                properties = newProperties;
                tracks = newTracks;
            }

            #region Ensure

            Debug.Assert(Count == properties.TrackCount);

            #endregion
        }

        /// <summary>
        /// Loads a MIDI stream into the Sequence.
        /// </summary>
        /// <param name="fileStream">
        /// The MIDI file's stream.
        /// </param>
        public void Load(Stream fileStream)
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }
            else if (IsBusy)
            {
                throw new InvalidOperationException();
            }
            else if (fileStream == null)
            {
                throw new ArgumentNullException("fileStream");
            }

            #endregion                        

            using (fileStream)
            {
                MidiFileProperties newProperties = new MidiFileProperties();
                TrackReader reader = new TrackReader();
                List<Track> newTracks = new List<Track>();

                newProperties.Read(fileStream);

                for (int i = 0; i < newProperties.TrackCount; i++)
                {
                    reader.Read(fileStream);
                    newTracks.Add(reader.Track);
                }

                properties = newProperties;
                tracks = newTracks;
            }

            #region Ensure

            Debug.Assert(Count == properties.TrackCount);

            #endregion
        }

        public void LoadAsync(string fileName)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }
            else if(IsBusy)
            {
                throw new InvalidOperationException();
            }
            else if(fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            loadWorker.RunWorkerAsync(fileName);
        }

        public void LoadAsyncCancel()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            loadWorker.CancelAsync();
        }

        /// <summary>
        /// Saves the Sequence as a MIDI file.
        /// </summary>
        /// <param name="fileName">
        /// The name to use for saving the MIDI file.
        /// </param>
        public void Save(string fileName)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }
            else if(fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            FileStream stream = new FileStream(fileName, FileMode.Create,
                FileAccess.Write, FileShare.None);
            using (stream)
            {
                Save(stream);
            }
        }

        /// <summary>
        /// Saves the Sequence as a Stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to use for saving the sequence.
        /// </param>
        public void Save(Stream stream)
        {
            properties.Write(stream);

            TrackWriter writer = new TrackWriter();

            foreach(Track trk in tracks)
            {
                writer.Track = trk;
                writer.Write(stream);
            }
        }

        public void SaveAsync(string fileName)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }
            else if(IsBusy)
            {
                throw new InvalidOperationException();
            }
            else if(fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            saveWorker.RunWorkerAsync(fileName);
        }

        public void SaveAsyncCancel()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            saveWorker.CancelAsync();
        }

        /// <summary>
        /// Gets the length in ticks of the Sequence.
        /// </summary>
        /// <returns>
        /// The length in ticks of the Sequence.
        /// </returns>
        /// <remarks>
        /// The length in ticks of the Sequence is represented by the Track 
        /// with the longest length.
        /// </remarks>
        public int GetLength()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            int length = 0;

            foreach(Track t in this)
            {
                if(t.Length > length)
                {
                    length = t.Length;
                }
            }

            return length;
        }

        private void OnLoadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EventHandler<AsyncCompletedEventArgs> handler = LoadCompleted;

            if(handler != null)
            {
                handler(this, new AsyncCompletedEventArgs(e.Error, e.Cancelled, null));
            }
        }

        private void OnLoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressChangedEventHandler handler = LoadProgressChanged;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        private void LoadDoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = (string)e.Argument;

            FileStream stream = new FileStream(fileName, FileMode.Open,
                FileAccess.Read, FileShare.Read);

            using(stream)
            {
                MidiFileProperties newProperties = new MidiFileProperties();
                TrackReader reader = new TrackReader();
                List<Track> newTracks = new List<Track>();

                newProperties.Read(stream);

                float percentage;

                for(int i = 0; i < newProperties.TrackCount && !loadWorker.CancellationPending; i++)
                {
                    reader.Read(stream);
                    newTracks.Add(reader.Track);

                    percentage = (i + 1f) / newProperties.TrackCount;

                    loadWorker.ReportProgress((int)(100 * percentage));
                }

                if(loadWorker.CancellationPending)
                {
                    e.Cancel = true;
                }
                else
                {
                    properties = newProperties;
                    tracks = newTracks;
                }
            }            
        }

        private void OnSaveCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EventHandler<AsyncCompletedEventArgs> handler = SaveCompleted;

            if(handler != null)
            {
                handler(this, new AsyncCompletedEventArgs(e.Error, e.Cancelled, null));
            }
        }

        private void OnSaveProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressChangedEventHandler handler = SaveProgressChanged;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        private void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = (string)e.Argument;

            FileStream stream = new FileStream(fileName, FileMode.Create,
                FileAccess.Write, FileShare.None);

            using(stream)
            {
                properties.Write(stream);

                TrackWriter writer = new TrackWriter();

                float percentage;

                for(int i = 0; i < tracks.Count && !saveWorker.CancellationPending; i++)
                {
                    writer.Track = tracks[i];
                    writer.Write(stream);

                    percentage = (i + 1f) / properties.TrackCount;

                    saveWorker.ReportProgress((int)(100 * percentage));
                }

                if(saveWorker.CancellationPending)
                {
                    e.Cancel = true;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Track at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the Track to get.
        /// </param>
        /// <returns>
        /// The Track at the specified index.
        /// </returns>
        public Track this[int index]
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }
                else if(index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index", index,
                        "Sequence index out of range.");
                }

                #endregion

                return tracks[index];
            }
        }

        /// <summary>
        /// Gets the Sequence's division value.
        /// </summary>
        public int Division
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }

                #endregion

                return properties.Division;
            }
        }

        /// <summary>
        /// Gets or sets the Sequence's format value.
        /// </summary>
        public int Format
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }

                #endregion

                return properties.Format;
            }
            set
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }
                else if(IsBusy)
                {
                    throw new InvalidOperationException();
                }

                #endregion

                properties.Format = value;
            }
        }

        /// <summary>
        /// Gets the Sequence's type.
        /// </summary>
        public SequenceType SequenceType
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }

                #endregion

                return properties.SequenceType;
            }
        }

        public bool IsBusy
        {
            get
            {
                return loadWorker.IsBusy || saveWorker.IsBusy;
            }
        }

        #endregion

        #endregion

        #region ICollection<Track> Members

        public void Add(Track item)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            } 
            else if(item == null)
            {
                throw new ArgumentNullException("item");
            }

            #endregion

            tracks.Add(item);

            properties.TrackCount = tracks.Count;
        }

        public void Clear()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            tracks.Clear();

            properties.TrackCount = tracks.Count;
        }

        public bool Contains(Track item)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            return tracks.Contains(item);
        }

        public void CopyTo(Track[] array, int arrayIndex)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            tracks.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }

                #endregion

                return tracks.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("Sequence");
                }

                #endregion

                return false;
            }
        }

        public bool Remove(Track item)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            bool result = tracks.Remove(item);

            if(result)
            {
                properties.TrackCount = tracks.Count;
            }

            return result;
        }

        #endregion

        #region IEnumerable<Track> Members

        public IEnumerator<Track> GetEnumerator()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            return tracks.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("Sequence");
            }

            #endregion

            return tracks.GetEnumerator();
        }

        #endregion

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

            loadWorker.Dispose();
            saveWorker.Dispose();

            disposed = true;

            EventHandler handler = Disposed;

            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
