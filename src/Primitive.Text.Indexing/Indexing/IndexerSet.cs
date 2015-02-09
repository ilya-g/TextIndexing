using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Primitive.Text.Documents.Sources;
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
        ///  Gets the default <see cref="IStreamParser"/> used to extract index words from the document stream
        /// </summary>
        /// <remarks>
        ///  This stream parser is used in <see cref="Add(IDocumentSource, IStreamParser, bool)"/> method to create <see cref="Indexer"/> for <see cref="IDocumentSource"/>
        /// </remarks>
        [NotNull]
        public IStreamParser DefaultStreamParser { get; private set; }

        /// <summary>
        ///  Gets the list of sources included into this index
        /// </summary>
        /// <value>
        ///  The list containing <see cref="Indexer"/> instances for each <see cref="IDocumentSource"/> added.
        /// </value>
        /// <remarks>
        ///  Use <see cref="Add(Indexer)"/> and <see cref="Remove"/> methods to change the list of sources
        /// </remarks>
        [NotNull]
        public IReadOnlyList<Indexer> Indexers { get { return indexers; } }


        private volatile IImmutableList<Indexer> indexers;


        private IndexerSet([NotNull] IIndex index, [NotNull] IStreamParser defaultStreamParser)
        {
            if (index == null) throw new ArgumentNullException("index");
            if (defaultStreamParser == null) throw new ArgumentNullException("defaultStreamParser");

            Index = index;
            DefaultStreamParser = defaultStreamParser;

            indexers = ImmutableList<Indexer>.Empty;
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

            var index = options.CreateIndex();
            var parser = options.GetDefaultStreamParser();
            return new IndexerSet(index, parser);
        }


        /// <summary>
        ///  Adds the specified <paramref name="indexer"/> to the <see cref="Indexers"/> list
        /// </summary>
        /// <param name="indexer">An <see cref="Indexer"/> to add</param>
        /// <returns>Returns a reference to the same <paramref name="indexer"/>, allowing other operations to be chained</returns>
        /// <exception cref="ArgumentException">
        ///  Throw when this set already contains an indexer with the same <see cref="Indexer.Source"/>
        ///  property value as the <paramref name="indexer"/> being added
        /// </exception>
        public Indexer Add([NotNull] Indexer indexer)
        {
            if (indexer == null) throw new ArgumentNullException("indexer");
            if (ContainsSource(indexer.Source))
                throw new ArgumentException("Source is already included in this IndexerSet", "indexer");

            lock (this)
                indexers = indexers.Add(indexer);

            return indexer;
        }

        /// <summary>
        ///  Creates <see cref="Indexer"/> for the specified <paramref name="source"/> and 
        ///  adds it to the <see cref="Indexers"/> list
        /// </summary>
        /// <param name="source">The source, providing documents to include in the index</param>
        /// <param name="streamParser">
        ///  The <see cref="IStreamParser"/> used to extract words from documents of this source.
        ///  In case if this parameter is not specified, the <see cref="DefaultStreamParser"/> property value is used.
        /// </param>
        /// <param name="autoStartIndexing">Specifies, whether to start indexing this source immediately</param>
        /// <returns>Returns an <see cref="Indexer"/> created for the <paramref name="source"/></returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="source"/> is already included in this set</exception>
        public Indexer Add([NotNull] IDocumentSource source, IStreamParser streamParser = null, bool autoStartIndexing = true)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (ContainsSource(source))
                throw new ArgumentException("Source is already included in this IndexerSet", "source");

            var indexer = new Indexer(Index, source, streamParser ?? DefaultStreamParser);

            lock (this)
                indexers = indexers.Add(indexer);

            if (autoStartIndexing)
                indexer.StartIndexing();

            return indexer;
        }

        private bool ContainsSource(IDocumentSource source)
        {
            return indexers.Any(indexer => indexer.Source == source);
        }

        /// <summary>
        ///  Removes the specified <paramref name="indexer"/> from the <see cref="Indexers"/> list
        /// </summary>
        /// <param name="indexer">The <see cref="Indexer"/> to remove from list</param>
        /// <remarks>
        ///  If the <see cref="Indexers"/> list doesn't contain the specified <paramref name="indexer"/>,
        ///  this method does nothing.
        /// </remarks>
        public void Remove([NotNull] Indexer indexer)
        {
            if (indexer == null)
                throw new ArgumentNullException("indexer");

            lock (this)
            {
                var newSources = indexers.Remove(indexer);
                if (indexers == newSources)
                    return;
                indexers = newSources;
            }

            indexer.StopIndexing();
            indexer.RemoveFromIndex();
        }



    }
}
