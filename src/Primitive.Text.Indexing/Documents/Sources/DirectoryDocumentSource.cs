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

            try
            {
                // validate filter pattern
                using (rootInfo.EnumerateFiles(filter).GetEnumerator()) { }
            }
            catch (DirectoryNotFoundException)
            {
                // it's ok if directory doesn't exists
            }
        }

        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            return Observable.Defer(() =>
                rootInfo.EnumerateFiles(filter, SearchOption.AllDirectories)
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
    }
}
