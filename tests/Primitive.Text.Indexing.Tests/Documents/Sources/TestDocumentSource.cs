using System;
using System.IO;
using System.Linq;
using System.Text;
using Primitive.Text.Parsers;

namespace Primitive.Text.Documents.Sources
{
    public class TestDocumentSource : IDocumentSource
    {

        public static readonly TestDocumentSource Instance = new TestDocumentSource();

        public IObservable<DocumentInfo> FindAllDocuments()
        {
            throw new NotImplementedException();
        }

        public IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            throw new NotImplementedException();
        }

        public IObservable<string> ExtractDocumentWords(DocumentInfo document, IStreamParser streamParser)
        {
            throw new NotImplementedException();
        }
    }
}
