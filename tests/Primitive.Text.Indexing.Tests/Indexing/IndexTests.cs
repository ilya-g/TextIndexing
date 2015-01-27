using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Primitive.Text.Documents;

namespace Primitive.Text.Indexing
{
    [TestFixture]
    public class IndexTests
    {
        [Test]
        public void ParallelPopulateIndexAndQueryDocuments(
            [Values(IndexLocking.NoLocking, IndexLocking.Exclusive, IndexLocking.ReadWrite)] IndexLocking indexLocking
        )
        {
            var rndDoc = new Random();
            int maxDocuments = 1000;
            int maxWords = 1000;
            var documents = Enumerable.Range(1, maxDocuments * 10)
                .Select(n =>
                {
                    int docNumber = rndDoc.Next(1, maxDocuments);
                    return new
                    {
                        document = new DocumentInfo(docNumber.ToString(), TestDocumentSource.Instance),
                        words = Enumerable.Range(n, 500)
                            .Select(_ => string.Format("word{0:000}", rndDoc.Next(1, maxWords / 2) * 2 + docNumber % 2)),
                    };
                }).ToList();

            var indexCreationOptions = new IndexerCreationOptions()
            {
                IndexLocking = indexLocking
            };
            IIndex index = indexCreationOptions.CreateIndex();
            Console.WriteLine("Created index {0}", index.GetType().Name);

            Parallel.Invoke(
                () =>
                {
                    Thread.Sleep(5000);
                    var rnd = new Random();
                    int totalDocuments = 0;
                    var queryCount = 100000;
                    var sw = Stopwatch.StartNew();

                    Parallel.For(0, queryCount, new ParallelOptions() { MaxDegreeOfParallelism = 2 },
                        _ => Interlocked.Add(ref totalDocuments, index.QueryDocuments(string.Format("word{0:000}", rnd.Next(2, maxWords))).Count()));
                    Console.WriteLine("Made {0} queries, queried {1} documents total, elapsed {2} ms total", queryCount, totalDocuments, sw.ElapsedMilliseconds);
                },
                () =>
                {
                    Thread.Sleep(5000);
                    var snapshots = 1000;
                    var sw = Stopwatch.StartNew();
                    Parallel.For(0, snapshots, new ParallelOptions() { MaxDegreeOfParallelism = 2 },
                        _ => index.Snapshot());
                    Console.WriteLine("Made {0} snapshots, elapsed {1} ms total", snapshots, sw.ElapsedMilliseconds);
                }, 
                () =>
                {
                    var sw = Stopwatch.StartNew();
                    Parallel.ForEach(documents, new ParallelOptions {MaxDegreeOfParallelism = 2},
                        item => index.Merge(item.document, item.words));
                    Console.WriteLine("Populated index, elapsed {0} ms", sw.ElapsedMilliseconds);
                });

            var indexedWords = index.GetIndexedWords();
            Assert.That(indexedWords.Count, Is.LessThanOrEqualTo(maxWords));
            foreach (var indexedWord in indexedWords)
            {
                int wordNumber = int.Parse(indexedWord.Substring(4));
                var wordDocuments = index.QueryDocuments(indexedWord);
                Assert.That(wordDocuments.Count(), Is.LessThanOrEqualTo(maxDocuments));
                Assert.That(wordDocuments.All(d => int.Parse(d.Id) % 2 == wordNumber % 2));
            }
        }
    }
}
