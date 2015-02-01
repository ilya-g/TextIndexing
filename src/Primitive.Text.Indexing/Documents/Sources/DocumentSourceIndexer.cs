using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public sealed class DocumentSourceIndexer : IDisposable, INotifyPropertyChanged
    {
        private static readonly int maxConcurrentIndexing = 8;

        private readonly IDisposable subscription;
        private volatile int documentsParsed;
        private volatile int documentsFound;
        private volatile int documentsChanged;

        public IDocumentSource DocumentSource { get; private set; }

        public DocumentSourceIndexerState State { get; private set; }

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

        internal DocumentSourceIndexer([NotNull] IDocumentSource source, [NotNull] Func<DocumentInfo, Task<ISet<string>>> documentParser,
            [NotNull] IObserver<IndexedDocument> indexedDocumentsObserver)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (documentParser == null) throw new ArgumentNullException("documentParser");
            if (indexedDocumentsObserver == null) throw new ArgumentNullException("indexedDocumentsObserver");

            DocumentSource = source;

            subscription = source.FindAllDocuments().Do(_ => DocumentsFound += 1)
                .Merge(source.WatchForChangedDocuments().Do(_ => DocumentsChanged += 1))
                .Select(document => Observable.FromAsync(() => documentParser(document)).Select(words => new IndexedDocument(document, words)))
                .Merge(maxConcurrentIndexing)
                .Do(_ => DocumentsParsed += 1)
                .Subscribe(indexedDocumentsObserver);
        }

        public void StopIndexing()
        {
            // Stop producing indexed documents 
            subscription.Dispose();
        }

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
    }

    public enum DocumentSourceIndexerState
    {
        Indexing,
        Watching,
        Failed
    }
}
