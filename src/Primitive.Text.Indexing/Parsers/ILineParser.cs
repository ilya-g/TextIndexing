using System.Collections.Generic;

namespace Primitive.Text.Parsers
{
    public interface ILineParser
    {
        IEnumerable<string> ExtractWords(string line);
    }
}