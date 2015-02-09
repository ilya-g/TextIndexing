using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Primitive.Text.Indexing.Internal;
using Primitive.Text.Parsers;

namespace Primitive.Text.Indexing
{
    /// <summary>
    ///  Specifies the options to be used in <see cref="IndexerSet.Create(IndexerCreationOptions)" /> method.
    /// </summary>
    public sealed class IndexerCreationOptions
    {
        /// <summary>
        ///  Creates <see cref="IndexerCreationOptions"/> instance with all values set to their defaults
        /// </summary>
        public IndexerCreationOptions()
        {
            WordComparison = StringComparison.OrdinalIgnoreCase;
            IndexLocking = IndexLocking.ReadWrite;
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
        ///  Default value is <see cref="Indexing.IndexLocking.ReadWrite"/>.
        ///  See the <see cref="Indexing.IndexLocking"/> for detailed description of index locking modes.
        /// </remarks>
        public IndexLocking IndexLocking { get; set; }

        /// <summary>
        ///  Gets or sets <see cref="ILineParser"/> instance to be used with the default <see cref="LineTextParser"/> implemetation,
        ///  when the <see cref="TextParser"/> is not specified
        /// </summary>
        /// <remarks>
        ///  <para>Cannot be specified simultaneously with the <see cref="TextParser"/>.</para>
        ///  <para>When the both <see cref="LineParser"/> and <see cref="TextParser"/> are not specified, 
        ///  an instance of <see cref="AlphaNumericWordsLineParser"/> is used.</para>
        /// </remarks>
        /// <seealso cref="GetDefaultStreamParser"/>
        public ILineParser LineParser { get; set; }


        /// <summary>
        ///  Gets or sets <see cref="ITextParser" /> to extract index words from document streams
        /// </summary>
        /// <remarks>
        ///  <para>Cannot be specified simultaneously with the <see cref="LineParser"/>.</para>
        ///  When this value is not specified the default implementation of <see cref="LineTextParser"/> is created with the value of 
        /// <see cref="LineParser"/> as line parser.
        /// </remarks>
        /// <seealso cref="GetDefaultStreamParser"/>
        public ITextParser TextParser { get; set; }

        /// <summary>
        ///  Creates the index using the options specified in this instance
        /// </summary>
        /// <remarks>
        ///  Used in <see cref="IndexerSet"/> to create the <see cref="IIndex"/> implementation
        /// </remarks>
        public IIndex CreateIndex()
        {
            switch (IndexLocking)
            {
                case IndexLocking.NoLocking:
                    return new ImmutableIndex(WordComparison);
#pragma warning disable 618 // obsolete usage
                case IndexLocking.Exclusive:
#pragma warning restore 618
                    return new LockingIndex(WordComparison, new LockingStrategy.Exclusive());
                case IndexLocking.ReadWrite:
                    return new LockingIndex(WordComparison, new LockingStrategy.PrioritizedReadWrite());
                default:
                    throw new ArgumentOutOfRangeException(string.Format("IndexLocking option value '{0}' is out of range", IndexLocking), "IndexLocking");
            }
        }


        /// <summary>
        ///  Gets the instance of <see cref="ITextParser"/> based on values of 
        ///  <see cref="TextParser"/> and <see cref="LineParser"/> properties.
        /// </summary>
        /// <returns>
        ///  The value of <see cref="TextParser"/> if it's specified.
        ///  Otherwise a new instance of <see cref="LineTextParser"/> with its 
        ///  <see cref="LineTextParser.LineParser"/> initialized with the value of <see cref="LineParser"/> property.
        ///  If the latter is not set, the <see cref="AlphaNumericWordsLineParser"/> is used.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///  Both <see cref="TextParser"/> and <see cref="LineParser"/> are not null
        /// </exception>
        public ITextParser GetDefaultStreamParser()
        {
            if (TextParser != null && LineParser != null)
                throw new InvalidOperationException("StreamParser and LineParser cannot be specified simulaneosly");

            return TextParser ?? new LineTextParser(LineParser ?? AlphaNumericWordsLineParser.Instance);
        }

    }

    /// <summary>
    ///  Specifies which <see cref="IIndex"/> implementation and locking strategy to be used.
    /// </summary>
    /// <remarks>
    ///  Default value is <see cref="ReadWrite"/>, 
    /// </remarks>
    public enum IndexLocking
    {
        /// <summary>
        ///  No-locking reads, exclusive writes.
        ///  Slow modification operations, instantaneous read operations: both querying and snapshots.
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
        ///  Shared reads, exclusive writes.
        ///  Fastest modification operations, read operations don't block each other and have priority over writes, slow snapshots.
        /// </summary>
        /// <remarks>
        ///  Uses exclusive locking for modification operations and shared locking for read operations.
        ///  When there are no writes, queries are as fast as in <see cref="NoLocking"/> mode,
        ///  snapshots is as slow as in <see cref="Exclusive"/> and merges are a little bit faster than
        ///  in <see cref="Exclusive"/>.
        /// </remarks>
        ReadWrite,


        /// <summary>
        ///  Exclusive both reads and writes.
        ///  Fast modification operations, blocking reads, slow snapshots.
        /// </summary>
        /// <remarks>
        /// <para>Uses exclusive locking for both read and modification operations.</para>
        /// <para>Can be used when index queries are relatively seldom compared to index merges</para>
        /// </remarks>
        [Obsolete("Use ReadWrite instead for better perfomance")]
        Exclusive,
        
    }
}
