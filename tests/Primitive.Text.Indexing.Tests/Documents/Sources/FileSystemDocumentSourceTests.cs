using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;

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

        protected static string GetContentPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        }

        protected static IList<string> CopyContentFilesTo(string pattern, string destinationPath)
        {
            var results = new List<string>();
            string sourcePath = GetContentPath();
            foreach (var fileName in Directory.EnumerateFiles(sourcePath, pattern, SearchOption.AllDirectories))
            {
                string destinationFile = fileName.Replace(sourcePath, destinationPath);
                File.Copy(fileName, Path.Combine(destinationPath, destinationFile));
                results.Add(destinationFile);
            }
            return results;
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