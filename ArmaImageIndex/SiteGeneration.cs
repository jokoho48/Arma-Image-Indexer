#region

using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;

#endregion

namespace ArmaImageIndex
{
    public static class SiteGeneration
    {
        internal static void StartGeneration(object outputDir)
        {
            Program.LOG("Generate Side: " + (string)outputDir);
            File.WriteAllText(Path.Combine((string)outputDir, "sty.css"), Properties.Resources.style);
            using (StreamWriter output = new StreamWriter(Path.Combine((string) outputDir, "index.html")))
            {
                output.WriteLine(
                    "<html><head><title>Arma 3 Image Index</title><link rel=\"stylesheet\" type=\"text/css\" href=\"sty.css\"></head></body bg>");

                CheckSubDirs((string) outputDir, output);
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
            List<string> files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count == 0)
            {
                return;
            }

            files.Sort();
            // ThreadPool.QueueUserWorkItem(StartGeneration, path);
            // StartGeneration(path);
            try
            {
                File.WriteAllText(path, Properties.Resources.style);
            }
            catch (System.Exception)
            {
            }

            output.WriteLine($"<h2>  {"\\a3\\" + path.Replace(Program.outputDir + "\\", "")} </h2>"); // TODO: add Link
            foreach (string file in files)
            {
                if (Path.GetFileName(file).EndsWith("_preview.png")) continue;
                var f = file.Replace(Program.outputDir + "\\", "");

                string filePNGPreview = f.Replace(".", "_preview.");
                if (!File.Exists(Path.Combine(Program.outputDir, filePNGPreview)))
                {
                    filePNGPreview = f;
                }
                output.WriteLine($"<a href=\"{f}\"><img loading=\"lazy\" title= \"{"\\a3\\" + f.Replace(".png",".paa")}\" src=\"{filePNGPreview} \"></a>\n");
            }
        }
    }
}