using System;
using System.IO;
using System.Reactive.Linq;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public class SingleFileDocumentSource : FileSystemDocumentSource 
    {
        private readonly FileInfo fileInfo;
        private readonly DocumentInfo document;

        public string FilePath { get { return fileInfo.FullName; } }

        public SingleFileDocumentSource([NotNull] string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");
            fileInfo = new FileInfo(filePath);
            document = DocumentFromPath(fileInfo.FullName);
        }

        protected override void EnsureOwnDocument([NotNull] DocumentInfo document)
        {
            base.EnsureOwnDocument(document);
            if (document != this.document)
                throw new ArgumentException("Document was not originated from this source", "document"); // TODO: Details
        }


        public override IObservable<DocumentInfo> FindAllDocuments()
        {
            if (!fileInfo.Directory.Exists)
                return Observable.Throw<DocumentInfo>(new DirectoryNotFoundException(string.Format("Directory '{0}' is invalid", fileInfo.Directory.FullName)));
            if (fileInfo.Exists)
                return Observable.Return(document);
            else
                return Observable.Empty<DocumentInfo>();
        }

        public override IObservable<DocumentInfo> WatchForChangedDocuments()
        {
            return CreateWatcher(fileInfo.DirectoryName, fileInfo.Name).Select(_ => document);
        }
    }
}