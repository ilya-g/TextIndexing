using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;
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
        public IReadOnlyList<DocumentSourceIndexer> DocumentSources { get { return documentSources; } }


        private volatile IImmutableList<DocumentSourceIndexer> documentSources;

        private readonly IComparer<string> wordComparer;


        private Indexer([NotNull] IIndex index, [NotNull] IStreamParser streamParser)
        {
            if (index == null) throw new ArgumentNullException("index");
            if (streamParser == null) throw new ArgumentNullException("streamParser");

            Index = index;
            StreamParser = streamParser;

            wordComparer = new StringComparisonComparer(index.WordComparison);
            documentSources = ImmutableList<DocumentSourceIndexer>.Empty;
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
            var parser = options.StreamParser ?? new LineStreamParser(options.LineParser ?? RegexLineParser.Default);
            return new Indexer(index, parser);
        }


        public DocumentSourceIndexer AddDocumentSource([NotNull] IDocumentSource source, bool autoStartIndexing = true)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var indexedSource = new DocumentSourceIndexer(
                source, 
                GetDocumentIndexWords,
                MergeIndexedDocument);

            lock (this)
                documentSources = documentSources.Add(indexedSource);

            if (autoStartIndexing)
                indexedSource.StartIndexing();

            return indexedSource;
        }

        public void RemoveDocumentSource([NotNull] DocumentSourceIndexer documentSourceIndexer)
        {
            if (documentSourceIndexer == null)
                throw new ArgumentNullException("documentSourceIndexer");

            lock (this)
                documentSources = documentSources.Remove(documentSourceIndexer);

            documentSourceIndexer.StopIndexing();
            // remove all documents from this source from index
            Index.RemoveDocumentsMatching(document => document.Source == documentSourceIndexer.DocumentSource);
        }


        private Task<ISet<string>> GetDocumentIndexWords(DocumentInfo documentInfo)
        {
            var documentReader = Observable.Using(
                () => documentInfo.Source.OpenDocument(documentInfo),
                reader =>
                    StreamParser.ExtractWords(reader ?? StreamReader.Null).Aggregate(
                        new SortedSet<string>(wordComparer) as ISet<string>,
                        (set, word) =>
                        {
                            set.Add(word);
                            return set;
                        }));

            return documentReader.ToTask();
        }

        private void MergeIndexedDocument(IndexedDocument indexedDocument)
        {
            Index.Merge(indexedDocument.Document, indexedDocument.IndexWords);
        }

    }
}
