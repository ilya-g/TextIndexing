using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Primitive.Text.Documents.Sources
{
    [TestFixture]
    public class DirectoryDocumentSourceTests
    {
        [Test]
        public void CreateDirectorySource_ForNonExistingDirectory_Fails()
        {
            var unknownPath = GetNewTempDirectoryPath();

            Assert.That(() => new DirectoryDocumentSource(unknownPath, "*"), Throws.ArgumentException);
        }

        [Test]
        public void FindAllDocuments_ReturnsExistingDocuments()
        {
            InNewTempDirectory(path =>
            {
                var destFiles = CopyContentFilesTo("*.txt", path);

                var source = new DirectoryDocumentSource(path, "*.txt");
                var list = source.FindAllDocuments().ToList().Wait();
                Assert.That(list, Has.Count.EqualTo(destFiles.Count));
                Assert.That(list.Select(d => d.Id), Is.EquivalentTo(destFiles).IgnoreCase);
            });
        }

        [Test]
        public void ChangedDocuments_ReactsOnCreateUpdateDelete()
        {
            InNewTempDirectory(path => 
            {
                var source = new DirectoryDocumentSource(path, "*.txt");
                Assert.That(source.FindAllDocuments().ToList().Wait(), Is.Empty);

                var tcsLastChanged = new TaskCompletionSource<DocumentInfo>();
                Action reset = () =>
                {
                    Thread.Sleep(300);
                    tcsLastChanged = new TaskCompletionSource<DocumentInfo>();
                };

                using (source.ChangedDocuments().Subscribe(d => tcsLastChanged.TrySetResult(d)))
                {
                    var destFileName = CopyContentFilesTo("text.txt", path).Single();
                    var lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    Assert.That(lastChanged.Id, Is.EqualTo(destFileName).IgnoreCase);

                    reset();
                    File.AppendAllText(destFileName, "\nNew line to change file");
                    lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    Assert.That(lastChanged.Id, Is.EqualTo(destFileName).IgnoreCase);

                    reset();
                    File.Delete(destFileName);
                    lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    Assert.That(lastChanged.Id, Is.EqualTo(destFileName).IgnoreCase);
                }
            });
        }

        [Test]
        public void ChangedDocuments_ReactsOnRename()
        {
            InNewTempDirectory(path =>
            {
                var source = new DirectoryDocumentSource(path, "*.txt");
                var sourceFileName = CopyContentFilesTo("text.txt", path).Single();

                var tcsLastChanged = new TaskCompletionSource<IList<DocumentInfo>>();


                using (source.ChangedDocuments()
                    .Buffer(TimeSpan.FromSeconds(0.1))
                    .Where(list => list.Any())
                    .Subscribe(d => tcsLastChanged.TrySetResult(d)))
                {
                    var newFileName = sourceFileName + ".new.txt";
                    File.Move(sourceFileName, newFileName);

                    var lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    lastChanged = lastChanged.Distinct().ToList();
                    Assert.That(lastChanged, Has.Count.EqualTo(2));
                    Assert.That(lastChanged.Select(d => d.Id), Is.EquivalentTo(new[] {sourceFileName, newFileName}).IgnoreCase);
                }
            });
        }

        private static string GetNewTempDirectoryPath()
        {
            var newTempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assume.That(Directory.Exists(newTempDirectoryPath), Is.False);
            return newTempDirectoryPath;
        }

        private static string GetContentPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        }

        private static IList<string> CopyContentFilesTo(string pattern, string destinationPath)
        {
            var results = new List<string>();
            string sourcePath = GetContentPath();
            foreach (var fileName in Directory.EnumerateFiles(sourcePath, pattern, SearchOption.AllDirectories))
            {
                string destinationFile = fileName.Replace(sourcePath, destinationPath);
                File.Copy(fileName, Path.Combine(destinationPath, destinationFile));
                results.Add(destinationFile);
            }
            return results;
        }

        private static void InNewTempDirectory(Action<string> testAction)
        {
            var path = GetNewTempDirectoryPath();
            Directory.CreateDirectory(path);
            try
            {
                testAction(path);
            }
            finally
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
