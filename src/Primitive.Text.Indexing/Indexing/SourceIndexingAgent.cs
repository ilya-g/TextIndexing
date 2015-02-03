﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Orchestrates and controls an individual document source indexing pipeline and exposes properties
    ///  describing the indexing state of the document source.
    /// </summary>
    public sealed class SourceIndexingAgent : IDisposable, INotifyPropertyChanged
    {
        private static readonly int maxConcurrentIndexing = 8;

        private volatile IDisposable subscription;
        private volatile int documentsParsed;
        private volatile int documentsFound;
        private volatile int documentsChanged;

        private volatile int runningParsers;
        private volatile Exception error;

        /// <summary>
        ///  The Indexer this <see cref="SourceIndexingAgent"/> belongs to.
        /// </summary>
        public Indexer Indexer { get; private set; }

        /// <summary>
        ///  Gets the <see cref="IDocumentSource"/> being indexed
        /// </summary>
        public IDocumentSource DocumentSource { get; private set; }

        /// <summary>
        ///  Gets the indexing state value
        /// </summary>
        /// <remarks>
        ///  The indexing state describes which activity this <see cref="SourceIndexingAgent"/> is busy with.
        ///  The returned value may reflect state changes with a certain amount of delay.
        /// </remarks>
        public SourceIndexingState State
        {
            get
            {
                return 
                    subscription == null ? SourceIndexingState.Stopped :
                    error != null ? SourceIndexingState.Failed :
                    runningParsers > 0 ? SourceIndexingState.Indexing :
                        SourceIndexingState.Watching;
            }
        }

        /// <summary>
        ///  Gets the exception value, describing the reason this instance is in the <see cref="SourceIndexingState.Failed"/> state
        /// </summary>
        /// <value>
        ///  In case if the <see cref="State"/> is <see cref="SourceIndexingState.Failed"/> the exception which has lead to this state,
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

        internal SourceIndexingAgent([NotNull] Indexer indexer, [NotNull] IDocumentSource source)
        {
            if (indexer == null) throw new ArgumentNullException("indexer");
            if (source == null) throw new ArgumentNullException("source");

            Indexer = indexer;
            DocumentSource = source;
        }

        /// <summary>
        ///  Begins the indexing of <see cref="DocumentSource"/>.
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
            lock (this)
            {
                if (subscription != null) return;

                subscription = 
                    Observable.Merge(
                        DocumentSource.FindAllDocuments()
                            .Buffer(TimeSpan.FromSeconds(0.5), 50)
                            .Where(changes => changes.Count > 0)
                            .Do(files => DocumentsFound += files.Count),
                        DocumentSource.WatchForChangedDocuments()
                            .Buffer(TimeSpan.FromSeconds(0.5))
                            .Where(changes => changes.Count > 0)
                            .Select(changes => changes.Distinct().ToList())
                            .Do(changes => DocumentsChanged += changes.Count))
                    .Select(files => files.ToObservable())
                    .Concat()
                    .Do(_ => OnParsingStarted())
                    .SelectMany(IndexDocument)
                    .Do(_ => OnParsingStarted())
                    .Select(d => Observable.FromAsync(() => Task.Run(() => Indexer.Index.Merge(d.Document, d.IndexWords))))
                    .Merge(maxConcurrentIndexing)
                    .Do(_ => DocumentsParsed += 1, ex => { this.Error = ex; OnStateChanged(); })
                    .Throttle(TimeSpan.FromSeconds(1))
                    .Subscribe(_ => OnParsingCompleted(), _ => { });;
            }
            OnStateChanged();
        }

        /// <summary>
        ///  Stops the indexing of <see cref="DocumentSource"/> and clears the <see cref="Error"/> if any.
        /// </summary>
        public void StopIndexing()
        {
            
            // Stop producing indexed documents
            lock (this)
            {
                if (subscription != null)
                    subscription.Dispose();
                subscription = null;
                Error = null;
            }
            OnStateChanged();
        }


        private IObservable<IndexedDocument> IndexDocument(DocumentInfo documentInfo)
        {
            var documentReader = Observable.Using(
                () => documentInfo.Source.OpenDocument(documentInfo),
                reader =>
                    reader != null
                        ? Indexer.StreamParser.ExtractWords(reader).Aggregate(
                            Indexer.CreateEmptyWordSet(),
                            (set, word) =>
                            {
                                set.Add(word);
                                return set;
                            })
                        : Observable.Return(Indexer.CreateEmptyWordSet()))
                    // consider file doesn't contain any words if access is denied
                    .Catch((UnauthorizedAccessException e) => Observable.Return(Indexer.CreateEmptyWordSet()));

            return RetryOn(documentReader, shouldRetry: e => e is IOException, retryTimes: 4, retryDelay: TimeSpan.FromSeconds(1))
                .Select(words => new IndexedDocument(documentInfo, words));
        }

        private static IObservable<T> RetryOn<T>(/*this*/ IObservable<T> source, Func<Exception, bool> shouldRetry, int retryTimes, TimeSpan retryDelay)
        {
            return source.Catch(
                (Exception e) => shouldRetry(e) && retryTimes > 0
                    ? RetryOn(source, shouldRetry, retryTimes - 1, retryDelay).DelaySubscription(retryDelay)
                    : Observable.Throw<T>(e));
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
    ///  Indicates the state of <see cref="SourceIndexingAgent"/> indexing
    /// </summary>
    public enum SourceIndexingState
    {
        /// <summary>Indexing is stopped</summary>
        Stopped,
        /// <summary>Documents are being parsed and merged into the index</summary>
        Indexing,
        /// <summary>Document parsing and indexing is completed, now watching for changes in document source</summary>
        Watching,
        /// <summary>An error is happened during the indexing, which lead to furthest indexing being impossible.</summary>
        /// <remarks>The error can be obtained with the <see cref="SourceIndexingAgent.Error"/> property</remarks>
        Failed
    }
}