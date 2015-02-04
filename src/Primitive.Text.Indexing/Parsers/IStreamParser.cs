using System;
using System.IO;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Represents a parser that can extract words from a document content read with a <see cref="TextReader"/>
    /// </summary>
    public interface IStreamParser
    {
        /// <summary>
        ///  Extracts words from a document content read with <paramref name="sourceReader"/> and returns them as an observable sequence
        /// </summary>
        /// <param name="sourceReader">The reader to read the document content from</param>
        /// <returns>Observable sequence with a document words, one by one</returns>
        IObservable<string> ExtractWords([NotNull] TextReader sourceReader);
    }
}