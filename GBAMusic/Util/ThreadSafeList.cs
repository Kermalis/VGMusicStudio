using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GBAMusic.Util
{
    class ThreadSafeList<T> : IList<T>
    {
        private List<T> _list = new List<T>();
        private object _lock = new object();

        public int Count { get => _list.Count; }
        public bool IsReadOnly { get => ((IList<T>)_list).IsReadOnly; }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    if (index < 0)
                    {
                        index = this.Count - index;
                    }
                    return _list[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    if (index < 0)
                    {
                        index = this.Count - index;
                    }
                    _list[index] = value;
                }
            }
        }

        public void Add(T value)
        {
            lock (_lock)
            {
                _list.Add(value);
            }
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.FirstOrDefault(predicate);
            }
        }

        public int IndexOf(T item)
        {
            lock (_lock)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _list.Insert(index, item);
            }
        }

        public bool Remove(T value)
        {
            lock (_lock)
            {
                return _list.Remove(value);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _list.RemoveAt(index);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        public List<T> Clone()
        {
            List<T> newList = new List<T>();
            lock (_lock)
            {
                _list.ForEach(x => newList.Add(x));
            }
            return newList;
        }
    }
}
