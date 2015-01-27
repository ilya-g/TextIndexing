using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace Primitive.Text.Documents.Sources
{
    public class DirectoryDocumentSource : FileSystemDocumentSource
    {
        private readonly string filter;
        private DirectoryInfo rootInfo;

        public DirectoryDocumentSource(string rootPath, string filter)
        {
            this.filter = filter;
            if (Directory.Exists(rootPath))
                rootInfo = new DirectoryInfo(rootPath);
            else 
                throw new ArgumentException(string.Format("Directory '{0} does not exist", rootPath), "rootPath");


            // create watcher for fodler or directory
        }

        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            return Observable.Defer(() =>
                rootInfo.EnumerateFiles(filter, SearchOption.AllDirectories)
                    .Select(fileInfo => new DocumentInfo(fileInfo.FullName, this))
                    .ToObservable())
                .SubscribeOn(Scheduler.Default);
        }

        public override IObservable<DocumentInfo> ChangedDocuments()
        {
            return CreateWatcher(rootInfo.FullName, filter)
                .SelectMany(e =>
                    e is RenamedEventArgs
                    ? RenameChanges((RenamedEventArgs) e).ToObservable() 
                    : Observable.Return(DocumentFromPath(e.FullPath)));
        }

        private IEnumerable<DocumentInfo> RenameChanges(RenamedEventArgs e)
        {
            return new[] {DocumentFromPath(e.OldFullPath), DocumentFromPath(e.FullPath)};
        } 
    }
}
