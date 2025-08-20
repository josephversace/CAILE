using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IIM.Core.Collections
{
    /// <summary>
    /// Thread-safe circular buffer with fixed capacity
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private readonly int _capacity;
        private readonly ReaderWriterLockSlim _lock = new();
        private int _head = 0;
        private int _tail = 0;
        private int _count = 0;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            _capacity = capacity;
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % _capacity;

                if (_count < _capacity)
                {
                    _count++;
                }
                else
                {
                    _head = (_head + 1) % _capacity; // Overwrite oldest
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int RemoveWhere(Func<T, bool> predicate)
        {
            _lock.EnterWriteLock();
            try
            {
                var items = ToArrayInternal();
                var kept = items.Where(x => !predicate(x)).ToArray();
                var removed = items.Length - kept.Length;

                if (removed > 0)
                {
                    Array.Clear(_buffer, 0, _capacity);
                    _head = 0;
                    _tail = 0;
                    _count = 0;

                    foreach (var item in kept)
                    {
                        _buffer[_tail] = item;
                        _tail = (_tail + 1) % _capacity;
                        _count++;
                    }
                }

                return removed;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public T[] ToArray()
        {
            _lock.EnterReadLock();
            try
            {
                return ToArrayInternal();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private T[] ToArrayInternal()
        {
            if (_count == 0)
                return Array.Empty<T>();

            var result = new T[_count];
            var index = _head;

            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[index];
                index = (index + 1) % _capacity;
            }

            return result;
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ToArray().AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}