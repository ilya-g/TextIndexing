using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Primitive.Text.Documents
{
    /// <summary>
    ///  Represents the collection of documents associated with the specific word
    /// </summary>
    public struct WordDocuments : IGrouping<string, DocumentInfo>, IReadOnlyCollection<DocumentInfo>
    {
        private readonly string word;
        private readonly IReadOnlyCollection<DocumentInfo> documents;

        internal WordDocuments([CanBeNull] string word, [NotNull] IReadOnlyCollection<DocumentInfo> documents)
        {
            if (documents == null) throw new ArgumentNullException("documents");
            this.word = word;
            this.documents = documents;
        }
        /// <summary>
        ///  Gets the word that all the documents have
        /// </summary>
        public string Word { get { return word; } }

        string IGrouping<string, DocumentInfo>.Key { get { return word; } }

        /// <summary>
        ///  Returns an enumerator that iterates through a collection.
        /// </summary>
        public IEnumerator<DocumentInfo> GetEnumerator() { return (documents ?? Enumerable.Empty<DocumentInfo>()).GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>
        /// Gets the number of documents associated with the word
        /// </summary>
        public int Count { get { return documents != null ? documents.Count : 0; } }
    }
}