using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primitive.Text.Documents
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
