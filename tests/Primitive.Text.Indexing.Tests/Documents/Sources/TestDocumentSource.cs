using System;
using System.IO;
using System.Linq;
using System.Text;

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

        public IObservable<T> ReadDocumentText<T>(DocumentInfo document, Func<TextReader, IObservable<T>> documentReader)
        {
            throw new NotImplementedException();
        }
    }
}
