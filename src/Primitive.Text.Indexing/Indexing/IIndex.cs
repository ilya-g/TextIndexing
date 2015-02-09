using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Primitive.Text.Documents;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Represents an index data structure to store one-way relationships 
    ///  between index words and documents, containing that words.
    ///  Provides methods to get documents by index words.
    /// </summary>
    public interface IReadOnlyIndex
    {
        /// <summary>
        ///  String comparison type used to compare words being indexed
        /// </summary>
        StringComparison WordComparison { get; }

        /// <summary>
        ///  Queries the index for the exact <paramref name="word"/> 
        ///  and returns the <see cref="WordDocuments"/> structure, 
        ///  containing the documents associtated with that word
        /// </summary>
        /// <param name="word">The word to query index for</param>
        /// <returns>
        ///  <see cref="WordDocuments"/> structure, containing the word that was queried
        ///  and the documents from the index associtated with that word
        /// </returns>
        WordDocuments GetExactWord([NotNull] string word);

        /// <summary>
        /// Queries the index for all words starting with the specified <paramref name="wordBeginning"/> part 
        /// and returns the collection of <see cref="WordDocuments"/> structures, 
        /// containing the documents associtated with each matching word
        /// </summary>
        /// <param name="wordBeginning">Word beginning part to match index words with</param>
        /// <returns>
        /// List of <see cref="WordDocuments"/> structures, 
        /// containing the documents associtated with each matching word
        /// </returns>
        /// <remarks>
        ///  Some implementations may fall back to <see cref="GetWordsMatching"/> 
        ///  with <see cref="string.StartsWith(string)"/> as a predicate, 
        ///  and others may have more efficient implementation for the prefix search.
        /// </remarks>
        IList<WordDocuments> GetWordsStartWith([NotNull] string wordBeginning);

        /// <summary>
        /// Queries the index for all words matching the specified <paramref name="wordPredicate"/> 
        /// and returns the collection of <see cref="WordDocuments"/> structures, 
        /// containing the documents associtated with each matching word
        /// </summary>
        /// <param name="wordPredicate">Predicate defining which words to match</param>
        /// <returns>
        /// List of <see cref="WordDocuments"/> structures, 
        /// containing the documents associtated with each matching word
        /// </returns>
        IList<WordDocuments> GetWordsMatching([NotNull] Func<string, bool> wordPredicate);

        /// <summary>
        ///  Gets all words that were placed in the index
        /// </summary>
        /// <returns>
        ///  Words without associated documents should not be returned
        /// </returns>
        IList<string> GetIndexedWords();
    }




    /// <summary>
    ///  Represents an index data structure to store one-way relationships 
    ///  between index words and documents, containing that words.
    ///  Extends <see cref="IReadOnlyIndex"/> with methods to merge index words of new documents to this index
    /// </summary>
    /// <threadsafety instance="yes" />
    /// <remarks>
    ///  Note to implementors: instances of this interface should be thread-safe, providing the consistent state
    ///  to the read operations, even if the write operations, such as <see cref="Merge"/> or <see cref="RemoveDocumentsMatching"/> 
    ///  are in progress.
    /// </remarks>
    public interface IIndex : IReadOnlyIndex
    {
        /// <summary>
        ///  Creates the copy of this index, that will remain unchanged even if this instance is changed later.
        /// </summary>
        /// <returns><see cref="IReadOnlyIndex"/> instance that holds the frozen snapshot of this index</returns>
        /// <remarks>
        /// Snapshot of index can be used to make several requests, requiring the consistent state that do not changes 
        /// during the serie of that requests
        /// </remarks>
        IReadOnlyIndex Snapshot();

        /// <summary>
        ///  Merges document word index into this index
        /// </summary>
        /// <param name="document">Document to include in index</param>
        /// <param name="indexWords">Words to index document by</param>
        /// <remarks>
        /// <para>
        ///  Merge should be implemented as an atomic operation: queries and snapshot operations 
        ///  either will see index before the merge or after the merge.
        /// </para>
        /// <para>
        /// When the <paramref name="document"/> is already included in this index, 
        /// its old index words are removed from index before merging new index words.
        /// </para>
        /// </remarks>
        void Merge([NotNull] DocumentInfo document, [NotNull] IEnumerable<string> indexWords);

        /// <summary>
        ///  Removes from the index all documents matching the specified <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">Predicate that defines the conditions of the documents to remove.</param>
        /// <remarks>
        ///  This method should be implemented as an atomic operation.
        /// </remarks>
        void RemoveDocumentsMatching(Func<DocumentInfo, bool> predicate);
    }
}