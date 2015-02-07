using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    /// <summary>
    ///  Encapsulates the search pattern string to match against the names of files.
    /// </summary>
    /// <seealso cref="DirectoryInfo.EnumerateFiles(string)"/>
    [TypeConverter(typeof(SearchPattern.TypeConverter))]
    public sealed class SearchPattern
    {

        private static readonly char[] invalidSearchPatternChars;
        static bool IsNotWildcardChar(char c) { return c != '?' && c != '*'; }

        static SearchPattern()
        {
            invalidSearchPatternChars = Path.GetInvalidFileNameChars().Where(IsNotWildcardChar).ToArray();
        }


        private readonly string pattern;
        private readonly Lazy<Regex> patternRegex; 

        /// <summary>
        ///  Initializes a new instance of <see cref="SearchPattern"/> class with the specified <paramref name="pattern"/> string
        /// </summary>
        /// <param name="pattern">The search string to match against the names of files. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters.</param>
        /// <seealso cref="DirectoryInfo.EnumerateFiles(string)"/>
        public SearchPattern([NotNull] string pattern)
        {
            if (pattern == null) throw new ArgumentNullException("pattern");
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be empty or whitespace. Use * to match all file names.");

            for (int i = 0; i < pattern.Length; i++)
                if (invalidSearchPatternChars.Contains(pattern[i]))
                throw new ArgumentException(string.Format("Pattern '{0}' contains invalid char '{1}' at position {2}", pattern, pattern[i], i), "pattern");

            this.pattern = pattern.TrimEnd();
            this.patternRegex = new Lazy<Regex>(CreatePatternRegex);
        }

        private Regex CreatePatternRegex()
        {
            return new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        ///  Checks whether the specified <paramref name="fileName"/> matches this search pattern.
        /// </summary>
        /// <param name="fileName">The filename without path to be matched.</param>
        /// <returns>true, if filename matches search pattern</returns>
        public bool IsMatch([NotNull] string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            return patternRegex.Value.IsMatch(fileName);
        }

        /// <summary>
        ///  Returns the string representation of this search pattern, 
        ///  which can be used as a pattern to search or to watch for files.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return pattern;
        }


        /// <summary>
        ///  The type converter to convert <see cref="SearchPattern"/> to and from its string representation
        /// </summary>
        public class TypeConverter : System.ComponentModel.TypeConverter
        {
            /// <summary>
            /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
            /// </summary>
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            /// <summary>
            /// Converts the given object to the type of this converter, using the specified context and culture information.
            /// </summary>
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                    return new SearchPattern((string) value);

                return base.ConvertFrom(context, culture, value);
            }
        }

    }
}
