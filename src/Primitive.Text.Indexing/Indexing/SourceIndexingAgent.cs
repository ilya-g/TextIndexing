using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;

namespace Primitive.Text.Indexing
{
    public sealed class SourceIndexingAgent : IDisposable, INotifyPropertyChanged
    {
        private static readonly int maxConcurrentIndexing = 8;

        private readonly Func<IDisposable> startIndexing;
        private volatile IDisposable subscription;
        private volatile int documentsParsed;
        private volatile int documentsFound;
        private volatile int documentsChanged;

        private volatile int runningParsers;
        private volatile Exception error;

        public IDocumentSource DocumentSource { get; private set; }

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

        public Exception Error
        {
            get { return error; }
            private set
            {
                error = value;
                OnPropertyChanged();
            }
        }

        public int DocumentsFound
        {
            get { return documentsFound; }
            private set
            {
                documentsFound = value;
                OnPropertyChanged();
            }
        }

        public int DocumentsParsed
        {
            get { return documentsParsed; }
            private set
            {
                documentsParsed = value;
                OnPropertyChanged();
            }
        }

        public int DocumentsChanged
        {
            get { return documentsChanged; }
            private set
            {
                documentsChanged = value;
                OnPropertyChanged();
            }
        }

        internal SourceIndexingAgent([NotNull] IDocumentSource source, [NotNull] Func<DocumentInfo, Task<ISet<string>>> documentParser,
            [NotNull] Action<IndexedDocument> indexedDocumentAction)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (documentParser == null) throw new ArgumentNullException("documentParser");
            if (indexedDocumentAction == null) throw new ArgumentNullException("indexedDocumentAction");

            DocumentSource = source;

            this.startIndexing = () =>
                Observable.Merge(
                    source.FindAllDocuments()
                        .Buffer(TimeSpan.FromSeconds(0.5), 50)
                        .Where(changes => changes.Count > 0)
                        .Do(files => DocumentsFound += files.Count), 
                    source.WatchForChangedDocuments()
                        .Buffer(TimeSpan.FromSeconds(0.5))
                        .Where(changes => changes.Count > 0)
                        .Select(changes => changes.Distinct().ToList())
                        .Do(changes => DocumentsChanged += changes.Count))
                .Select(files => files.ToObservable())
                .Concat()
                .Do(_ => OnParsingStarted())
                .Select(document =>
                    Observable.FromAsync(() => documentParser(document))
                        .Select(words => new IndexedDocument(document, words))
                    )
                .Merge()
                .Do(_ => OnParsingStarted())
                .Select(d => Observable.FromAsync(() => Task.Run(() => indexedDocumentAction(d))))
                .Merge(maxConcurrentIndexing)
                .Do(_ => DocumentsParsed += 1)
                .Throttle(TimeSpan.FromSeconds(1))
                .Do(_ => OnParsingCompleted())
                .Subscribe(_ => { },
                    error => { this.Error = error; OnStateChanged(); });
        }

        public void StartIndexing()
        {
            lock (this)
            {
                if (subscription != null) return;
                subscription = startIndexing();
            }
            OnStateChanged();
        }

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

        //private static Func<Task<T>> WatchRunning<T>(Func<Task<T>> taskSource, Action onStarted, Action onCompleted)
        //{
        //    return () =>
        //    {
        //        onStarted();
        //        return taskSource().ContinueWith(t =>
        //        {
        //            onCompleted();
        //            return t.Result;
        //        });
        //    };
        //} 


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

    public enum SourceIndexingState
    {
        Stopped,
        Indexing,
        Watching,
        Failed
    }
}
