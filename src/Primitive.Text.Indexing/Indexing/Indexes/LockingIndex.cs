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
        private readonly ISet<DocumentInfo> allDocuments;
        private readonly StringComparisonComparer wordComparer;
        private readonly LockingStrategy locking;

        public LockingIndex(StringComparison wordComparison, [NotNull] LockingStrategy locking)
        {
            if (locking == null) throw new ArgumentNullException("locking");

            this.wordComparer = new StringComparisonComparer(wordComparison);
            this.wordIndex = new SortedList<string, ISet<DocumentInfo>>(wordComparer);
            this.allDocuments = new HashSet<DocumentInfo>();
            this.locking = locking;
        }

        public StringComparison WordComparison { get { return wordComparer.ComparisonType; } }


        public WordDocuments GetExactWord(string word)
        {
            if (word == null) throw new ArgumentNullException("word");
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

            Func<string, bool> wordPredicate = key => key.StartsWith(wordBeginning, WordComparison);

            using (locking.InReadLock())
            {
                return wordIndex
                        .SkipWhile(item => !wordPredicate(item.Key))
                        .TakeWhile(item => wordPredicate(item.Key))
                        .Select(item => new WordDocuments(item.Key, item.Value.ToList()))
                        .ToList();
            }
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

        public IReadOnlyIndex Snapshot()
        {
            var snapshot = new LockingIndex(this.WordComparison, new LockingStrategy.SnapshotLocking());
            using (locking.InReadLock())
            {
                snapshot.allDocuments.UnionWith(this.allDocuments);
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
            bool hasWords = sourceWords.Any();

            // Merge join sorted word list with wordIndex list
            using (locking.InWriteLock())
            {
                bool isNewDocument = hasWords ? allDocuments.Add(document) : !allDocuments.Remove(document);
                if (isNewDocument && !hasWords) return;

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
                    }
                    else if (compareResult > 0)
                    {
                        if (!isNewDocument)
                        {
                            var documents = wordIndex.Values[idxTarget];
                            documents.Remove(document);
                            if (documents.Count <= 0)
                            {
                                wordIndex.RemoveAt(idxTarget);
                                idxTarget -= 1;
                            }
                        }
                    }
                    else
                    {
                        var documents = wordIndex.Values[idxTarget];
                        documents.Add(document);
                        idxSource += 1;
                    }

                    idxTarget += 1;
                }
            }
        }

        public void RemoveDocumentsMatching(Func<DocumentInfo, bool> predicate)
        {
            using (locking.InWriteLock())
            {
                var valuesToRemove = allDocuments.Where(predicate).ToList();
                if (!valuesToRemove.Any())
                    return;
                allDocuments.ExceptWith(valuesToRemove);
                valuesToRemove = new List<DocumentInfo>();

                for (int i = wordIndex.Count - 1; i >= 0; i--)
                {
                    var value = wordIndex.Values[i];

                    foreach (var document in value)
                    {
                        if (predicate(document))
                            valuesToRemove.Add(document);
                    }
                    if (valuesToRemove.Count > 0)
                    {
                        if (valuesToRemove.Count != value.Count)
                            value.ExceptWith(valuesToRemove);
                        else
                            wordIndex.RemoveAt(i);

                        valuesToRemove.Clear();
                    }
                }
            }
        }
    }
}