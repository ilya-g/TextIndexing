using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Primitive.Text.Parsers
{
    public class LineStreamParser : IStreamParser
    {
        private readonly ILineParser innerParser;

        public LineStreamParser(ILineParser innerParser)
        {
            this.innerParser = innerParser;
        }



        public IObservable<string> ExtractWords([NotNull] TextReader sourceReader)
        {
            if (sourceReader == null) throw new ArgumentNullException("sourceReader");

            return Observable.Create<string>(obs =>
            {
                var scheduler = Scheduler.Default;
                var subscription = new SingleAssignmentDisposable();
                subscription.Disposable = scheduler.Schedule(() =>
                {
                    try
                    {
                        while (!subscription.IsDisposed)
                        {
                            var line = sourceReader.ReadLine();
                            if (line == null)
                                break;
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
            //        while (!cancel.IsCancellationRequested)
            //        {
            //            var line = await sourceReader.ReadLineAsync().ConfigureAwait(false);
            //            if (line == null)
            //                break;
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
