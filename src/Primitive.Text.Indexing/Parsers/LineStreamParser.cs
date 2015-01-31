using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primitive.Text.Parsers
{
    public class LineStreamParser : IStreamParser
    {
        private readonly ILineParser innerParser;

        public LineStreamParser(ILineParser innerParser)
        {
            this.innerParser = innerParser;
        }



        public IObservable<string> ExtractWords(StreamReader sourceReader)
        {

            return Observable.Create<string>(async (obs, cancel) =>
            {
                try
                {
                    //var contents = await sourceReader.ReadToEndAsync();
                    //foreach (var line in contents.Split(new[]{ "\r\n", "\r", "\n"}, StringSplitOptions.None))
                    //{
                    //    if (cancel.IsCancellationRequested) return;
                    //    foreach (var word in innerParser.ExtractWords(line))
                    //        obs.OnNext(word);
                    //}
                    while (!sourceReader.EndOfStream && !cancel.IsCancellationRequested)
                    {
                        var line = await sourceReader.ReadLineAsync().ConfigureAwait(false);
                        foreach (var word in innerParser.ExtractWords(line))
                            obs.OnNext(word);
                    }
                    obs.OnCompleted();
                }
                catch (Exception e)
                {
                    obs.OnError(e);
                }
            }).SubscribeOn(Scheduler.Default);
        }
    }


}
