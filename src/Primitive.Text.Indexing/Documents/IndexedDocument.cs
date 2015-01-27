using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Documents
{
    public class IndexedDocument
    {
        public IndexedDocument([NotNull] DocumentInfo document, [NotNull] ISet<string> indexWords)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (indexWords == null) throw new ArgumentNullException("indexWords");
            Document = document;
            IndexWords = indexWords;
        }

        public DocumentInfo Document { get; private set; }
        public ISet<string> IndexWords { get; private set; } 
    }
}
