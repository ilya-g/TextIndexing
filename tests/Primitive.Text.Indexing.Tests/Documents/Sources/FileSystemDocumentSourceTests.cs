using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;
using Primitive.Text.Content;

namespace Primitive.Text.Documents.Sources
{
    public abstract class FileSystemDocumentSourceTests 
    {
        protected static string GetNewTempDirectoryPath()
        {
            var newTempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assume.That(Directory.Exists(newTempDirectoryPath), Is.False);
            return newTempDirectoryPath;
        }


        protected static void InNewTempDirectory(Action<string> testAction)
        {
            var path = GetNewTempDirectoryPath();
            Directory.CreateDirectory(path);
            try
            {
                testAction(path);
            }
            finally
            {
                Directory.Delete(path, recursive: true);
            }
        }

        protected static IList<string> CopyContentFilesTo(string pattern, string path)
        {
            return TestContentManager.CopyContentFilesTo(pattern, path);
        }


        protected static IObservable<Exception> IgnoreEventsReturnError<TSource>(IObservable<TSource> source)
        {
            return Observable.Create<Exception>(obs =>
                source.Subscribe(
                    _ => { },
                    exception => { obs.OnNext(exception); obs.OnCompleted(); },
                    obs.OnCompleted));
        } 
    }
}