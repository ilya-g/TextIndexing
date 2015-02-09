using System;
using System.IO;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Primitive.Text.Parsers;

namespace Primitive.Text.Documents.Sources
{

    /// <summary>
    ///  Provides the base implementation for classes implementing <see cref="IDocumentSource"/> interface
    /// </summary>
    public abstract class DocumentSourceBase : IDocumentSource
    {
        /// <summary>
        ///  When overriden in the derived class opens the <paramref name="document"/> 
        ///  and returns the <see cref="TextReader"/> to read its content
        /// </summary>
        /// <param name="document">Document to open</param>
        /// <returns>
        ///  <see cref="TextReader"/> to read text content.
        ///  Can return null if the document exists no more in the source and should be removed from the index
        /// </returns>
        [CanBeNull]
        public abstract TextReader OpenDocument(DocumentInfo document);


        /// <summary>
        ///  Extracts words to index from the <paramref name="document"/> with the specified <paramref name="textParser"/>
        /// </summary>
        /// <param name="document">The document from this source</param>
        /// <param name="textParser">The parser to be used to extract words from the document stream</param>
        /// <returns>
        /// Returns an observable sequence of document words, that being subscribed to
        /// pushes all words from the document and then completes. This sequence also complete with fail, if there was
        /// an error opening or reading the document.
        /// </returns>
        /// <remarks>
        /// This method can be overriden in derived classes to add some behavior to the returned observable sequence
        /// </remarks>
        public virtual IObservable<string> ExtractDocumentWords(DocumentInfo document, ITextParser textParser)
        {
            EnsureOwnDocument(document);

            return Observable.Using(
                () => OpenDocument(document),
                reader =>
                    reader != null
                        ? textParser.ExtractWords(reader)
                        : Observable.Empty<string>());
        }

        /// <summary>
        ///  Validates the <paramref name="document"/> was originated from the current <see cref="DocumentSourceBase"/> instance.
        /// </summary>
        /// <param name="document">The document to check</param>
        /// <exception cref="ArgumentException">The document was originated </exception>
        protected virtual void EnsureOwnDocument([NotNull] DocumentInfo document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (document.Source != this)
                throw new ArgumentException("Document was not originated from this source", "document"); // TODO: Details
        }


        /// <summary>
        ///  When overriden in derived class, enumerates all documents in the source available for indexing
        /// </summary>
        public abstract IObservable<DocumentInfo> FindAllDocuments();

        /// <summary>
        /// When overriden in derived class, starts watching for any changes in documents
        /// </summary>
        public abstract IObservable<DocumentInfo> WatchForChangedDocuments();
    }
}