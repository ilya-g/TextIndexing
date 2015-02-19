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
        public abstract TextReader OpenDocument([NotNull] DocumentInfo document);


        /// <summary>
        ///  Reads the <paramref name="document"/> with the specified progressive <paramref name="documentReader"/>
        /// </summary>
        /// <typeparam name="T">Type of data elements being read from the document</typeparam>
        /// <param name="document">The document from this source</param>
        /// <param name="documentReader">
        ///  A function, that given the document <see cref="TextReader"/> extracts the elements of the <typeparamref name="T"/> type from it.
        /// </param>
        /// <returns>
        ///  Returns an observable sequence returned by <paramref name="documentReader"/>.
        /// </returns>
        /// <remarks>
        /// This method can be overriden in derived classes to add some behavior to the returned observable sequence
        /// </remarks>
        public virtual IObservable<T> ReadDocumentText<T>([NotNull] DocumentInfo document, [NotNull] Func<TextReader, IObservable<T>> documentReader)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (documentReader == null) throw new ArgumentNullException("documentReader");
            EnsureOwnDocument(document);

            return Observable.Using(
                () => OpenDocument(document),
                reader =>
                    reader != null
                        ? documentReader(reader)
                        : Observable.Empty<T>());
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