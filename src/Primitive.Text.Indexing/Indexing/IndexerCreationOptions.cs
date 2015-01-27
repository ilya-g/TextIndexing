using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    public class IndexerCreationOptions
    {
        public IndexerCreationOptions()
        {
            WordComparison = StringComparison.OrdinalIgnoreCase;
            IndexLocking = IndexLocking.NoLocking;
        }
        public StringComparison WordComparison { get; set; }
        public IndexLocking IndexLocking { get; set; }
        public ILineParser LineParser { get; set; }

        public IStreamParser StreamParser { get; set; }

        public IIndex CreateIndex()
        {
            switch (IndexLocking)
            {
                case IndexLocking.NoLocking:
                    return new ImmutableIndex(WordComparison);
                case IndexLocking.Exclusive:
                    return new LockingIndex(WordComparison, new LockingStrategy.Exclusive());
                case IndexLocking.ReadWrite:
                    return new LockingIndex(WordComparison, new LockingStrategy.ReadWrite());
                default:
                    throw new ArgumentOutOfRangeException(string.Format("IndexLocking option value '{0}' is out of range", IndexLocking), "IndexLocking");
            }
        }

    }

    public enum IndexLocking
    {
        NoLocking,
        Exclusive,
        ReadWrite
    }
}
