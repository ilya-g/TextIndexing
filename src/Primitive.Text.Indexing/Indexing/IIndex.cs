using System;
using System.Collections.Generic;
using Primitive.Text.Documents;

namespace Primitive.Text.Indexing
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public interface IIndex
    {
        /// <summary>
        ///  String comparison type used to compare words being indexed
        /// </summary>
        StringComparison WordComparison { get; }
        IEnumerable<DocumentInfo> QueryDocuments(string word);
        IList<KeyValuePair<string, IEnumerable<DocumentInfo>>> QueryDocumentsStartsWith(string word);
        IList<KeyValuePair<string, IEnumerable<DocumentInfo>>> QueryDocumentsMatching(Func<string, bool> wordPredicate);
        /// <summary>
        ///  Gets all words that were placed in the index
        /// </summary>
        /// <returns>
        ///  Some words may have no associated documents after the index is updated
        /// </returns>
        IList<string> GetIndexedWords();

        IIndex Snapshot();

        /// <summary>
        ///  Merges document word index into this index
        /// </summary>
        /// <param name="documentInfo">Document to include in index</param>
        /// <param name="indexWords">Words to index document by</param>
        /// <remarks>
        ///  Merge is an atomic operation: the queries and snapshot operations either will see index before the merge or after the merge
        /// </remarks>
        void Merge(DocumentInfo documentInfo, IEnumerable<string> indexWords);

        /// <summary>
        ///  Removes from the index all documents matching the specified <paramref name="predicate"/>
        /// </summary>
        /// <param name="predicate">Predicate that defines the conditions of the documents to remove.</param>
        void RemoveDocumentsMatching(Func<DocumentInfo, bool> predicate);
    }
}