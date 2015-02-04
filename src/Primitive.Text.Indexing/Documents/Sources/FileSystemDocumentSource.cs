using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;
using Primitive.Text.Parsers;

namespace Primitive.Text.Documents.Sources
{

    /// <summary>
    ///  Provides the abstract base class for document sources obtaining their documents from the file system
    /// </summary>
    /// <seealso cref="SingleFileDocumentSource"/>
    /// <seealso cref="DirectoryDocumentSource"/>
    public abstract class FileSystemDocumentSource : DocumentSourceBase
    {
        /// <summary>
        ///  Initializes this instance
        /// </summary>
        protected FileSystemDocumentSource()
        {
            DefaultEncoding = System.Text.Encoding.Default;
        }

        /// <summary>
        ///  Gets or sets the encoding to be used to create streams for reading the documents
        /// </summary>
        public Encoding DefaultEncoding { get; set; }

        /// <summary>
        ///  Opens the <paramref name="document"/> 
        ///  and returns the <see cref="TextReader"/> to read its content
        /// </summary>
        /// <param name="document">Document to open</param>
        /// <returns>
        ///  <see cref="TextReader"/> to read text content, or null if the document file does not exist
        /// </returns>
        [CanBeNull]
        public override TextReader OpenDocument(DocumentInfo document)
        {
            EnsureOwnDocument(document);
            var path = document.Id;

            if (!File.Exists(path))
                return null;

            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 
                    65536,
                    FileOptions.SequentialScan | FileOptions.Asynchronous);
                return new StreamReader(stream, DefaultEncoding);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }


        /// <summary>
        ///  Extracts words to index from the <paramref name="document"/> with the specified <paramref name="streamParser"/>
        /// </summary>
        /// <param name="document">The document from this source</param>
        /// <param name="streamParser">The parser to be used to extract words from the document stream</param>
        /// <returns>
        /// Returns an observable sequence of document words, that being subscribed to
        /// pushes all words from the document and then completes. This sequence also complete with fail, if there was
        /// an error opening or reading the document.
        /// </returns>
        /// <remarks>
        /// This override adds retry semantics in case of document file is locked or cannot be opened due to some other
        /// <see cref="IOException"/>
        /// </remarks>
        public override IObservable<string> ExtractDocumentWords(DocumentInfo document, IStreamParser streamParser)
        {
            return RetryOn(
                base.ExtractDocumentWords(document, streamParser),
                shouldRetry: e => e is IOException, retryTimes: 3, retryDelay: TimeSpan.FromSeconds(1));
        }

        private static IObservable<T> RetryOn<T>(/*this*/ IObservable<T> source, Func<Exception, bool> shouldRetry, int retryTimes, TimeSpan retryDelay)
        {
            return source.Catch(
                (Exception e) => shouldRetry(e) && retryTimes > 0
                    ? RetryOn(source, shouldRetry, retryTimes - 1, retryDelay).DelaySubscription(retryDelay)
                    : Observable.Throw<T>(e));
        }


        /// <summary>
        ///  Constructs the <see cref="DocumentInfo"/> from the file <paramref name="path"/> provided
        /// </summary>
        /// <param name="path">Full path to the file</param>
        /// <returns>
        ///  <see cref="DocumentInfo"/> instance, with the path provided in <see cref="DocumentInfo.Id"/> property
        /// </returns>
        /// <remarks>
        /// When being overriden in derived classes, may construct more specified DocumentInfo and return it
        /// </remarks>
        protected virtual DocumentInfo DocumentFromPath(string path)
        {
            return new DocumentInfo(path, this);
        }


        /// <summary>
        ///  Creates the <see cref="FileSystemWatcher"/> and provides its events as an observable sequence 
        /// </summary>
        /// <param name="path">Path to directory to monitor changes in</param>
        /// <param name="filterPattern"><see cref="SearchPattern"/> instance to restrict notifications to matching this pattern only</param>
        /// <returns>
        ///  Observable sequence of <see cref="FileSystemEventArgs"/> or its subtype <see cref="RenamedEventArgs"/> notifications.
        ///  Sequence may complete with error, if it is reported by <see cref="FileSystemWatcher"/>.
        ///  Returned sequence is cold: new watcher is created on every subscribe.
        /// </returns>
        /// <seealso cref="FileSystemWatcher"/>
        protected static IObservable<FileSystemEventArgs> CreateWatcher([NotNull] string path, [NotNull] SearchPattern filterPattern)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (filterPattern == null) throw new ArgumentNullException("filterPattern");

            return Observable.Create<FileSystemEventArgs>(obs =>
            {
                var watcher = new FileSystemWatcher(path, filterPattern.ToString())
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                };
                FileSystemEventHandler watcherOnChanged = (s, e) => obs.OnNext(e);
                watcher.Changed += watcherOnChanged;
                watcher.Created += watcherOnChanged;
                watcher.Deleted += watcherOnChanged;
                watcher.Renamed += (s, e) => watcherOnChanged(s, e);
                watcher.Error += (s, e) => obs.OnError(e.GetException());
                return watcher;
            });
        }
    }
}