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
    public class DirectoryDocumentSourceTests : FileSystemDocumentSourceTests
    {
        [Test]
        public void Create_RequiresValidPath()
        {
            var path = "some^invalid*path[chars.txt";

            Assert.That(() => new DirectoryDocumentSource(path), Throws.ArgumentException);
        }

        [Test]
        public void Create_NonExistingDirectory_Succeeds()
        {
            var path = GetNewTempDirectoryPath();
            Assert.That(() => new DirectoryDocumentSource(path), Throws.Nothing);
        }


        [Test]
        public void Create_ValidatesFilterIsCorrect()
        {
            var incorrectFilter = "/";
            InNewTempDirectory(path =>
            {
                Assert.That(() => new DirectoryDocumentSource(path, incorrectFilter), Throws.ArgumentException);
                Assert.That(() => new DirectoryDocumentSource(path, null), Throws.InstanceOf<ArgumentNullException>());
            });
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
        public void FindAllDocuments_WhenFolderDoesNotExist_Fail()
        {
            var source = new DirectoryDocumentSource(GetNewTempDirectoryPath());
            Assert.That(IgnoreEventsReturnError(source.FindAllDocuments()).Wait(), Is.Not.Null);
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

                string destFileName = null;
                Action assertLastChangedIsDestFile = () =>
                {
                    var lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    Assert.That(lastChanged.Id, Is.EqualTo(destFileName).IgnoreCase);
                };

                using (source.WatchForChangedDocuments().Subscribe(d => tcsLastChanged.TrySetResult(d)))
                {
                    destFileName = CopyContentFilesTo("text.txt", path).Single();
                    assertLastChangedIsDestFile();
                    reset();

                    File.AppendAllText(destFileName, "\nNew line to change file");
                    assertLastChangedIsDestFile();
                    reset();

                    File.Delete(destFileName);
                    assertLastChangedIsDestFile();
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


                using (source.WatchForChangedDocuments()
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

        [Test]
        public void ChangedDocuments_WhenFolderDoesNotExist_Fails()
        {
            var source = new DirectoryDocumentSource(GetNewTempDirectoryPath());
            Assert.That(IgnoreEventsReturnError(source.WatchForChangedDocuments()).Wait(), Is.Not.Null);
        }


        [Test]
        public void OpenDocument_ForExistingFile_ReturnsStream()
        {
            InNewTempDirectory(path =>
            {
                CopyContentFilesTo("text.txt", path);
                var source = new DirectoryDocumentSource(path);
                var document = source.FindAllDocuments().SingleAsync().Wait();

                using (var reader = source.OpenDocument(document))
                {
                    Assert.That(reader, Is.Not.Null);
                    Assert.That(reader.ReadLine(), Is.Not.Empty);
                }
            });
        }

        [Test]
        public void OpenDocument_ForNonExistingFile_ReturnsNull()
        {
            InNewTempDirectory(path =>
            {
                var source = new DirectoryDocumentSource(path);
                var document = new DocumentInfo("text.txt", source);

                using (var reader = source.OpenDocument(document))
                {
                    Assert.That(reader, Is.Null);
                }
            });
        }


        [Test]
        public void OpenDocument_ForLockedFile_Fails()
        {
            InNewTempDirectory(path =>
            {
                var destFile = CopyContentFilesTo("text.txt", path).Single();
                var source = new DirectoryDocumentSource(path);
                var document = source.FindAllDocuments().SingleAsync().Wait();
                using (File.Open(destFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.That(() => source.OpenDocument(document), Throws.InstanceOf<IOException>());
                }
            });
        }

    }
}
