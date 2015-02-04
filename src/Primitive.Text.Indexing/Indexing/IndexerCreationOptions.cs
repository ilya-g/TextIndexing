using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Specifies the options to be used in <see cref="Indexer.Create(IndexerCreationOptions)" /> method.
    /// </summary>
    public sealed class IndexerCreationOptions
    {
        /// <summary>
        ///  Creates <see cref="IndexerCreationOptions"/> instance with all values set to their defaults
        /// </summary>
        public IndexerCreationOptions()
        {
            WordComparison = StringComparison.OrdinalIgnoreCase;
            IndexLocking = IndexLocking.NoLocking;
        }

        /// <summary>
        ///  Gets or sets string comparison type used to compare word in index
        /// </summary>
        /// <remarks>
        ///  Default value is <see cref="StringComparison.OrdinalIgnoreCase"/>
        /// </remarks>
        public StringComparison WordComparison { get; set; }

        /// <summary>
        ///  Gets or sets the locking mode, defining which index implementation is to be used
        /// </summary>
        /// <remarks>
        ///  Default value is <see cref="Indexing.IndexLocking.NoLocking"/>.
        ///  See the <see cref="Indexing.IndexLocking"/> for detailed description of index locking modes.
        /// </remarks>
        public IndexLocking IndexLocking { get; set; }

        /// <summary>
        ///  Gets or sets <see cref="ILineParser"/> instance to be used with the default <see cref="LineStreamParser"/> implemetation,
        ///  when the <see cref="StreamParser"/> is not specified
        /// </summary>
        /// <remarks>
        ///  <para>Cannot be specified simultaneously with the <see cref="StreamParser"/>.</para>
        ///  <para>When the both <see cref="LineParser"/> and <see cref="StreamParser"/> are not specified, 
        ///  <see cref="RegexLineParser.Default"/> value is used.</para>
        /// </remarks>
        public ILineParser LineParser { get; set; }


        /// <summary>
        ///  Gets or sets <see cref="IStreamParser" /> to extract index words from document streams
        /// </summary>
        /// <remarks>
        ///  <para>Cannot be specified simultaneously with the <see cref="LineParser"/>.</para>
        ///  When this value is not specified the default implementation of <see cref="LineStreamParser"/> is created with the value of 
        /// <see cref="LineParser"/> as line parser.
        /// </remarks>
        public IStreamParser StreamParser { get; set; }

        /// <summary>
        ///  Creates the index using the options specified in this instance
        /// </summary>
        /// <remarks>
        ///  Used in <see cref="Indexer"/> to create the <see cref="IIndex"/> implementation
        /// </remarks>
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

    /// <summary>
    ///  Specifies which <see cref="IIndex"/> implementation and locking strategy to be used.
    /// </summary>
    /// <remarks>
    ///  Default value is <see cref="NoLocking"/>, 
    /// </remarks>
    public enum IndexLocking
    {
        /// <summary>
        ///  Slow modification operations, instantaneous read operations: both querying and snapshots
        /// </summary>
        /// <remarks>
        /// <para>
        ///  Incurs no locking on read operations such as query or snapshot.
        ///  Uses exclusive lock for write operations.
        /// </para>
        /// <para>
        ///  Good for situations when index queries significantly exceed index merges 
        ///  or when there are many queries, requiring index snapshots to be made
        /// </para>
        /// </remarks>
        NoLocking,

        /// <summary>
        ///  Fastest modification operations, blocking read, slow snapshots
        /// </summary>
        /// <remarks>
        /// <para>Uses exclusive locking for both read and modification operations.</para>
        /// <para>Can be used when index queries are relatively seldom compared to index merges</para>
        /// </remarks>
        Exclusive,
        
        /// <summary>
        ///  Fast modification operations, read operations don't block each other, slow snapshots
        /// </summary>
        /// <remarks>
        ///  Uses exclusive locking for modification operations and shared locking for read operations.
        ///  When there are no writes, queries are as fast as in <see cref="NoLocking"/> mode,
        ///  snapshots is as slow as in <see cref="Exclusive"/> and merges are a little bit slower than
        ///  in <see cref="Exclusive"/>, but faster than in <see cref="NoLocking"/> mode.
        /// </remarks>
        ReadWrite
    }
}
