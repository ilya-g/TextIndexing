using System;
using System.IO;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public interface IDocumentSource
    {
        [CanBeNull]
        StreamReader OpenDocument(DocumentInfo document);

        IObservable<DocumentInfo> FindAllDocuments();

        IObservable<DocumentInfo> WatchForChangedDocuments();
    }
}