using System;
using System.IO;
using System.Reactive.Linq;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    /// <summary>
    ///  Provides the <see cref="IDocumentSource"/> implementation for a single file.
    /// </summary>
    public class SingleFileDocumentSource : FileSystemDocumentSource 
    {
        private readonly FileInfo fileInfo;
        private readonly Lazy<DocumentInfo> document;

        /// <summary>
        ///  Gets the full path to the single file this source represents
        /// </summary>
        public string FilePath { get { return fileInfo.FullName; } }

        /// <summary>
        ///  Initialized a new instance of <see cref="SingleFileDocumentSource"/> class
        ///  with the path to file
        /// </summary>
        /// <param name="filePath">Full path to the file</param>
        /// <exception cref="ArgumentException">Path is not rooted</exception>
        public SingleFileDocumentSource([NotNull] string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            fileInfo = new FileInfo(filePath);
            document = new Lazy<DocumentInfo>(() => DocumentFromPath(fileInfo.FullName));
        }

        /// <summary>
        ///  Validates the <paramref name="document"/> was originated from the current <see cref="DocumentSourceBase"/> instance.
        /// </summary>
        /// <param name="document">The document to check</param>
        /// <exception cref="ArgumentException">The document was originated not in this source</exception>
        protected override void EnsureOwnDocument([NotNull] DocumentInfo document)
        {
            base.EnsureOwnDocument(document);
            if (document != this.document.Value)
                throw new ArgumentException("Document was not originated from this source", "document"); // TODO: Details
        }


        /// <summary>
        ///  Returns the single document in the source available for indexing as an observable sequence
        /// </summary>
        /// <returns>
        ///  An observable sequence of <see cref="DocumentInfo"/>:
        ///     - empty when the document does not exist
        ///     - with the single document when it exists
        ///     - sequence that throws <see cref="DirectoryNotFoundException"/> when the directory of the file is not found
        /// </returns>
        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            if (!fileInfo.Directory.Exists)
                return Observable.Throw<DocumentInfo>(new DirectoryNotFoundException(string.Format("Directory '{0}' is invalid", fileInfo.Directory.FullName)));
            if (fileInfo.Exists)
                return Observable.Return(document.Value);
            else
                return Observable.Empty<DocumentInfo>();
        }


        /// <summary>
        /// Starts watching for any changes of the single document in this source
        /// </summary>
        /// <seealso cref="IDocumentSource.WatchForChangedDocuments"/>
        public override IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            return CreateWatcher(fileInfo.DirectoryName, new SearchPattern(fileInfo.Name)).Select(_ => document.Value);
        }
    }
}