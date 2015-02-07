using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Primitive.Text.Content
{
    public static class TestContentManager
    {
        public static string GetContentPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        }


        public static IList<string> CopyContentFilesTo(string pattern, string destinationPath)
        {
            var results = new List<string>();
            string sourcePath = GetContentPath();
            foreach (var fileName in Directory.EnumerateFiles(sourcePath, pattern, SearchOption.TopDirectoryOnly))
            {
                string destinationFile = fileName.Replace(sourcePath, destinationPath);
                File.Copy(fileName, Path.Combine(destinationPath, destinationFile));
                results.Add(destinationFile);
            }
            return results;
        }


        public static string PrepareLargeTextFile()
        {
            var targetName = "europarl.subset.txt";
            var targetPath = Path.Combine(GetContentPath(), "Large", targetName);
            if (File.Exists(targetPath)) return targetPath;

    
            Console.WriteLine("Downloading content file");
            const string url = "https://dl.dropboxusercontent.com/u/88783474/europarl.subset.zip";
            try
            {
                using (var archive = new ZipArchive(new WebClient().OpenRead(url), ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry(targetName);
                    if (entry == null) return null;
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    using (var source = entry.Open())
                    using (var target = File.Create(targetPath))
                        source.CopyTo(target);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to download content file: {0}", e);
            }
            return targetPath;
        }


    }
}
