using System;
using JetBrains.Annotations;
using Primitive.Text.Documents.Sources;

namespace Primitive.Text.Documents
{

    /// <summary>
    ///  Provides basic information about the document being indexed: its identifier, required to find document later
    ///  and the source it was originated from
    /// </summary>
    public class DocumentInfo : IEquatable<DocumentInfo>
    {
        /// <summary>
        ///  Initializes a new instance of <see cref="DocumentInfo"/> with the specified
        ///  <paramref name="id"/> and the <paramref name="source"/> it was originated from
        /// </summary>
        /// <param name="id">Identifier of the document</param>
        /// <param name="source">Source of the document</param>
        public DocumentInfo([NotNull] string id, [NotNull] IDocumentSource source)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (source == null) throw new ArgumentNullException("source");

            Id = id;
            Source = source;
        }

        /// <summary>
        ///  Gets the identifier of this document
        /// </summary>
        /// <remarks>
        ///  Documents with the same identifier are threated as equal.
        ///  Examples of document identifier:
        ///   - file name for file
        ///   - url for internet document
        /// </remarks>
        [NotNull]
        public string Id { get; private set; }


        /// <summary>
        ///  Gets the source this document was originated from
        /// </summary>
        [NotNull]
        public IDocumentSource Source { get; private set; }

        #region Equality members

        /// <summary>
        /// Indicates whether the current document is equal to another object of the same type.
        /// </summary>
        /// <param name="other">A document to compare with this document.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <remarks>
        /// Only the document <see cref="Id"/> is considered for comparison
        /// </remarks>
        public bool Equals(DocumentInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="DocumentInfo"/> instance.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current document; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current document. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentInfo) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            //unchecked
            {
                return Id.GetHashCode(); // ^ ((Source != null ? Source.GetHashCode() : 0) * 397);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public static bool operator ==(DocumentInfo left, DocumentInfo right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public static bool operator !=(DocumentInfo left, DocumentInfo right)
        {
            return !Equals(left, right);
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Document Id: {0}, Source: {1}", Id, Source);
        }
    }
}