using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace Primitive.Text.Parsers
{
    public class StreamLineParser : IStreamParser
    {
        private readonly ILineParser innerParser;

        public StreamLineParser(ILineParser innerParser)
        {
            this.innerParser = innerParser;
        }

        public IObservable<string> ExtractWords(StreamReader sourceReader)
        {
            return Observable.Create<string>(async obs =>
            {
                while (!sourceReader.EndOfStream)
                {
                    try
                    {
                        var line = await sourceReader.ReadLineAsync().ConfigureAwait(false);
                        foreach (var word in innerParser.ExtractWords(line))
                            obs.OnNext(word);
                        obs.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        obs.OnError(e);
                    }
                }
            });
        }
    }


}
