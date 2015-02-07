using System;
using System.Linq;
using System.Text;

namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Represents <see cref="WordCharacterParser"/> that threats any non-(punctuation or whitespace) character
    ///  as a word character
    /// </summary>
    public sealed class PunctuationSplittingLineParser : WordCharacterParser
    {
        private PunctuationSplittingLineParser() : base(IsNotPunctuationOrWhitespace) {}

        private static bool IsNotPunctuationOrWhitespace(char c)
        {
            return !(char.IsPunctuation(c) || char.IsWhiteSpace(c));
        }

        /// <summary>
        ///  The only instance of <see cref="PunctuationSplittingLineParser"/>
        /// </summary>
        public static readonly PunctuationSplittingLineParser Instance = new PunctuationSplittingLineParser();


    }
}
