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

    /// <summary>
    ///  Provides the <see cref="IStreamParser"/> implementation which reads the document content line by line
    ///  and parses each line with specified <see cref="ILineParser"/> instance.
    /// </summary>
    public class LineStreamParser : IStreamParser
    {
        private readonly ILineParser lineParser;

        /// <summary>
        ///  Initializes a new <see cref="LineStreamParser"/> instance with the <paramref name="lineParser"/>
        /// </summary>
        /// <param name="lineParser">A <see cref="ILineParser"/> instance used to extract words from individual lines of content</param>
        public LineStreamParser(ILineParser lineParser)
        {
            this.lineParser = lineParser;
        }


        /// <summary>
        ///  Extracts words from a document content read with <paramref name="sourceReader"/> and returns them as an observable sequence
        /// </summary>
        /// <param name="sourceReader">The reader to read the document content from</param>
        /// <returns>Observable sequence with a document words, one by one</returns>
        /// <remarks>
        ///  Reads the content from the <paramref name="sourceReader"/> line by line and extracts words from each line 
        ///  with the specified <see cref="ILineParser"/> instance.
        /// </remarks>
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
                            foreach (var word in lineParser.ExtractWords(line))
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
