using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;

namespace Primitive.Text.Indexing
{
    [TestFixture(IndexLocking.NoLocking)]
    [TestFixture(IndexLocking.Exclusive)]
    [TestFixture(IndexLocking.ReadWrite)]
    public class IndexTests
    {
        private readonly IndexerCreationOptions indexCreationOptions;

        public IndexTests(IndexLocking indexLocking)
        {
            indexCreationOptions = new IndexerCreationOptions()
            {
                IndexLocking = indexLocking
            };
        }

        [Test]
        public void MergeDocument_GetItBack()
        {
            IIndex index = indexCreationOptions.CreateIndex();
            var document = new DocumentInfo("id1", TestDocumentSource.Instance);
            var words = new[] {"cat", "category"};

            index.Merge(document, words);

            foreach (var word in words)
            {
                Assert.That(index.GetExactWord(word).Single(), Is.EqualTo(document));
            }

            foreach (var partialWord in new[]{"cat", "ca"})
            {
                var catDocuments = index.GetWordsStartWith(partialWord);
                Assert.That(catDocuments, Has.Count.EqualTo(words.Count()));
                foreach (var documents in catDocuments)
                {
                    Assert.That(documents.Single(), Is.EqualTo(document));
                }
            }
            Assert.That(index.GetWordsStartWith("cate"), Has.Count.EqualTo(1));

            index.Merge(document, Enumerable.Empty<string>());
            foreach (var word in words)
            {
                Assert.That(index.GetExactWord(word), Is.Empty);
            }
            Assert.That(index.GetIndexedWords(), Is.Empty);
        }

        [Test]
        public void RemovingDocumentsMatching()
        {
            IIndex index = indexCreationOptions.CreateIndex();
            PopulateIndex(index, GenerateDocuments(5, 5, 100));

            Func<DocumentInfo, bool> documentPredicate = document => int.Parse(document.Id) % 2 == 0;

            index.RemoveDocumentsMatching(documentPredicate);

            foreach (var documents in index.GetWordsMatching(_ => true))
            {
                Assert.That(documents.Where(documentPredicate), Is.Empty);
            }

            index.RemoveDocumentsMatching(_ => true);
            Assert.That(index.GetIndexedWords(), Is.Empty);
        }

        [Test]
        public void StartsWith_UsingInvariantComparison()
        {
            var indexCreationOptions = new IndexerCreationOptions()
            {
                IndexLocking = this.indexCreationOptions.IndexLocking,
                WordComparison = StringComparison.InvariantCultureIgnoreCase
            };
            IIndex index = indexCreationOptions.CreateIndex();
            var document = new DocumentInfo("id1", TestDocumentSource.Instance);
            var words = new[] { "Schrœdinger", "Schroedinger", "Schroeder" };

            index.Merge(document, words);
            Assert.That(index.GetIndexedWords(), Has.Count.EqualTo(2));

            Assert.That(index.GetWordsStartWith("schroe"), Has.Count.EqualTo(2));
            Assert.That(index.GetWordsStartWith("schrœ"), Has.Count.EqualTo(2));
        }

        [Test]
        public void Snapshot_UnchangedAfterMerge()
        {
            IIndex index = indexCreationOptions.CreateIndex();
            var document = new DocumentInfo("id1", TestDocumentSource.Instance);
            var words = new[] { "cat", "category" };
            index.Merge(document, words);

            var snapshot = index.Snapshot();

            var document2 = new DocumentInfo("id2", TestDocumentSource.Instance);
            var words2 = new[] {"bar"};
            index.Merge(document2, words2);

            Assert.That(snapshot.GetIndexedWords(), Is.EquivalentTo(words));
            Assert.That(snapshot.GetWordsMatching(_ => true).SelectMany(item => item).Distinct().Single(), Is.EqualTo(document));
        }



        [Test, Category("Performance")]
        public void SequentialPerformance()
        {
            var documents = GenerateDocuments(distinctDocumentsCount: 500, distinctWordsCount: 200);
            var index = indexCreationOptions.CreateIndex();

            PopulateIndex(index, documents);

            MeasureUntil(TimeSpan.FromSeconds(1), "Snapshot", () => index.Snapshot());

            var rng = new Random();
            var words = index.GetIndexedWords();
            MeasureUntil(TimeSpan.FromSeconds(1), "Query", () => index.GetExactWord(words[rng.Next(words.Count)]));
        }

        [Test, Category("Performance")]
        public void PopulationPatternsPerformance()
        {
            const int documentCount = 100;
            const int wordCount = 5000;
            Console.WriteLine("No changes");
            PopulateIndex(indexCreationOptions.CreateIndex(), GenerateDocumentChangePattern(documentCount, wordCount, 1));

            Console.WriteLine("Change all words every time");
            PopulateIndex(indexCreationOptions.CreateIndex(), GenerateDocumentChangePattern(documentCount, wordCount, 0));

            Console.WriteLine("Change 50% words every time");
            PopulateIndex(indexCreationOptions.CreateIndex(), GenerateDocumentChangePattern(documentCount, wordCount, 0.5));

        }

