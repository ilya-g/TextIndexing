using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Primitive.Text.Content;

namespace Primitive.Text.Parsers
{
    [TestFixture]
    public class LineStreamParserTests
    {
        private readonly string largeContentPath;

        public LineStreamParserTests()
        {
            largeContentPath = TestContentManager.PrepareLargeTextFile();
        }

        [Test]
        public void ReadAllLines()
        {
            var file = Path.Combine(TestContentManager.GetContentPath(), "text.txt");
            var encoding = Encoding.Default;
            const int takeLines = 3;
            var lines = File.ReadAllLines(file, encoding).Take(takeLines);

            var streamParser = new LineTextParser(UnitLineParser.Instance);
            int linesTaken = 0;
            var lineStream = Observable.Using(() => new StreamReader(file, encoding),
                reader => streamParser.ExtractWords(reader).Do(line => linesTaken++).Take(takeLines));
            var lines2 = lineStream.ToList().Wait();
            Assert.That(lines2, Is.EquivalentTo(lines)); 
            Assert.That(linesTaken, Is.LessThanOrEqualTo(takeLines));
        }

        [Test, Category("Performance")]
        public void LargeFileReadingPerformance([Values(false,true)]bool isAsyncStream, [Values(false,true)] bool isAsyncParser)
        {
            var path = largeContentPath;
            Assume.That(path != null && File.Exists(path), "Failed to prepare test file");
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 
                    65536,
                    FileOptions.SequentialScan | (isAsyncStream ? FileOptions.Asynchronous : 0))))
            {
                var parser = new LineTextParser(AlphaNumericWordsLineParser.Instance) { UseAsync = isAsyncParser };
                var sw = Stopwatch.StartNew();
                var count = parser.ExtractWords(reader).Count().Wait();
                Console.WriteLine("Read {0} words in {1}", count, sw.Elapsed);
            }
        }


        private class UnitLineParser : ILineParser
        {
            public static readonly UnitLineParser Instance = new UnitLineParser();
            public IEnumerable<string> ExtractWords(string line) { return new[] {line}; }
        }

    }
}
