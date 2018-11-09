using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaImageIndex
{
    public static class SiteGeneration
    {

        internal static void StartGeneration(string outputDir)
        {
            using (StreamWriter output = new StreamWriter(Path.Combine(outputDir, "index.html")))
            {
                output.WriteLine("<html><head><title>Arma 3 Image Index</title><link rel=\"stylesheet\" type=\"text/css\" href=\"sty.css\"></head></body bg>");

                CheckSubDirs(outputDir, output);
            }
        }

        public static void CheckSubDirs(string path, TextWriter output)
        {
            ScanImagesFiles(path, output);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            foreach (string folder in folders)
            {
                CheckSubDirs(folder, output);
            }
        }

        private static void ScanImagesFiles(string path, TextWriter output)
        {
            List<string> files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly).ToList();
            files.AddRange(Directory.GetFiles(path, "*.pac", SearchOption.TopDirectoryOnly).ToList());
            if (files.Count == 0)
            {
                return;
            }

            files.Sort();

            StartGeneration(path);

            output.WriteLine($"<h2>  {path.Remove(0, 2)} </h2>"); // TODO: add Link
            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith("_preview.png")) continue;
                
                string filePNGPreview = file.Replace(".", "_preview.");
                output.WriteLine(
                    $"<a href=\"{file.Replace(Program.outputDir + "\\", "")}\"> <img title= \"{file.Remove(0, 2)}\" src=\" {filePNGPreview.Replace(Program.outputDir + "\\", "")} \"></a>\n"
                        .Replace("\\", "/"));
            }
        }
    }
}
