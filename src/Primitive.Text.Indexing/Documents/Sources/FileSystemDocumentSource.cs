using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Primitive.Text.Documents.Sources
{
    public abstract class FileSystemDocumentSource : AbstractDocumentSource
    {
        protected FileSystemDocumentSource()
        {
            DefaultEncoding = System.Text.Encoding.Default;
        }

        public Encoding DefaultEncoding { get; set; }

        [CanBeNull]
        public override StreamReader OpenDocument(DocumentInfo document)
        {
            EnsureOwnDocument(document);

            try
            {
                return new StreamReader(File.Open(document.Id, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete), DefaultEncoding);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        protected DocumentInfo DocumentFromPath(string fileName)
        {
            return new DocumentInfo(fileName, this);
        }

        protected static IObservable<FileSystemEventArgs> CreateWatcher(string path, string filter)
        {
            return Observable.Create<FileSystemEventArgs>(obs =>
            {
                var watcher = new FileSystemWatcher(path, filter)
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