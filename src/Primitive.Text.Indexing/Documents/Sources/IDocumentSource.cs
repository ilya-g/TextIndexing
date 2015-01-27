using System;
using System.IO;

namespace Primitive.Text.Documents.Sources
{
    public interface IDocumentSource
    {
        StreamReader OpenDocument(DocumentInfo document);

        IObservable<DocumentInfo> FindAllDocuments();

        IObservable<DocumentInfo> ChangedDocuments();
    }
}