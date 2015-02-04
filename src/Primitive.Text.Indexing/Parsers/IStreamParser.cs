using System;
using System.IO;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    public interface IStreamParser
    {

        IObservable<string> ExtractWords([NotNull] TextReader sourceReader);
    }
}