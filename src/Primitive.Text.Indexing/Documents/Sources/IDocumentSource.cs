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
        ///  Extracts words to index from the <paramref name="document"/> with the specified <paramref name="streamParser"/>
        /// </summary>
        /// <param name="document">The document from this source</param>
        /// <param name="streamParser">The parser to be used to extract words from the document stream</param>
        /// <returns>
        /// Returns an observable sequence of document words, that being subscribed to
        /// pushes all words from the document and then completes. 
        /// This sequence can also complete with fail, if there was  an error opening or reading the document.
        /// </returns>
        IObservable<string> ExtractDocumentWords([NotNull] DocumentInfo document, [NotNull] IStreamParser streamParser);
    }
}