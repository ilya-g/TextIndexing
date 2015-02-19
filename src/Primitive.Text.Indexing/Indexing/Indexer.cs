using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Orchestrates and controls an individual document source indexing pipeline and exposes properties
    ///  describing the indexing state of the document source.
    /// </summary>
    public sealed class Indexer : IDisposable, INotifyPropertyChanged
    {
        private static readonly int maxConcurrentIndexing = 8;

        private volatile IDisposable subscription;
        private volatile int documentsParsed;
        private volatile int documentsFound;
        private volatile int documentsChanged;
        private volatile int documentsFailed;

        private volatile int runningParsers;
        private volatile Exception error;

        private readonly Subject<Tuple<DocumentInfo, Exception>> indexingErrors = new Subject<Tuple<DocumentInfo, Exception>>();

        private readonly StringComparisonComparer wordComparer;
        private readonly object lockObject = new object();

        /// <summary>
        ///  Creates an instance of <see cref="Indexer"/> that will be indexing documents from 
        ///  the specified <paramref name="source"/> with the <paramref name="textParser"/> and 
        ///  merging them to the <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="source"></param>
        /// <param name="textParser"></param>
        public Indexer([NotNull] IIndex index, [NotNull] IDocumentSource source, [NotNull] ITextParser textParser)
        {
            if (index == null) throw new ArgumentNullException("index");
            if (source == null) throw new ArgumentNullException("source");
            if (textParser == null) throw new ArgumentNullException("textParser");

            this.Index = index;
            this.Source = source;
            this.TextParser = textParser;
            this.wordComparer = new StringComparisonComparer(index.WordComparison);
        }



        /// <summary>
        ///  The Indexer this <see cref="Indexing.Indexer"/> belongs to.
        /// </summary>
        public IIndex Index { get; private set; }

        /// <summary>
        ///  Gets the <see cref="IDocumentSource"/> being indexed
        /// </summary>
        public IDocumentSource Source { get; private set; }

        /// <summary>
        ///  Gets the <see cref="ITextParser"/> used to extract index words from the document stream
        /// </summary>
        public ITextParser TextParser { get; private set; }

        /// <summary>
        ///  Gets the indexing state value
        /// </summary>
        /// <remarks>
        ///  The indexing state describes which activity this <see cref="Indexing.Indexer"/> is busy with.
        ///  The returned value may reflect state changes with a certain amount of delay.
        /// </remarks>
        public IndexingState State
        {
            get
            {
                return 
                    subscription == null ? IndexingState.Stopped :
                    error != null ? IndexingState.Failed :
                    runningParsers > 0 ? IndexingState.Indexing :
                        IndexingState.Watching;
            }
        }

        /// <summary>
        ///  Gets the exception value, describing the reason this instance is in the <see cref="IndexingState.Failed"/> state
        /// </summary>
        /// <value>
        ///  In case if the <see cref="State"/> is <see cref="IndexingState.Failed"/> the exception which has lead to this state,
        ///  null otherwise.
        /// </value>
        public Exception Error
        {
            get { return error; }
            private set
            {
                error = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  Gets the counter value counting number of documents discovered from <see cref="IDocumentSource.FindAllDocuments"/> method.
        /// </summary>
        public int DocumentsFound
        {
            get { return documentsFound; }
            private set
            {
                documentsFound = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        ///  Gets the counter value counting number of changed documents from <see cref="IDocumentSource.WatchForChangedDocuments"/> method.
        /// </summary>
        public int DocumentsChanged
        {
            get { return documentsChanged; }
            private set
            {
                documentsChanged = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  Gets the counter value counting number of documents that was parsed and merged into the index
        /// </summary>
        public int DocumentsParsed
        {
            get { return documentsParsed; }
            private set
            {
                documentsParsed = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  Gets the counter value counting number of documents failed to be indexed.
        /// </summary>
        public int DocumentsFailed
        {
            get { return documentsFailed; }
            private set
            {
                documentsFailed = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  Exposes hot observable stream of errors, encountered during parsing or indexing individual documents
        /// </summary>
        /// <remarks>
        ///  Document indexing error does not lead this instance to the <see cref="IndexingState.Failed"/> state
        ///  and is to be ignored.
        ///  This stream provides the way to observe such errors.
        /// </remarks>
        public IObservable<Tuple<DocumentInfo, Exception>> IndexingErrors { get { return indexingErrors; } }


        /// <summary>
        ///  Begins the indexing of <see cref="Source"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        ///  Starts with calling <see cref="IDocumentSource.FindAllDocuments"/> to discover all documents in source, 
        ///  and then monitors changes returned with <see cref="IDocumentSource.WatchForChangedDocuments"/>.
        ///  All theses documents are being parsed and merged into the index.
        /// </para>
        /// <para>In case if indexing is already started, does nothing.</para>
        /// </remarks>
        public void StartIndexing()
        {
            lock (lockObject)
            {
                if (subscription != null) return;

                subscription = 
                    Observable.Merge(
                        Source.FindAllDocuments()
                            .Buffer(TimeSpan.FromSeconds(0.5), 50)
                            .Where(changes => changes.Count > 0)
                            .Do(files => DocumentsFound += files.Count),
                        Source.WatchForChangedDocuments()
                            .Buffer(TimeSpan.FromSeconds(0.5))
                            .Where(changes => changes.Count > 0)
                            .Select(changes => changes.Distinct().ToList())
                            .Do(changes => DocumentsChanged += changes.Count))
                    .Do(_ => OnParsingStarted())
                    .SelectMany(files => files)
                    .SelectMany(IndexDocument)
                    .Do(_ => OnParsingStarted())
                    .Select(d => Observable.FromAsync(() => Task.Run(() => Index.Merge(d.Document, d.IndexWords))))
                    .Merge(maxConcurrentIndexing)
                    .Do(_ => DocumentsParsed += 1, ex => { this.Error = ex; OnStateChanged(); })
                    .Throttle(TimeSpan.FromSeconds(1))
                    .Subscribe(_ => OnParsingCompleted(), _ => { });
            }
            OnStateChanged();
        }

        /// <summary>
        ///  Stops the indexing of <see cref="Source"/> and clears the <see cref="Error"/> if any.
        /// </summary>
        public void StopIndexing()
        {
            
            // Stop producing indexed documents
            lock (lockObject)
            {
                if (subscription != null)
                    subscription.Dispose();
                subscription = null;
                Error = null;
            }
            OnStateChanged();
        }

        /// <summary>
        ///  Removes all documents, indexed with this indexer, from the <see cref="Index"/>
        /// </summary>
        public void RemoveFromIndex()
        {
            Index.RemoveDocumentsMatching(document => document.Source == Source);
        }


        private IObservable<IndexedDocument> IndexDocument(DocumentInfo documentInfo)
        {
            return
                Source.ExtractDocumentWords(documentInfo, TextParser)
                    .Aggregate(new SortedSet<string>(wordComparer),
                        (set, word) =>
                        {
                            set.Add(word);
                            return set;
                        }, 
                        words => new IndexedDocument(documentInfo, words))
                    .Catch((Exception e) =>
                    {
                        DocumentsFailed += 1;
                        indexingErrors.OnNext(Tuple.Create(documentInfo, e));
                        // consider there is no document to index if words can't be extracted from it.
                        return Observable.Empty<IndexedDocument>();
                    });
        }


#pragma warning disable 0420 // using ref volatile with Interlocked
        private void OnParsingStarted()
        {
            if (Interlocked.Exchange(ref runningParsers, 1) != 1)
                OnStateChanged();
        }

        private void OnParsingCompleted()
        {
            if (Interlocked.Exchange(ref runningParsers, 0) != 0)
                OnStateChanged();
        }
#pragma warning restore 0420



        void IDisposable.Dispose()
        {
            StopIndexing();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnStateChanged() { OnPropertyChanged("State");}
    }


    /// <summary>
    ///  Indicates the state of <see cref="Indexer"/> indexing
    /// </summary>
    public enum IndexingState
    {
        /// <summary>Indexing is stopped</summary>
        Stopped,
        /// <summary>Documents are being parsed and merged into the index</summary>
        Indexing,
        /// <summary>Document parsing and indexing is completed, now watching for changes in document source</summary>
        Watching,
        /// <summary>An error is happened during the indexing, which made further indexing being impossible.</summary>
        /// <remarks>The error can be obtained with the <see cref="Indexer.Error"/> property</remarks>
        Failed
    }
}
