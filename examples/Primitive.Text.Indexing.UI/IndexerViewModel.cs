using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using Primitive.Text.Documents;
using Primitive.Text.Documents.Sources;

namespace Primitive.Text.Indexing.UI
{
    public class IndexerViewModel : INotifyPropertyChanged
    {
        private IList<DocumentInfo> queryResults;
        private string queryText;

        public IndexerViewModel()
        {
            DefaultFilterPattern = "*.txt";
            Indexer = Indexer.Create(new IndexerCreationOptions() { IndexLocking = IndexLocking.Exclusive});
            Indexer.AddDocumentSource(new DirectoryDocumentSource(
                MoveUpThroughHierarhy(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), 5).FullName, "*.cs"));
        }

        public Indexer Indexer { get; private set; }

        public IReadOnlyList<DocumentSourceIndexer> DocumentSources { get { return Indexer.DocumentSources; } }

        public string DefaultFilterPattern { get; set; }

        public string QueryText
        {
            get { return queryText; }
            set
            {
                queryText = value;
                OnPropertyChanged();
                ExecuteQuery();
            }
        }

        public IList<DocumentInfo> QueryResults
        {
            get { return queryResults; }
            private set
            {
                queryResults = value;
                OnPropertyChanged();
            }
        }

        public void ExecuteQuery()
        {
            var terms = (QueryText ?? string.Empty).Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (!terms.Any())
            {
                QueryResults = new DocumentInfo[0];
                return;
            }

            var index = terms.Length > 1 ? Indexer.Index.Snapshot() : Indexer.Index;

            HashSet<DocumentInfo> resultDocumentSet = null;
            foreach (var term in terms)
            {
                var documents = GetTermDocuments(index, term);
                if (resultDocumentSet == null)
                    resultDocumentSet = new HashSet<DocumentInfo>(documents);
                else
                    resultDocumentSet.IntersectWith(documents);
            }
            QueryResults = resultDocumentSet.OrderBy(d => d.Id).ToList();
        }

        private static IEnumerable<DocumentInfo> GetTermDocuments(IIndex index, string term)
        {
            if (term.EndsWith("*"))
                return index.QueryDocumentsStartsWith(term.TrimEnd('*')).SelectMany(wordDocuments => wordDocuments.Value);
            
            if (term.StartsWith("*"))
            {
                term = term.TrimStart('*');
                return index.QueryDocumentsMatching(word => word.EndsWith(term, index.WordComparison)).SelectMany(wordDocuments => wordDocuments.Value);
            }
            
            return index.QueryDocuments(term);
        }

        public void ExploreToDocument(DocumentInfo document)
        {
            Process.Start("explorer.exe", string.Format("/select, \"{0}\"", document.Id));
        }

        private static DirectoryInfo MoveUpThroughHierarhy([NotNull] DirectoryInfo directory, int levels)
        {
            if (directory == null) throw new ArgumentNullException("directory");

            while (levels > 0 && directory.Parent != null)
            {
                directory = directory.Parent;
                levels--;
            }
            return directory;
        }




        public void AddDocumentSourcesFromPathList([NotNull] IEnumerable<string> files)
        {
            if (files == null) throw new ArgumentNullException("files");

            foreach (var path in files)
            {
                IDocumentSource documentSource;
                try
                {
                    if (Directory.Exists(path))
                        documentSource = new DirectoryDocumentSource(path, DefaultFilterPattern);
                    else
                        documentSource = new SingleFileDocumentSource(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
                Indexer.AddDocumentSource(documentSource);
            }
            OnPropertyChanged("DocumentSources");
        }


        #region INofityPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
