using System.Collections.Generic;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    public interface ILineParser
    {
        IEnumerable<string> ExtractWords([NotNull] string line);
    }
}