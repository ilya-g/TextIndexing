using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Provides <see cref="ILineParser"/> implementation, that uses <see cref="Regex"/> pattern
    ///  to extract the individual words from a line of text
    /// </summary>
    public sealed class RegexLineParser : ILineParser
    {
        /// <summary>
        ///  The default instance of <see cref="RegexLineParser"/> initialized with <c>\w+</c> pattern to match words
        /// </summary>
        public static readonly RegexLineParser Default = new RegexLineParser(new Regex(@"\w+", RegexOptions.Compiled));

        /// <summary>
        ///  Initializes a new <see cref="RegexLineParser"/> instance with the specified <paramref name="wordPattern"/>
        /// </summary>
        /// <param name="wordPattern">A <see cref="Regex"/> pattern to match words</param>
        public RegexLineParser([NotNull] Regex wordPattern)
        {
            if (wordPattern == null) throw new ArgumentNullException("wordPattern");
            WordPattern = wordPattern;
        }

        /// <summary>
        ///  Gets the <see cref="Regex"/> pattern to match words
        /// </summary>
        public Regex WordPattern { get; private set; }

        /// <summary>
        ///  Extracts words from a <paramref name="line"/>
        /// </summary>
        /// <param name="line">A line of content text to extract words from</param>
        /// <returns>An enumerable sequence with extracted words</returns>
        public IEnumerable<string> ExtractWords([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");

            return WordPattern.Matches(line).Cast<Match>().Select(match => match.Value);
        }
    }
}