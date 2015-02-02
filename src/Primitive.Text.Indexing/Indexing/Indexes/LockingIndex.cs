using System;
using System.Collections.Generic;
using System.Linq;
using Primitive.Text.Documents;
using Primitive.Text.Indexing.Internal;

namespace Primitive.Text.Indexing
{
    internal sealed class LockingIndex : IIndex
    {
        private readonly SortedList<string, ISet<DocumentInfo>> wordIndex;
        private readonly StringComparisonComparer wordComparer;
        private readonly LockingStrategy locking;

        public LockingIndex(StringComparison wordComparison, LockingStrategy locking)
        {
            this.wordComparer = new StringComparisonComparer(wordComparison);
            this.wordIndex = new SortedList<string, ISet<DocumentInfo>>(wordComparer);
            this.locking = locking;
        }

        public StringComparison WordComparison { get { return wordComparer.ComparisonType; } }


        public IEnumerable<DocumentInfo> QueryDocuments(string word)
        {
            using (locking.InReadLock())
            {
                ISet<DocumentInfo> documents;
                if (!wordIndex.TryGetValue(word, out documents))
                    return Enumerable.Empty<DocumentInfo>();
                return documents.ToList();
            }
        }

        public IList<KeyValuePair<string, IEnumerable<DocumentInfo>>> QueryDocumentsStartsWith(string word)
        {
            return QueryDocumentsMatching(key => key.StartsWith(word, WordComparison));
        }

        public IList<KeyValuePair<string, IEnumerable<DocumentInfo>>> QueryDocumentsMatching(Func<string, bool> wordPredicate)
        {
            using (locking.InReadLock())
            {
                return (
                    from item in wordIndex
                    where wordPredicate(item.Key)
                    select new KeyValuePair<string, IEnumerable<DocumentInfo>>(item.Key, item.Value.ToList())
                    ).ToList();
            }
        }

        public IList<string> GetIndexedWords()
        {
            using (locking.InReadLock())
                return wordIndex.Keys.ToList();
        }

        public IIndex Snapshot()
        {
            var snapshot = new LockingIndex(this.WordComparison, this.locking);
            using (locking.InReadLock())
            {
                foreach (var item in wordIndex)
                {
                    snapshot.wordIndex.Add(item.Key, new HashSet<DocumentInfo>(item.Value));
                }
            }
            return snapshot;
        }

        /// <summary>
        ///  Merges document word index into this index
        /// </summary>
        /// <param name="documentInfo">Document to include in index</param>
        /// <param name="indexWords">Words to index document by</param>
        /// <remarks>
        ///  Merge is an atomic operation
        /// </remarks>
        public void Merge(DocumentInfo documentInfo, IEnumerable<string> indexWords)
        {
            var sourceWords = new SortedSet<string>(indexWords, wordComparer).ToList();

            // Merge join sorted word list with wordIndex list
            using (locking.InWriteLock())
            {
                int idxSource = 0;
                int idxTarget = 0;

                while (true)
                {

                    int compareResult = 0;
                    if (idxTarget >= wordIndex.Count)
                    {
                        compareResult = -1;
                    }
                    if (idxSource >= sourceWords.Count)
                    {
                        if (compareResult != 0)
                            break;
                        compareResult = 1;
                    }
                    if (compareResult == 0)
                        compareResult = wordComparer.Compare(sourceWords[idxSource], wordIndex.Keys[idxTarget]);

                    if (compareResult < 0)
                    {
                        // add and include
                        wordIndex.Add(sourceWords[idxSource], new HashSet<DocumentInfo> { documentInfo });
                        idxSource += 1;
                        idxTarget += 1;
                    }
                    else if (compareResult > 0)
                    {
                        var documents = wordIndex.Values[idxTarget];
                        documents.Remove(documentInfo);
                        idxTarget += 1;
                    }
                    else
                    {
                        var documents = wordIndex.Values[idxTarget];
                        documents.Add(documentInfo);
                        idxSource += 1;
                        idxTarget += 1;
                    }
                }
            }
        }

        public void RemoveDocumentsMatching(Func<DocumentInfo, bool> predicate)
        {
            using (locking.InWriteLock())
            {
                foreach (var item in wordIndex)
                {
                    List<DocumentInfo> valuesToRemove = null;
                    foreach (var document in item.Value)
                    {
                        if (predicate(document))
                            (valuesToRemove ?? (valuesToRemove = new List<DocumentInfo>())).Add(document);
                    }
                    if (valuesToRemove != null)
                        item.Value.ExceptWith(valuesToRemove);
                }
            }
        }
    }
}