using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Indexing.Internal;

namespace Primitive.Text.Indexing
{

    /// <summary>
    ///  <see cref="IIndex"/> implementation, that uses mutable structures guarded with the specified 
    ///  <see cref="LockingStrategy"/>.
    /// </summary>
    internal sealed class LockingIndex : IIndex
    {
        private readonly SortedList<string, ISet<DocumentInfo>> wordIndex;
        private readonly StringComparisonComparer wordComparer;
        private readonly LockingStrategy locking;

        public LockingIndex(StringComparison wordComparison, [NotNull] LockingStrategy locking)
        {
            if (locking == null) throw new ArgumentNullException("locking");

            this.wordComparer = new StringComparisonComparer(wordComparison);
            this.wordIndex = new SortedList<string, ISet<DocumentInfo>>(wordComparer);
            this.locking = locking;
        }

        public StringComparison WordComparison { get { return wordComparer.ComparisonType; } }


        public WordDocuments GetExactWord(string word)
        {
            using (locking.InReadLock())
            {
                ISet<DocumentInfo> documents;
                if (!wordIndex.TryGetValue(word, out documents))
                    return new WordDocuments(word, ImmutableArray<DocumentInfo>.Empty);
                return new WordDocuments(word, documents.ToList());
            }
        }

        public IList<WordDocuments> GetWordsStartWith(string wordBeginning)
        {
            if (wordBeginning == null) throw new ArgumentNullException("wordBeginning");

            return GetWordsMatching(key => key.StartsWith(wordBeginning, WordComparison));
        }

        public IList<WordDocuments> GetWordsMatching(Func<string, bool> wordPredicate)
        {
            if (wordPredicate == null) throw new ArgumentNullException("wordPredicate");

            using (locking.InReadLock())
            {
                return (
                    from item in wordIndex
                    where wordPredicate(item.Key)
                    select new WordDocuments(item.Key, item.Value.ToList())
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

        public void Merge(DocumentInfo document, IEnumerable<string> indexWords)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (indexWords == null) throw new ArgumentNullException("indexWords");

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
                        wordIndex.Add(sourceWords[idxSource], new HashSet<DocumentInfo> { document });
                        idxSource += 1;
                        idxTarget += 1;
                    }
                    else if (compareResult > 0)
                    {
                        var documents = wordIndex.Values[idxTarget];
                        documents.Remove(document);
                        if (documents.Count > 0)
                            idxTarget += 1;
                        else
                            wordIndex.RemoveAt(idxTarget);
                    }
                    else
                    {
                        var documents = wordIndex.Values[idxTarget];
                        documents.Add(document);
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
                List<DocumentInfo> valuesToRemove = null;
                for (int i = 0; i < wordIndex.Count; i++)
                {
                    var value = wordIndex.Values[i];
                    foreach (var document in value)
                    {
                        if (predicate(document))
                            (valuesToRemove ?? (valuesToRemove = new List<DocumentInfo>())).Add(document);
                    }
                    if (valuesToRemove != null && valuesToRemove.Count > 0)
                    {
                        if (valuesToRemove.Count != value.Count)
                            value.ExceptWith(valuesToRemove);
                        else
                        {
                            wordIndex.RemoveAt(i);
                            i -= 1;
                        }
                        valuesToRemove.Clear();
                    }
                }
            }
        }
    }
}