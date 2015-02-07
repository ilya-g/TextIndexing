using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Provides <see cref="ILineParser"/> implementation, that 
    ///  extracts consecutive word characters as words from a line of text.
    /// </summary>
    /// <seealso cref="AlphaNumericWordsLineParser"/>
    /// <seealso cref="PunctuationSplittingLineParser"/>
    public class WordCharacterParser : ILineParser
    {
        private readonly Func<char, bool> isWordCharacter;

        /// <summary>
        ///  Initializes an instance of <see cref="WordCharacterParser"/> with the specified predicate <paramref name="isWordCharacter"/>
        /// </summary>
        /// <param name="isWordCharacter">A predicate that classifies a character as word or non-word</param>
        public WordCharacterParser([NotNull] Func<char, bool> isWordCharacter)
        {
            if (isWordCharacter == null) throw new ArgumentNullException("isWordCharacter");

            this.isWordCharacter = isWordCharacter;
        }

        /// <summary>
        ///  Extracts words from a <paramref name="line"/>
        /// </summary>
        /// <param name="line">A line of content text to extract words from</param>
        /// <returns>An enumerable sequence with extracted words</returns>
        public IEnumerable<string> ExtractWords(string line)
        {
            if (line == null) throw new ArgumentNullException("line");

            return ExtractWordsImpl(line);
        }

        private IEnumerable<string> ExtractWordsImpl(string line)
        {
            int lastPosition = 0;
            int length = line.Length;
            int wordLength;
            for (int i = 0; i < length; i++)
            {
                if (!isWordCharacter(line[i]))
                {
                    wordLength = i - lastPosition;
                    if (wordLength > 0)
                        yield return line.Substring(lastPosition, wordLength);
                    lastPosition = i + 1;
                }
            }
            wordLength = length - lastPosition;
            if (wordLength > 0)
                yield return line.Substring(lastPosition, wordLength);
        }
    }
}