using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;

namespace ConsoleApp1
{
    internal class Program
    {
        private static string CMDConvert = "Pal2PacE.exe";
        private static string baseDir = "P:";
        private static string outputDir = Environment.CurrentDirectory;
        private static readonly Queue<string> startInfos = new Queue<string>();
        private static readonly Process[] processes = new Process[Environment.ProcessorCount];

        private static void Main(string[] args)
        {

            Console.WriteLine("Success");
            foreach (string arg in args)
            {
                if (arg.StartsWith("-f="))
                {
                    baseDir = arg.TrimStart("-f=".ToCharArray());
                    continue;
                }

                if (arg.StartsWith("-o="))
                {
                    outputDir = arg.TrimStart("-o=".ToCharArray());
                    continue;
                }

                if (arg.StartsWith("-t="))
                {
                    CMDConvert = arg.TrimStart("-t=".ToCharArray());
                }
            }

            Thread t = new Thread(ThreadWork);
            t.Start();
            using (StreamWriter output = new StreamWriter(Path.Combine(outputDir, "index.html")))
            {
                output.WriteLine(
                    "<html><head><title>Arma 3 Image Index</title><link rel=\"stylesheet\" type=\"text/css\" href=\"sty.css\"></head></body bg>");
                CheckSubDirs(baseDir, output);
                output.WriteLine("</ body ></ html >");
            }

            Console.Beep();
            Thread.Sleep(10);
            Console.Beep();
            while (startInfos.Count != 0)
            {
                Thread.Sleep(10);
            }

            Console.Beep();
            Thread.Sleep(10);
            Console.Beep();
            Environment.Exit(0);
        }

        private static void ThreadWork()
        {
            while (true)
            {
                CheckNewProcessRequired();
                Thread.Sleep(10);
            }
        }

        private static void CheckSubDirs(string path, TextWriter output)
        {
            ConvertImage(path, output);
            CheckNewProcessRequired();
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            foreach (string folder in folders)
            {
                CheckSubDirs(folder, output);
            }
        }

        private static void ConvertImage(string path, TextWriter output)
        {
            List<string> files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count == 0)
            {
                return;
            }
            files.Sort();

            output.WriteLine($"<h2>  {path.Remove(0, 2)} </h2>");
            foreach (string file in files)
            {
                CheckNewProcessRequired();
                string newFile = file.Replace(baseDir, "");
                newFile = Path.Combine(outputDir, newFile);
                string filePNG = newFile.Replace("paa", "png");
                filePNG = filePNG.Replace(baseDir, "");
                string filePNGPreview = newFile.Replace(".paa", "_preview.png");
                filePNGPreview = filePNGPreview.Replace(baseDir, "");

                Directory.CreateDirectory(Path.GetDirectoryName(filePNG) ?? throw new FileNotFoundException());

                output.WriteLine(
                    $"<a href=\"{filePNG.Replace(outputDir + "\\", "")}\"> <img title= \"{file.Remove(0, 2)}\" src=\" {filePNGPreview.Replace(outputDir + "\\", "")} \"></a>\n"
                        .Replace("\\", "/"));
                if (File.Exists(filePNG))
                {
                    continue;
                }

                startInfos.Enqueue($"-size={GetPreviewSize(filePNG)} \"{file}\" \"{filePNGPreview}\"");
                startInfos.Enqueue($"\"{file}\" \"{filePNG}\"");
            }
        }

        private static void CheckNewProcessRequired()
        {
            lock (startInfos)
            {
                if (startInfos.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < processes.Length; i++)
                {
                    if (processes[i] == null || processes[i].HasExited)
                    {
                        if (startInfos.Count == 0)
                        {
                            return;
                        }

                        ProcessStartInfo process = new ProcessStartInfo
                        {
                            FileName = CMDConvert,
                            Arguments = startInfos.Dequeue(),
                            UseShellExecute = false,
                            CreateNoWindow = false
                        };
                        processes[i] = Process.Start(process);
                    }
                }
            }
        }

        private static int GetPreviewSize(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path)?.ToLower() ?? throw new FileNotFoundException();
            if (
                name.EndsWith("_smdi")
                || name.EndsWith("_sdmi") // because BI fucking miss spells shit
                || name.EndsWith("_nohq")
                || name.EndsWith("_adshq")
                || name.EndsWith("_ads")
                || name.EndsWith("_as")
                || name.EndsWith("_mc")
                || name.EndsWith("_ti_ca")
                || name.EndsWith("_mask")
                || name.EndsWith("_dtsmdi")
                || name.EndsWith("_dt")
                || name.EndsWith("_sm")
                || name.EndsWith("_mca")
                || name.EndsWith("_nofhq")
                || name.EndsWith("_dxt5")
                || name.EndsWith("_sdm")
                || name.EndsWith("_dxt1")
                || name.StartsWith("surface_")
                || name.EndsWith("_4444")
                || name.EndsWith("_nopx")
                || name.EndsWith("_gs")
                || name.EndsWith("_mco")
                || name.EndsWith("_gs")
                || name.EndsWith("_4x4")
                || name.EndsWith("_detail")
                || name.EndsWith("_mlod")
                || name.EndsWith("_ti")
                || name.EndsWith("hohq")
            )
            {
                return 32;
            }

            if (name.EndsWith("_lca") || name.EndsWith("_lco") || name.EndsWith("_no"))
            {
                return 16;
            }

            if (name.EndsWith("_co"))
            {
                return 64;
            }

            if (name.EndsWith("_ca") || name.EndsWith("_sky"))
            {
                return 128;
            }

            LOG("File Format not Found Using Default: " + name + " Path: " + path);
            return 128;
        }

        private static void LOG(string log)
        {
            Console.WriteLine(log);
            using (StreamWriter file = new StreamWriter("log", true))
            {
                file.WriteLine("{0:HH-mm-ss-fffffff} {1}", arg0: DateTime.Now, arg1: log);
            }
        }
        

    }
}