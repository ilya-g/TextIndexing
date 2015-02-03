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
        private readonly SearchPattern searchPattern;
        private readonly DirectoryInfo rootInfo;

        public string RootPath { get { return rootInfo.FullName; } }
        public SearchPattern SearchPattern { get { return searchPattern; } }

        public DirectoryDocumentSource([NotNull] string rootPath) 
            : this(rootPath, new SearchPattern("*")) {}

        public DirectoryDocumentSource([NotNull] string rootPath, [NotNull] string searchPattern) 
            : this(rootPath, new SearchPattern(searchPattern)) {}

        public DirectoryDocumentSource([NotNull] string rootPath, [NotNull] SearchPattern searchPattern)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            if (searchPattern == null) throw new ArgumentNullException("searchPattern");

            this.searchPattern = searchPattern;
            this.rootInfo = new DirectoryInfo(rootPath);
        }

        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            return Observable.Defer(() =>
                SafeEnumerateAllFiles(rootInfo, searchPattern.ToString())
                    .Select(fileInfo => new DocumentInfo(fileInfo.FullName, this))
                    .ToObservable())
                .SubscribeOn(Scheduler.Default);
        }

        public override IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            return CreateWatcher(rootInfo.FullName, searchPattern.ToString())
                .SelectMany(e =>
                    e is RenamedEventArgs
                        ? ChangesFromRenameEventArgs((RenamedEventArgs) e).ToObservable()
                        : Observable.Return(DocumentFromPath(e.FullPath)));
        }

        private IEnumerable<DocumentInfo> ChangesFromRenameEventArgs(RenamedEventArgs e)
        {
            return new[] {e.OldFullPath, e.FullPath}.Where(SearchPattern.IsMatch).Select(DocumentFromPath);
        }

        private static IEnumerable<FileInfo> SafeEnumerateAllFiles(DirectoryInfo directory, string seachPattern)
        {
            return EnumerateIgnoreException(
                () => Enumerable.Concat(
                    directory.EnumerateFiles(seachPattern), 
                    directory.EnumerateDirectories().SelectMany(nested => SafeEnumerateAllFiles(nested, seachPattern))), 
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
