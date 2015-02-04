using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primitive.Text.Parsers
{
    public class RegexLineParser : ILineParser
    {
        public static readonly RegexLineParser Default = new RegexLineParser(new Regex(@"\w+", RegexOptions.Compiled));

        public RegexLineParser(string wordPattern) : this(new Regex(wordPattern))
        {
        }

        public RegexLineParser(Regex wordPattern)
        {
            WordPattern = wordPattern;
        }

        public Regex WordPattern { get; private set; }

        public IEnumerable<string> ExtractWords(string line)
        {
            if (line == null) throw new ArgumentNullException("line");

            return WordPattern.Matches(line).Cast<Match>().Select(match => match.Value);
        }
    }
}