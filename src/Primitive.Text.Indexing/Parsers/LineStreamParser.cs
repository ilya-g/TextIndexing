using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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

            return Observable.Create<string>(obs =>
            {
                var scheduler = Scheduler.Default;
                var subscription = new SingleAssignmentDisposable();
                subscription.Disposable = scheduler.Schedule(() =>
                {
                    try
                    {
                        while (!sourceReader.EndOfStream && !subscription.IsDisposed)
                        {
                            var line = sourceReader.ReadLine();
                            foreach (var word in innerParser.ExtractWords(line))
                                obs.OnNext(word);
                        }
                        obs.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        obs.OnError(e);
                    }
                });
                return subscription;
            });

            // async version is much more slower
            //return Observable.Create<string>(async (obs, cancel) =>
            //{
            //    try
            //    {
            //        while (!sourceReader.EndOfStream && !cancel.IsCancellationRequested)
            //        {
            //            var line = await sourceReader.ReadLineAsync().ConfigureAwait(false);
            //            foreach (var word in innerParser.ExtractWords(line))
            //                obs.OnNext(word);
            //        }
            //        obs.OnCompleted();
            //    }
            //    catch (Exception e)
            //    {
            //        obs.OnError(e);
            //    }
            //}).SubscribeOn(Scheduler.Default);
        }
    }


}
