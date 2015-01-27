using System;
using JetBrains.Annotations;

namespace Primitive.Text.Documents
{
    public class DocumentInfo : IEquatable<DocumentInfo>
    {
        public DocumentInfo([NotNull] string id, [NotNull] IDocumentSource source)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (source == null) throw new ArgumentNullException("source");

            Id = id;
            Source = source;
        }

        [NotNull]
        public string Id { get; private set; }

        [NotNull]
        public IDocumentSource Source { get; private set; }

        #region Equality members

        public bool Equals(DocumentInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Id.GetHashCode(); // ^ ((Source != null ? Source.GetHashCode() : 0) * 397);
            }
        }

        public static bool operator ==(DocumentInfo left, DocumentInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DocumentInfo left, DocumentInfo right)
        {
            return !Equals(left, right);
        }

        #endregion

        public override string ToString()
        {
            return string.Format("Document Id: {0}, Source: {1}", Id, Source);
        }
    }
}