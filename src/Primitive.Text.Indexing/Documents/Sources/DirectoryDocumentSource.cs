using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public class DirectoryDocumentSource : FileSystemDocumentSource
    {
        private readonly string filter;
        private readonly DirectoryInfo rootInfo;

        public string RootPath { get { return rootInfo.FullName; } }
        public string Filter { get { return filter; } }

        public DirectoryDocumentSource([NotNull] string rootPath) : this(rootPath, "*") {}

        public DirectoryDocumentSource([NotNull] string rootPath, [NotNull] string filter)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            if (filter == null) throw new ArgumentNullException("filter");

            this.filter = filter;
            this.rootInfo = new DirectoryInfo(rootPath);
        }

        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            return Observable.Defer(() =>
                SafeEnumerateAllFiles(rootInfo, filter)
                    .Select(fileInfo => new DocumentInfo(fileInfo.FullName, this))
                    .ToObservable())
                .SubscribeOn(Scheduler.Default);
        }

        public override IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            return CreateWatcher(rootInfo.FullName, filter)
                .SelectMany(e =>
                    e is RenamedEventArgs
                        ? ChangesFromRenameEventArgs((RenamedEventArgs) e).ToObservable()
                        : Observable.Return(DocumentFromPath(e.FullPath)));
        }

        private IEnumerable<DocumentInfo> ChangesFromRenameEventArgs(RenamedEventArgs e)
        {
            return new[] {DocumentFromPath(e.OldFullPath), DocumentFromPath(e.FullPath)};
        }

        private static IEnumerable<FileInfo> SafeEnumerateAllFiles(DirectoryInfo directory, string filter)
        {
            return EnumerateIgnoreException(
                () => Enumerable.Concat(
                    directory.EnumerateFiles(filter), 
                    directory.EnumerateDirectories().SelectMany(nested => SafeEnumerateAllFiles(nested, filter))), 
                shouldIgnore: e => e is UnauthorizedAccessException);
        }

        private static IEnumerable<T> EnumerateIgnoreException<T>(Func<IEnumerable<T>> sourceProvider, Func<Exception, bool> shouldIgnore)
        {
            IEnumerator<T> enumerator;
            try
            {
                enumerator = sourceProvider().GetEnumerator();
            }
            catch (Exception e)
            {
                if (!shouldIgnore(e)) throw; 
                yield break;
            }

            while (true)
            {
                try
                {
                    if (!enumerator.MoveNext()) yield break;
                }
                catch (Exception e)
                {
                    if (!shouldIgnore(e)) throw;
                    yield break;
                }
                yield return enumerator.Current;
            }


        }
    }

}
