using System;
using System.IO;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public abstract class AbstractDocumentSource : IDocumentSource
    {
        public abstract StreamReader OpenDocument(DocumentInfo document);

        protected virtual void EnsureOwnDocument([NotNull] DocumentInfo document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (document.Source != this)
                throw new ArgumentException("Document was not originated from this source", "document"); // TODO: Details
        }

        public abstract IObservable<DocumentInfo> FindAllDocuments();
        public abstract IObservable<DocumentInfo> ChangedDocuments();
    }
}