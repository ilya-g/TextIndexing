using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Primitive.Text.Parsers
{
    [TestFixture]
    public class LineStreamParserTests
    {
        [Test]
        public void ReadAllLines()
        {
            var file = Path.Combine(GetContentPath(), "text.txt");
            var encoding = Encoding.Default;
            const int takeLines = 3;
            var lines = File.ReadAllLines(file, encoding).Take(takeLines);

            var streamParser = new LineStreamParser(new UnitLineParser());
            int linesTaken = 0;
            var lineStream = Observable.Using(() => new StreamReader(file, encoding),
                reader => streamParser.ExtractWords(reader).Do(line => linesTaken++).Take(takeLines));
            var lines2 = lineStream.ToList().Wait();
            Assert.That(lines2, Is.EquivalentTo(lines)); 
            Assert.That(linesTaken, Is.LessThanOrEqualTo(takeLines));
        }


        private class UnitLineParser : ILineParser
        {
            public IEnumerable<string> ExtractWords(string line) { return new[] {line}; }
        }


        static string GetContentPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        }
    }
}
