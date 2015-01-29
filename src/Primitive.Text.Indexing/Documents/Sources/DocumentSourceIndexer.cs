using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public sealed class DocumentSourceIndexer : IDisposable
    {
        private readonly IDisposable subscription;

        // TODO: Properties describing the state

        internal DocumentSourceIndexer([NotNull] IDocumentSource source, [NotNull] Func<DocumentInfo, Task<ISet<string>>> documentParser,
            [NotNull] IObserver<IndexedDocument> indexedDocumentsObserver)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (documentParser == null) throw new ArgumentNullException("documentParser");
            if (indexedDocumentsObserver == null) throw new ArgumentNullException("indexedDocumentsObserver");
            DocumentSource = source;

            subscription = source.FindAllDocuments()
                .Merge(source.WatchForChangedDocuments())
                .ObserveOn(Scheduler.Default)
                .SelectMany(documentParser, (document, indexWords) => new IndexedDocument(document, indexWords))
                .Subscribe(indexedDocumentsObserver);
        }

        public IDocumentSource DocumentSource { get; private set; }


        public void Dispose()
        {
            // Stop producing indexed documents 
            subscription.Dispose();
        }
    }
}
