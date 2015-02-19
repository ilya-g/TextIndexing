using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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

        private volatile State state;

        private readonly StringComparisonComparer wordComparer;

        public ImmutableIndex(StringComparison wordComparison)
        {
            this.wordComparer = new StringComparisonComparer(wordComparison);
            this.state = new State(
                new InternalSortedList<string, IImmutableSet<DocumentInfo>>(wordComparer),
                ImmutableHashSet<DocumentInfo>.Empty);
        }

        private ImmutableIndex(InternalSortedList<string, IImmutableSet<DocumentInfo>> wordIndex, IImmutableSet<DocumentInfo> allDocuments)
        {
            this.wordComparer = (StringComparisonComparer)wordIndex.KeyComparer;
            this.state = new State(wordIndex, allDocuments);
        }
        /// <summary>
        ///  String comparison type used to compare words being indexed
        /// </summary>
        public StringComparison WordComparison { get { return wordComparer.ComparisonType; } }

        public WordDocuments GetExactWord([NotNull] string word)
        {
            if (word == null) throw new ArgumentNullException("word");

            IImmutableSet<DocumentInfo> documents;
            if (!state.wordIndex.TryGetValue(word, out documents))
                return new WordDocuments(word, ImmutableArray<DocumentInfo>.Empty);
            return new WordDocuments(word, documents);
        }

        public IList<WordDocuments> GetWordsStartWith([NotNull] string wordBeginning)
        {
            if (wordBeginning == null) throw new ArgumentNullException("wordBeginning");

            var wordIndex = state.wordIndex;
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

        public IList<WordDocuments> GetWordsMatching([NotNull] Func<string, bool> wordPredicate)
        {
            if (wordPredicate == null) throw new ArgumentNullException("wordPredicate");

            return (
                from item in state.wordIndex
                where wordPredicate(item.Key)
                select new WordDocuments(item.Key, item.Value)
                ).ToList();
        }

        public IList<string> GetIndexedWords()
        {
            var wordIndex = state.wordIndex;
            var result = new List<string>(wordIndex.Count);
            result.AddRange(wordIndex.Keys);
            return result;
        }

        public IReadOnlyIndex Snapshot()
        {
            var state = this.state;
            return new ImmutableIndex(state.wordIndex, state.documents);
        }

        public Task Merge([NotNull] DocumentInfo document, [NotNull] IEnumerable<string> indexWords)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (indexWords == null) throw new ArgumentNullException("indexWords");

            var sourceWords = new SortedSet<string>(indexWords, wordComparer).ToList();
            bool hasWords = sourceWords.Any();
            var singleDocumentList = ImmutableHashSet.Create(document);
            // Merge join sorted word list with wordIndex list
            lock (lockIndex)
            {
                IImmutableSet<DocumentInfo> oldDocuments = state.documents;
                IImmutableSet<DocumentInfo> newDocuments;
                bool isNewDocument;
                if (hasWords)
                {
                    newDocuments = oldDocuments.Add(document);
                    isNewDocument = newDocuments != oldDocuments; // new if was added to document set
                }
                else
                {
                    newDocuments = oldDocuments.Remove(document);
                    isNewDocument = newDocuments == oldDocuments; // new if wasn't contained before in document set
                    if (isNewDocument)
                        return CompletedTask.Instance;
                }

                var oldIndex = state.wordIndex;
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
                        if (!isNewDocument)
                        {
                            var newValue = item.Value.Remove(document);
                            if (newValue.Count > 0)
                                newIndex.AddSorted(item.Key, newValue);
                        }
                        else
                        {
                            newIndex.AddSorted(item.Key, item.Value);
                        }
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
                this.state = new State(newIndex, newDocuments);
            }
            return CompletedTask.Instance;
        }

        public void RemoveDocumentsMatching([NotNull] Func<DocumentInfo, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            lock (lockIndex)
            {
                var oldDocuments = state.documents;
                var allDocumentsToRemove = new HashSet<DocumentInfo>(oldDocuments.Where(predicate));
                if (!allDocumentsToRemove.Any())
                    return;
                var newDocuments = oldDocuments.Except(allDocumentsToRemove);

                predicate = allDocumentsToRemove.Contains;

                var oldIndex = state.wordIndex;
                var newIndex = new InternalSortedList<string, IImmutableSet<DocumentInfo>>(this.wordComparer, oldIndex.Count);
                foreach (var item in oldIndex)
                {
                    var newValue = item.Value.Except(item.Value.Where(predicate));
                    if (newValue.Count > 0)
                        newIndex.AddSorted(item.Key, newValue);
                }
                this.state = new State(newIndex, newDocuments);
            }
        }


        private class State
        {
            public State(InternalSortedList<string, IImmutableSet<DocumentInfo>> wordIndex, IImmutableSet<DocumentInfo> allDocuments)
            {
                this.wordIndex = wordIndex;
                this.documents = allDocuments;
            }

            public readonly InternalSortedList<string, IImmutableSet<DocumentInfo>> wordIndex;
            public readonly IImmutableSet<DocumentInfo> documents;
        }

    }
}