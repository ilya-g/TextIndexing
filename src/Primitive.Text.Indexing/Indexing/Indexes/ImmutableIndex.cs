using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Indexing.Internal;

namespace Primitive.Text.Indexing
{

    /// <summary>
    ///  <see cref="IIndex"/> implementation, that uses immutable structures to provide non-blocking query
    ///  and zero-cost snapshot operations.
    /// </summary>
    internal sealed class ImmutableIndex : IIndex
    {

        private readonly object lockIndex = new object();
        private volatile InternalSortedList<string, IImmutableSet<DocumentInfo>> wordIndex;

        private readonly StringComparisonComparer wordComparer;

        public ImmutableIndex(StringComparison wordComparison)
        {
            this.wordComparer = new StringComparisonComparer(wordComparison);
            this.wordIndex = new InternalSortedList<string, IImmutableSet<DocumentInfo>>(wordComparer);
        }

        /// <summary>
        ///  String comparison type used to compare words being indexed
        /// </summary>
        public StringComparison WordComparison { get { return wordComparer.ComparisonType; } }

        public WordDocuments GetExactWord(string word)
        {
            IImmutableSet<DocumentInfo> documents;
            if (!wordIndex.TryGetValue(word, out documents))
                return new WordDocuments(word, ImmutableArray<DocumentInfo>.Empty);
            return new WordDocuments(word, documents);
        }

        public IList<WordDocuments> GetWordsStartWith(string wordBeginning)
        {
            if (wordBeginning == null) throw new ArgumentNullException("wordBeginning");

            var wordIndex = this.wordIndex;
            var startingIndex = wordIndex.IndexOfKey(wordBeginning);
            if (startingIndex < 0)
                startingIndex = ~startingIndex;
            return (
                Enumerable.Range(startingIndex, wordIndex.Count - startingIndex)
                    .Select(index =>
                    {
                        var item = wordIndex[index];
                        return new WordDocuments(item.Key, item.Value);
                    })
                    .TakeWhile(item => item.Word.StartsWith(wordBeginning, WordComparison))
                ).ToList();
        }

        public IList<WordDocuments> GetWordsMatching(Func<string, bool> wordPredicate)
        {
            if (wordPredicate == null) throw new ArgumentNullException("wordPredicate");

            return (
                from item in wordIndex
                where wordPredicate(item.Key)
                select new WordDocuments(item.Key, item.Value)
                ).ToList();
        }

        public IList<string> GetIndexedWords()
        {
            var wordIndex = this.wordIndex;
            var result = new List<string>(wordIndex.Count);
            result.AddRange(wordIndex.Keys);
            return result;
        }

        public IIndex Snapshot()
        {
            return new ImmutableIndex(this.WordComparison)
            {
                wordIndex = this.wordIndex
            };
        }

        public void Merge(DocumentInfo document, IEnumerable<string> indexWords)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (indexWords == null) throw new ArgumentNullException("indexWords");

            var sourceWords = new SortedSet<string>(indexWords, wordComparer).ToList();
            var singleDocumentList = ImmutableHashSet.Create(document);
            // Merge join sorted word list with wordIndex list
            lock (lockIndex)
            {
                var oldIndex = this.wordIndex;
                var newIndex = new InternalSortedList<string, IImmutableSet<DocumentInfo>>(this.wordComparer, oldIndex.Count + sourceWords.Count);
                int idxSource = 0;
                int idxTarget = 0;

                while (true)
                {

                    int compareResult = 0;
                    if (idxTarget >= oldIndex.Count)
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
                        compareResult = wordComparer.Compare(sourceWords[idxSource], oldIndex[idxTarget].Key);

                    if (compareResult < 0)
                    {
                        // add and include
                        newIndex.AddSorted(sourceWords[idxSource], singleDocumentList);
                        idxSource += 1;
                        //idxTarget += 1;
                    }
                    else if (compareResult > 0)
                    {
                        var item = oldIndex[idxTarget];
                        newIndex.AddSorted(item.Key, item.Value.Remove(document));
                        idxTarget += 1;
                    }
                    else
                    {
                        var item = oldIndex[idxTarget];
                        newIndex.AddSorted(item.Key, item.Value.Add(document));
                        idxSource += 1;
                        idxTarget += 1;
                    }
                }
                this.wordIndex = newIndex;
            }
        }

        public void RemoveDocumentsMatching(Func<DocumentInfo, bool> predicate)
        {
            lock (lockIndex)
            {
                var oldIndex = this.wordIndex;
                var newIndex = new InternalSortedList<string, IImmutableSet<DocumentInfo>>(this.wordComparer, oldIndex.Count);
                foreach (var item in oldIndex)
                {
                    var newValue = item.Value.Except(item.Value.Where(predicate));
                    if (newValue.Count > 0)
                        newIndex.AddSorted(item.Key, newValue);
                }
                this.wordIndex = newIndex;
            }
        }
    }
}