        [Test, Category("Performance")]
        public void ParallelPopulateQuerySnapshotPerformance()
        {
            int maxDocuments = 1000;
            int maxWords = 1000;
            var documents = GenerateDocuments(maxDocuments, maxWords);

            Action<WordDocuments> validateInvariant = wordDocuments =>
            {
                int wordNumber = int.Parse(wordDocuments.Word.Substring(4));
                Assert.That(wordDocuments.Count(), Is.LessThanOrEqualTo(maxDocuments));
                Assert.That(wordDocuments.All(d => int.Parse(d.Id) % 2 == wordNumber % 2));
            };

            IIndex index = indexCreationOptions.CreateIndex();
            Console.WriteLine("Created index {0}", index.GetType().Name);

            Parallel.Invoke(
                () =>
                {
                    Thread.Sleep(3000);
                    var rnd = new Random();
                    int totalDocuments = 0;

                    MeasureUntil(TimeSpan.FromSeconds(4), "Query", () =>
                    {
                        var wordId = rnd.Next(2, maxWords);
                        string word = string.Format("word{0:000}", wordId);
                        var wordDocuments = index.GetExactWord(word);
                        Interlocked.Add(ref totalDocuments, wordDocuments.Count());
                        if (wordId < maxWords / 100)
                            validateInvariant(wordDocuments);
                    }, maxIterations: 100000);
                    Console.WriteLine("Queried {0} documents total", totalDocuments);
                },
                () =>
                {
                    Thread.Sleep(3500);
                    MeasureUntil(TimeSpan.FromSeconds(4), "Snapshot", () => index.Snapshot());
                }, 
                () => PopulateIndex(index, documents));

            var indexedWords = index.GetIndexedWords();
            Assert.That(indexedWords.Count, Is.LessThanOrEqualTo(maxWords));
            foreach (var indexWord in indexedWords)
            {
                validateInvariant(index.GetExactWord(indexWord));
            }
        }

        private static void MeasureUntil(TimeSpan maxExecutionTime, string operationName, Action actionToMeasure, int maxDegreeOfParallelism = 2, int maxIterations = int.MaxValue)
        {
            {
                var sw = Stopwatch.StartNew();
                var loopResult = Parallel.For(0, maxIterations, new ParallelOptions() {MaxDegreeOfParallelism = maxDegreeOfParallelism},
                    (i, state) =>
                    {
                        actionToMeasure();
                        if (i%100 == 0 && sw.Elapsed > maxExecutionTime)
                            state.Break();
                    });
                sw.Stop();
                var executedIterations = loopResult.LowestBreakIteration ?? maxIterations;
                Console.WriteLine("{0}: {1} operations, elapsed {2} ms total, {3:0.000} ms per operation",
                    operationName, 
                    executedIterations,
                    sw.ElapsedMilliseconds, 
                    sw.Elapsed.TotalMilliseconds/executedIterations);
            }
        }


        private static List<IndexedDocument> GenerateDocuments(int distinctDocumentsCount, int distinctWordsCount, int wordsPerDocument = 500)
        {
            var rndDoc = new Random();
            return Enumerable.Range(1, distinctDocumentsCount * 10)
                .Select(n =>
                {
                    int docNumber = rndDoc.Next(1, distinctDocumentsCount);
                    return new IndexedDocument(
                        document: new DocumentInfo(docNumber.ToString(), TestDocumentSource.Instance),
                        indexWords: new HashSet<string>(Enumerable.Range(n, wordsPerDocument)
                            .Select(_ => string.Format("word{0:000}", rndDoc.Next(1, distinctWordsCount / 2) * 2 + docNumber % 2)))
                        );
                }).ToList();
        }

        private static List<IndexedDocument> GenerateDocumentChangePattern(int count, int totalWords, double fixedWordsPart)
        {
            var document = new DocumentInfo("single", TestDocumentSource.Instance);
            var fixedWords = Enumerable.Range(1, (int)(totalWords*fixedWordsPart)).Select(i => "Fixed_" + i).ToList();
            var wordSets = Enumerable.Range(0, 2).Select(n =>
                new HashSet<string>(
                    fixedWords.Concat(
                        Enumerable.Range(1, (int) (totalWords*(1 - fixedWordsPart))).Select(i => string.Format("Changing_{1}_{0}", n, i))
                    ))
                ).ToList();


            return Enumerable.Range(1, count).Select(i => new IndexedDocument(document, wordSets[i%2])).ToList();
        } 

        private static void PopulateIndex(IIndex index, ICollection<IndexedDocument> documents)
        {
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(documents, new ParallelOptions {MaxDegreeOfParallelism = 2},
                item => index.Merge(item.Document, item.IndexWords));
            sw.Stop();
            Console.WriteLine("Populated index, elapsed {0} ms, total {1} documents processed, total {2} words indexed", sw.ElapsedMilliseconds, documents.Count, index.GetIndexedWords().Count);
        }
    }
}
