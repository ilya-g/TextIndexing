using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    public sealed class Indexer
    {
        [NotNull]
        public IIndex Index { get; private set; }
        
        [NotNull]
        public IStreamParser StreamParser { get; private set; }

        [NotNull]
        public IReadOnlyList<IDocumentSource> DocumentSources { get; private set; }


        private readonly IComparer<string> wordComparer;


        private Indexer([NotNull] IIndex index, [NotNull] IStreamParser streamParser)
        {
            if (index == null) throw new ArgumentNullException("index");
            if (streamParser == null) throw new ArgumentNullException("streamParser");

            Index = index;
            StreamParser = streamParser;

            this.wordComparer = new StringComparisonComparer(index.WordComparison);
            DocumentSources = ImmutableList<IDocumentSource>.Empty;
        }

        /// <summary>
        ///  Creates Indexer with the default <see cref="IndexerCreationOptions"/>
        /// </summary>
        /// <returns></returns>
        public static Indexer Create()
        {
            return Create(new IndexerCreationOptions());
        }

        /// <summary>
        ///  Creates Indexer with the specified <see cref="IndexerCreationOptions"/>
        /// </summary>
        /// <param name="options">Options that define indexer behavior: word comparsion, index locking, document parser</param>
        /// <returns></returns>
        public static Indexer Create([NotNull] IndexerCreationOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (options.StreamParser != null && options.LineParser != null)
                throw new ArgumentException("StreamLexer and LineLexer cannot be specified simulaneosly", "options");

            var index = options.CreateIndex();
            var lexer = options.StreamParser ?? new StreamLineParser(options.LineParser ?? RegexLineParser.Default);
            return new Indexer(index, lexer);
        }


        public void AddDocumentSource([NotNull] IDocumentSource source) // return type: RegisteredDocumentSource?
        {
            if (source == null) throw new ArgumentNullException("source");

            source.FindAllDocuments()
                .Merge(source.ChangedDocuments())
                .ObserveOn(Scheduler.Default)
                .Subscribe(OnDocumentFound);
        }

        private void OnDocumentFound(DocumentInfo documentInfo)
        {
                Observable.Using(
                    () => documentInfo.Source.OpenDocument(documentInfo),
                    reader => 
                        StreamParser.ExtractWords(reader).Aggregate(
                            new SortedSet<string>(wordComparer), 
                            (set, word) =>
                            {
                                set.Add(word);
                                return set;
                            }))
                .Subscribe(indexWords => Index.Merge(documentInfo, indexWords));
        }

    }
}
