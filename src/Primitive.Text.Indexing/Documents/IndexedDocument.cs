using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Documents
{
    /// <summary>
    ///  Represents the document with the set of words extracted from it
    /// </summary>
    public sealed class IndexedDocument
    {
        /// <summary>
        ///  Creates <see cref="IndexedDocument"/> instance.
        /// </summary>
        /// <param name="document">A document that was indexed</param>
        /// <param name="indexWords">A set of words extracted from the document</param>
        public IndexedDocument([NotNull] DocumentInfo document, [NotNull] ISet<string> indexWords)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (indexWords == null) throw new ArgumentNullException("indexWords");
            Document = document;
            IndexWords = indexWords;
        }

        /// <summary>
        ///  Gets the <see cref="DocumentInfo"/>, identifying the document
        /// </summary>
        [NotNull]
        public DocumentInfo Document { get; private set; }

        /// <summary>
        ///  Gets the set of words, extracted from the <see cref="Document"/>
        /// </summary>
        [NotNull]
        public ISet<string> IndexWords { get; private set; } 
    }
}
