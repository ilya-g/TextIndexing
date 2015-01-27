using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Primitive.Text.Indexing.Internal
{
    internal class InternalSortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly List<KeyValuePair<TKey, TValue>> internalStorage;

        public InternalSortedList(IComparer<TKey> keyComparer, int capacity = 0)
        {
            kvpKeyComparer = new KVPairKeyComparer() {keyComparer = keyComparer};
            internalStorage = new List<KeyValuePair<TKey, TValue>>(capacity);
        }

        public InternalSortedList(InternalSortedList<TKey, TValue> source)
        {
            kvpKeyComparer = new KVPairKeyComparer() { keyComparer = source.KeyComparer };
            this.internalStorage = new List<KeyValuePair<TKey, TValue>>(source.internalStorage);
        }

        public IComparer<TKey> KeyComparer { get { return kvpKeyComparer.keyComparer; } }

        private readonly KVPairKeyComparer kvpKeyComparer;

        private class KVPairKeyComparer: IComparer<KeyValuePair<TKey, TValue>>
        {
            public IComparer<TKey> keyComparer;
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return keyComparer.Compare(x.Key, y.Key);
            }
        }

        public int BinarySearch(TKey key)
        {
            //return internalStorage.BinarySearch(new KeyValuePair<TKey, TValue>(key, default(TValue)), kvpKeyComparer);
            var comparer = KeyComparer;
            int lower = 0, upper = internalStorage.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int comparisonResult = comparer.Compare(key, internalStorage[middle].Key);

                if (comparisonResult == 0)
                    return middle;

                if (comparisonResult < 0)
                    upper = middle - 1;
                else 
                    lower = middle + 1;
            }

            return ~lower;
        }

        public int Count { get { return internalStorage.Count; } }


        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = BinarySearch(key);
            if (index < 0)
            {
                value = default(TValue);
                return false;
            }
            else
            {
                value = internalStorage[index].Value;
                return true;
            }
        }

        public KeyValuePair<TKey, TValue> this[int index]
        {
            get { return internalStorage[index]; }
        }


        public void SetValueAt(int index, TValue value)
        {
            internalStorage[index] = new KeyValuePair<TKey, TValue>(internalStorage[index].Key, value);
        }

        public void Add(TKey key, TValue value)
        {
            var index = BinarySearch(key);
            if (index >= 0)
                throw new ArgumentException(string.Format("Key '{0}' is already added", key), "key");
            index = ~index;
            internalStorage.Insert(index, new KeyValuePair<TKey, TValue>(key, value));

        }
        public void AddSorted(TKey key, TValue value)
        {
#if DEBUG
            if (internalStorage.Count > 0)
            {
                var lastKey = internalStorage.Last().Key;
                if (KeyComparer.Compare(key, lastKey) <= 0)
                    throw new ArgumentException("New key value must be stricty greater than the greatest key in the list", "key");
            }
#endif
            internalStorage.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator()
        {
            return internalStorage.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}