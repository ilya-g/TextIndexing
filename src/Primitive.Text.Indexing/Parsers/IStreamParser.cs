using System;
using System.IO;

namespace Primitive.Text.Parsers
{
    public interface IStreamParser
    {
        IObservable<string> ExtractWords(StreamReader sourceReader);
    }
}