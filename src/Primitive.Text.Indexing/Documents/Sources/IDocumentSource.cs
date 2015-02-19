using System;
using System.IO;
using JetBrains.Annotations;
using Primitive.Text.Parsers;

namespace Primitive.Text.Documents.Sources
{
    /// <summary>
    /// Represents a document source, which can provide a list of available documents it contains,
    /// watch for documents being changed and notify about it, and extract words to index from the specified document
    /// </summary>
    public interface IDocumentSource
    {
        /// <summary>
        ///  Enumerates all documents in the source available for indexing
        /// </summary>
        /// <returns>
        ///  Returns an observable sequence that pushes all available documents and then completes
        ///  or fails in case if there was an unrecoverable error during enumeration.
        /// </returns>
        IObservable<DocumentInfo> FindAllDocuments();

        /// <summary>
        /// Starts watching for any changes of documents in this source
        /// </summary>
        /// <returns>
        /// Returns an observable sequence of changed documents. That sequence after being subscribed, can only
        /// complete with fail, if there was an unrecoverable error making further watching impossible
        /// </returns>
        IObservable<DocumentInfo> WatchForChangedDocuments();

        /// <summary>
        ///  Reads the <paramref name="document"/> with the specified progressive <paramref name="documentReader"/>
        /// </summary>
        /// <typeparam name="T">Type of data elements being read from the document</typeparam>
        /// <param name="document">The document from this source</param>
        /// <param name="documentReader">
        ///  A function, that given the document <see cref="TextReader"/> extracts the elements of the <typeparamref name="T"/> type from it.
        /// </param>
        /// <returns>
        ///  Returns an empty sequence if the document is not found.
        ///  Returns a failed empty sequence if the document cannot be opened.
        ///  Returns an observable sequence returned by <paramref name="documentReader"/>, that being subscribed to
        ///  pushes all the extracted data elements from the document and then completes.
        ///  This sequence can also complete with fail, if there was an error reading the document.
        /// </returns>
        IObservable<T> ReadDocumentText<T>([NotNull] DocumentInfo document, [NotNull] Func<TextReader, IObservable<T>> documentReader);
    }
}