﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Primitive.Text.Documents.Sources;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Exposes properties and methods to manage indexed document sources and 
    ///  provides the access to the index
    /// </summary>
    /// <remarks>
    /// </remarks>
    public sealed class IndexerSet
    {
        /// <summary>
        ///  Gets the <see cref="IIndex"/> instance, used to store the relationship between words and documents
        /// </summary>
        [NotNull]
        public IIndex Index { get; private set; }
        
        /// <summary>
        ///  Gets the <see cref="IStreamParser"/> used to extract index words from the document stream
        /// </summary>
        [NotNull]
        public IStreamParser StreamParser { get; private set; }

        /// <summary>
        ///  Gets the list of sources included into this index
        /// </summary>
        /// <value>
        ///  The list containing <see cref="Indexer"/> instances for each <see cref="IDocumentSource"/> added.
        /// </value>
        /// <remarks>
        ///  Use <see cref="AddSource"/> and <see cref="RemoveSource"/> methods to change the list of sources
        /// </remarks>
        [NotNull]
        public IReadOnlyList<Indexer> Sources { get { return sources; } }


        private volatile IImmutableList<Indexer> sources;

        private readonly IComparer<string> wordComparer;


        private IndexerSet([NotNull] IIndex index, [NotNull] IStreamParser streamParser)
        {
            if (index == null) throw new ArgumentNullException("index");
            if (streamParser == null) throw new ArgumentNullException("streamParser");

            Index = index;
            StreamParser = streamParser;

            wordComparer = new StringComparisonComparer(index.WordComparison);
            sources = ImmutableList<Indexer>.Empty;
        }

        /// <summary>
        ///  Creates Indexer with the default <see cref="IndexerCreationOptions"/>
        /// </summary>
        public static IndexerSet Create()
        {
            return Create(new IndexerCreationOptions());
        }

        /// <summary>
        ///  Creates Indexer with the specified <see cref="IndexerCreationOptions"/>
        /// </summary>
        /// <param name="options">Options that define indexer behavior: word comparsion, index locking, document parser</param>
        public static IndexerSet Create([NotNull] IndexerCreationOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (options.StreamParser != null && options.LineParser != null)
                throw new ArgumentException("StreamParser and LineParser cannot be specified simulaneosly", "options");

            var index = options.CreateIndex();
            var parser = options.StreamParser ?? new LineStreamParser(options.LineParser ?? AlphaNumericWordsLineParser.Instance);
            return new IndexerSet(index, parser);
        }


        /// <summary>
        ///  Creates <see cref="Indexer"/> for the specified <paramref name="source"/> and 
        ///  adds it to the <see cref="Sources"/> list
        /// </summary>
        /// <param name="source">The source, providing documents to include in the index</param>
        /// <param name="autoStartIndexing">Specifies, whether to start indexing this source immediately</param>
        /// <returns>Returns <see cref="Indexer"/> created for the <paramref name="source"/></returns>
        public Indexer AddSource([NotNull] IDocumentSource source, bool autoStartIndexing = true)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var sourceIndexingAgent = new Indexer(this, source);

            lock (this)
                sources = sources.Add(sourceIndexingAgent);

            if (autoStartIndexing)
                sourceIndexingAgent.StartIndexing();

            return sourceIndexingAgent;
        }

        /// <summary>
        ///  Removes the specified <paramref name="indexerndexingAgent"/> from the <see cref="Sources"/> list
        /// </summary>
        /// <param name="indexerndexingAgent">The <see cref="Indexer"/> to remove from list</param>
        /// <remarks>
        ///  If the <see cref="Sources"/> list doesn't contain the specified <paramref name="indexerndexingAgent"/>,
        ///  this method does nothing.
        /// </remarks>
        public void RemoveSource([NotNull] Indexer indexer)
        {
            if (indexer == null)
                throw new ArgumentNullException("indexer");

            lock (this)
            {
                var newSources = sources.Remove(indexer);
                if (sources == newSources)
                    return;
                sources = newSources;
            }

            indexer.StopIndexing();
            // remove all documents from this source from index
            Index.RemoveDocumentsMatching(document => document.Source == indexer.DocumentSource);
        }

        internal ISet<string> CreateEmptyWordSet()
        {
            return new SortedSet<string>(wordComparer);
        }


    }
}
