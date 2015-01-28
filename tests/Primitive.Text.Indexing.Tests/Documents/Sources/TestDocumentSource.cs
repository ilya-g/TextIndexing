using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Primitive.Text.Documents.Sources
{
    public class TestDocumentSource : IDocumentSource
    {

        public static readonly TestDocumentSource Instance = new TestDocumentSource();

        public StreamReader OpenDocument(DocumentInfo document)
        {
            throw new NotImplementedException();
        }

        public IObservable<DocumentInfo> FindAllDocuments()
        {
            throw new NotImplementedException();
        }

        public IObservable<DocumentInfo> ChangedDocuments()
        {
            throw new NotImplementedException();
        }
    }
}
