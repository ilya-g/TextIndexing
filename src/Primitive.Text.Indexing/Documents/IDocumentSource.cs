using System;
using System.IO;

namespace Primitive.Text.Documents
{
    public interface IDocumentSource
    {
        StreamReader OpenDocument(DocumentInfo document);

        IObservable<DocumentInfo> FindAllDocuments();

        IObservable<DocumentInfo> ChangedDocuments();
    }
}