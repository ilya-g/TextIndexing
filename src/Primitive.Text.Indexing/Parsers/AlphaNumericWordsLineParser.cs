namespace Primitive.Text.Parsers
{
    /// <summary>
    ///  Represents <see cref="WordCharacterParser"/> that uses 
    /// <see cref="char.IsLetterOrDigit(char)"/> predicate to define word characters
    /// </summary>
    public class AlphaNumericWordsLineParser : WordCharacterParser
    {
        private AlphaNumericWordsLineParser() : base(char.IsLetterOrDigit) {}

        /// <summary>
        ///  The only instance of <see cref="AlphaNumericWordsLineParser"/>
        /// </summary>
        public static readonly AlphaNumericWordsLineParser Instance = new AlphaNumericWordsLineParser();
    }
}