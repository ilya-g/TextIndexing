using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Primitive.Text.Indexing.Internal
{
    internal class InternalSortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly List<TKey> keys;
        private readonly List<TValue> values; 

        public InternalSortedList(IComparer<TKey> keyComparer, int capacity = 0)
        {
            KeyComparer = keyComparer;
            keys = new List<TKey>(capacity);
            values = new List<TValue>(capacity);
        }

        public InternalSortedList(InternalSortedList<TKey, TValue> source)
        {
            this.KeyComparer = source.KeyComparer;
            this.keys = new List<TKey>(source.keys);
            this.values = new List<TValue>(source.values);
        }

        public IComparer<TKey> KeyComparer { get; private set; }


        /// <returns>
        /// The zero-based index of <paramref name="key"/> within the entire <see cref="T:System.Collections.Generic.SortedList`2"/>, if found;
        /// otherwise, 2-complement of index of first key that is greater than specified <paramref name="key"/>
        /// </returns>
        public int IndexOfKey(TKey key)
        {
            return keys.BinarySearch(key, KeyComparer);
        }

        public int Count { get { return keys.Count; } }


        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = IndexOfKey(key);
            if (index < 0)
            {
                value = default(TValue);
                return false;
            }
            else
            {
                value = values[index];
                return true;
            }
        }

        public KeyValuePair<TKey, TValue> this[int index]
        {
            get { return new KeyValuePair<TKey, TValue>(keys[index], values[index]); }
        }


        public void SetValueAt(int index, TValue value)
        {
            values[index] = value;
        }

        public void Add(TKey key, TValue value)
        {
            var index = IndexOfKey(key);
            if (index >= 0)
                throw new ArgumentException(string.Format("Key '{0}' is already added", key), "key");
            index = ~index;
            keys.Insert(index, key);
            values.Insert(index, value);
        }
        public void AddSorted(TKey key, TValue value)
        {
#if DEBUG
            if (keys.Count > 0)
            {
                var lastKey = keys.Last();
                if (KeyComparer.Compare(key, lastKey) <= 0)
                    throw new ArgumentException("New key value must be stricty greater than the greatest key in the list", "key");
            }
#endif
            keys.Add(key);
            values.Add(value);
        }

        public IEnumerable<TKey> Keys {get { return keys; }} 

        public Enumerator GetEnumerator()
        {
            return new Enumerator(keys, values);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private List<TKey>.Enumerator keyEnumerator;
            private List<TValue>.Enumerator valueEnumerator;

            internal Enumerator(List<TKey> keys, List<TValue> values) 
            {
                this.keyEnumerator = keys.GetEnumerator();
                this.valueEnumerator = values.GetEnumerator();
            }

            public void Dispose()
            {
                keyEnumerator.Dispose();
                valueEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return keyEnumerator.MoveNext() & valueEnumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    return new KeyValuePair<TKey, TValue>(keyEnumerator.Current, valueEnumerator.Current);
                }
            }

            object IEnumerator.Current { get { return Current; } }
        }
    }
}