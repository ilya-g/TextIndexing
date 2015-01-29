using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Primitive.Text.Documents.Sources
{
    [TestFixture]
    public class SingleFileDocumentSourceTests : FileSystemDocumentSourceTests
    {
        [Test]
        public void Create_RequiresValidPath()
        {
            var filePath = "some^invalid*path[chars.txt";

            Assert.That(() => new SingleFileDocumentSource(filePath), Throws.ArgumentException);
        }

        [Test]
        public void Create_NonExistingFile_Succeeds()
        {
            var filePath = Path.Combine(GetNewTempDirectoryPath(), "file.txt");
            Assert.That(() => new SingleFileDocumentSource(filePath), Throws.Nothing);
        }


        [Test]
        public void FindAllDocuments_ReturnsExistingDocument()
        {
            InNewTempDirectory(path =>
            {
                var destFile = CopyContentFilesTo("text.txt", path).Single();

                var source = new SingleFileDocumentSource(destFile);
                var list = source.FindAllDocuments().ToList().Wait();
                Assert.That(list, Has.Count.EqualTo(1));
                Assert.That(list.Select(d => d.Id).Single(), Is.EqualTo(destFile).IgnoreCase);
            });
        }

        [Test]
        public void FindAllDocuments_DoNotReturnNonExistingDocument()
        {
            InNewTempDirectory(path =>
            {
                var destFile = Path.Combine(path, "text.txt");

                var source = new SingleFileDocumentSource(destFile);
                var list = source.FindAllDocuments().ToList().Wait();
                Assert.That(list, Is.Empty);
            });
        }

        [Test]
        public void FindAllDocuments_WhenFolderDoesNotExist_Fail()
        {
            var source = new SingleFileDocumentSource(Path.Combine(GetNewTempDirectoryPath(), "text.txt"));
            Assert.That(IgnoreEventsReturnError(source.FindAllDocuments()).Wait(), Is.Not.Null);
        }


        [Test]
        public void ChangedDocuments_ReactsOnAnyChange()
        {
            InNewTempDirectory(path =>
            {
                var tcsLastChanged = new TaskCompletionSource<DocumentInfo>();
                Action reset = () =>
                {
                    Thread.Sleep(300);
                    tcsLastChanged = new TaskCompletionSource<DocumentInfo>();
                };
                var destFiles = CopyContentFilesTo("*.txt", path);
                Assume.That(destFiles, Has.Count.GreaterThan(1));

                var destFileName = destFiles.First();
                var source = new SingleFileDocumentSource(destFileName);

                Action assertLastChangedIsDestFile = () =>
                {
                    var lastChanged = tcsLastChanged.Task.Result;
                    Assert.That(lastChanged, Is.Not.Null);
                    Assert.That(lastChanged.Id, Is.EqualTo(destFileName).IgnoreCase);
                };

                using (source.WatchForChangedDocuments().Subscribe(d => tcsLastChanged.TrySetResult(d)))
                {
                    // change
                    File.AppendAllText(destFileName, "\nNew line to change file");
                    assertLastChangedIsDestFile();
                    reset();

                    // delete
                    File.Delete(destFileName);
                    assertLastChangedIsDestFile();
                    reset();

                    // rename to
                    File.Move(destFiles[1], destFileName);
                    assertLastChangedIsDestFile();
                    reset();

                    // rename from
                    File.Move(destFileName, destFiles[1]);
                    assertLastChangedIsDestFile();
                    reset();

                    // create
                    File.WriteAllText(destFileName, "new file content");
                    assertLastChangedIsDestFile();
                }
            });
        }


        [Test]
        public void ChangedDocuments_WhenFolderDoesNotExist_Fails()
        {
            var source = new SingleFileDocumentSource(Path.Combine(GetNewTempDirectoryPath(), "text.txt"));
            Assert.That(IgnoreEventsReturnError(source.WatchForChangedDocuments()).Wait(), Is.Not.Null);
        }





    }
}