using System.Collections.Generic;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Represents a parser that can extract words from a single line of text
    /// </summary>
    /// <remarks>
    ///  Implementation of this interface can used by <see cref="LineStreamParser"/> to split content lines to words
    /// </remarks>
    public interface ILineParser
    {

        /// <summary>
        ///  Extracts words from a <paramref name="line"/>
        /// </summary>
        /// <param name="line">A line of content text to extract words from</param>
        /// <returns>An enumerable sequence with extracted words</returns>
        IEnumerable<string> ExtractWords([NotNull] string line);
    }
}