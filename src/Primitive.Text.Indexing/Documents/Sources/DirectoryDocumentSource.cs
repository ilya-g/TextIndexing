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
    /// <summary>
    ///  Provides the <see cref="IDocumentSource"/> implementation for a directory with serveral files and nested subdirectories.
    /// </summary>
    public class DirectoryDocumentSource : FileSystemDocumentSource
    {
        private readonly SearchPattern searchPattern;
        private readonly DirectoryInfo directoryInfo;

        /// <summary>
        ///  Gets path to the directory this source represents
        /// </summary>
        public string DirectoryPath { get { return directoryInfo.FullName; } }

        /// <summary>
        /// Gets the <see cref="Sources.SearchPattern"/> used to restrict this source documents 
        /// to those only whose filename matches the specified pattern
        /// </summary>
        public SearchPattern SearchPattern { get { return searchPattern; } }

        /// <summary>
        ///  Initializes a new <see cref="DirectoryDocumentSource"/> instance with a specified <paramref name="path"/> and the default <see cref="SearchPattern"/> value
        /// </summary>
        /// <param name="path">Full path to the directory</param>
        public DirectoryDocumentSource([NotNull] string path) 
            : this(path, new SearchPattern("*")) {}

        /// <summary>
        ///  Initializes a new <see cref="DirectoryDocumentSource"/> instance with a specified <paramref name="path"/> 
        ///  and a <paramref name="searchPattern"/> specified in the string representation
        /// </summary>
        /// <param name="path">Full path to the directory</param>
        /// <param name="searchPattern">Search pattern, all documents in this source must match to</param>
        public DirectoryDocumentSource([NotNull] string path, [NotNull] string searchPattern) 
            : this(path, new SearchPattern(searchPattern)) {}

        /// <summary>
        ///  Initializes a new <see cref="DirectoryDocumentSource"/> instance with a specified <paramref name="path"/> 
        ///  and a <paramref name="searchPattern"/>
        /// </summary>
        /// <param name="path">Full path to the directory</param>
        /// <param name="searchPattern">Search pattern, all documents in this source must match to</param>
        public DirectoryDocumentSource([NotNull] string path, [NotNull] SearchPattern searchPattern)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (searchPattern == null) throw new ArgumentNullException("searchPattern");

            this.searchPattern = searchPattern;
            this.directoryInfo = new DirectoryInfo(path);
        }

        /// <summary>
        ///  Enumerates all document files in the source directory and its subdirectories matching the specified <see cref="SearchPattern"/>
        /// </summary>
        /// <remarks>
        ///  When the directory or one of its subdirectories cannot be read due to insufficient access rights of the caller,
        ///  that directory content is ignored and doesn't included into the sequence being returned.
        /// </remarks>
        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            return Observable.Defer(() =>
                SafeEnumerateAllFiles(directoryInfo, searchPattern.ToString())
                    .Select(fileInfo => DocumentFromPath(fileInfo.FullName))
                    .ToObservable())
                .SubscribeOn(Scheduler.Default);
        }

        /// <summary>
        /// Starts watching for any changes of documents in the source directory and its subdirectories only matching the specified <see cref="SearchPattern"/>
        /// </summary>
        public override IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            return CreateWatcher(directoryInfo.FullName, searchPattern)
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
