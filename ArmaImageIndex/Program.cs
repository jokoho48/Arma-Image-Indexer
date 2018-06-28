using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
namespace ConsoleApp1
{
    class Program
    {
        private static string CMDConvert = $"E:\\SteamLibrary\\steamapps\\common\\Arma 3 Tools\\TexView2\\Pal2PacE.exe";
        private static string baseDir = "P:\\a3";
        private static Queue<string> startInfos = new Queue<string>();
        private static Process[] processes = new Process[Environment.ProcessorCount];
        private static string startTime = "";
        static void Main(string[] args)
        {
            startTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            Thread t = new Thread(ThreadWork);
            t.Start();
            using (StreamWriter output = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "index.html")))
            {
                output.WriteLine("<html><head><title>Arma 3 Image Index</title><link rel=\"stylesheet\" type=\"text/css\" href=\"sty.css\"></head></body bg>");
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

        static void ThreadWork()
        {
            while (true)
            {
                CheckNewProcessRequired();
                Thread.Sleep(10);
            }
        }

        static void CheckSubDirs(string path, StreamWriter output)
        {
            ConvertImage(path, output);
            CheckNewProcessRequired();
            var folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            foreach (var folder in folders)
            {
                CheckSubDirs(folder, output);
            }
        }

        static void ConvertImage(string path, StreamWriter output)
        {
            var files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return;
            output.WriteLine($"<h2>  { path } </h2>");
            foreach (var file in files)
            {
                CheckNewProcessRequired();
                var filePNG = file.Replace("paa", "png");
                filePNG = filePNG.Replace("P:\\a3", Environment.CurrentDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(filePNG));
                output.WriteLine(("<img src=\"" + filePNG.Replace(Environment.CurrentDirectory + "\\", "") + "\">\n").Replace("\\", "/"));
                if (File.Exists(filePNG))
                    continue;
                if (Path.GetFileNameWithoutExtension(filePNG).EndsWith(""))
                {

                }
                startInfos.Enqueue($"-size={CheckFileName(filePNG)} \"{file}\" \"{filePNG}\"");
            }
        }

        static void CheckNewProcessRequired()
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
                            return;
                        ProcessStartInfo proc = new ProcessStartInfo
                        {
                            FileName = CMDConvert,
                            Arguments = startInfos.Dequeue(),
                            UseShellExecute = false,
                            CreateNoWindow = false
                        };
                        processes[i] = Process.Start(proc);
                    }
                }
            }
        }
        static int CheckFileName(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path).ToLower();
            if (
                name.EndsWith("_smdi")
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
               )
                return 32;
            else if (name.EndsWith("_lca") || name.EndsWith("_lco") || name.EndsWith("_no"))
                return 16;
            else if (name.EndsWith("_co"))
                return 64;
            else if (name.EndsWith("_ca") || name.EndsWith("_sky"))
                return 128;
            LOG("FileFormat not Found: " + name);
            return 128;
        }
        static void LOG(string log)
        {
            Console.WriteLine(log);
            using (StreamWriter file = new StreamWriter("log", true))
            {
                file.WriteLine(DateTime.Now.ToString("HH-mm-ss") + log);
            }
        }
    }
}
