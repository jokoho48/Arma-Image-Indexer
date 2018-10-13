#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BIS.PAA;

#endregion

namespace ArmaImageIndex
{
    internal static class Program
    {
        private static string baseDir = "P:";
        private static string outputDir = Environment.CurrentDirectory;
        private static int InQueue;
        private static int done;
        private static int runningTasks;
        private static int alreadyFound;
        private static readonly Queue<Tuple<string, string, string>> WorkQueue =
            new Queue<Tuple<string, string, string>>();

        private static readonly List<Tuple<string, string, string>> ActiveQueue =
            new List<Tuple<string, string, string>>();

        private static Method processingMethod = Method.Parallel;
        private static readonly object LogThreadMutex = new object();

        [STAThread]
        private static void Main(string[] args)
        {
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
                }

                if (arg.StartsWith("-method="))
                {
                    switch (arg.TrimStart("-method=".ToCharArray()).ToLower())
                    {
                        case "parallel":
                        case "p":
                            processingMethod = Method.Parallel;
                            break;

                        case "threadpool":
                        case "thread pool":
                        case "tp":
                            processingMethod = Method.ThreadPool;
                            break;

                        case "s":
                        case "sync":
                        case "Synchronise":
                            processingMethod = Method.Synchronise;
                            break;
                    }
                }
            }

            if (processingMethod == Method.ThreadPool)
            {
                ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOC);
                ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
                ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIOC);
                LOG("maxWorker: " + maxWorker);
                LOG("minWorker: " + minWorker);
                LOG("availableWorker: " + availableWorker);
                LOG("maxIOC: " + maxIOC);
                LOG("minIOC: " + minIOC);
                LOG("availableIOC: " + availableIOC);
            }
            else
            {
                Thread t = new Thread(WorkerThread);
                t.Start();
            }

            LOG("Success");

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
            if (processingMethod == Method.ThreadPool)
            {
                while (done != InQueue)
                {
                    UpdateTitle();
                    Thread.Sleep(10);
                }
            }
            else if (processingMethod == Method.Parallel)
            {
                while (ActiveQueue.Count != 0 && WorkQueue.Count != 0)
                {
                    UpdateTitle();
                    Thread.Sleep(10);
                }
            }

            Console.Beep();
            Thread.Sleep(10);
            Console.Beep();
            Environment.Exit(0);
        }

        private static void CheckSubDirs(string path, TextWriter output)
        {
            ScanImagesFiles(path, output);
            LOG("Folder Found: " + path);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            foreach (string folder in folders)
            {
                CheckSubDirs(folder, output);
                if (processingMethod == Method.ThreadPool)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private static void ScanImagesFiles(string path, TextWriter output)
        {
            UpdateTitle();
            List<string> files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly).ToList();
            files.AddRange(Directory.GetFiles(path, "*.pac", SearchOption.TopDirectoryOnly).ToList());
            if (files.Count == 0)
            {
                return;
            }

            files.Sort();

            output.WriteLine($"<h2>  {path.Remove(0, 2)} </h2>");
            foreach (string file in files)
            {
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
                    alreadyFound++;
                    continue;
                }

                LOG("File Found: " + file);
                Tuple<string, string, string> data = new Tuple<string, string, string>(file, filePNG, filePNGPreview);

                InQueue++;
                if (processingMethod == Method.ThreadPool)
                {
                    ThreadPool.QueueUserWorkItem(ProcessPAAConvert, data);
                }
                else if (processingMethod == Method.Parallel)
                {
                    lock (WorkQueue)
                    {
                        WorkQueue.Enqueue(data);
                    }
                }
                else
                {
                    ProcessPAAConvert(data);
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
            lock (LogThreadMutex)
            {
                using (StreamWriter file = new StreamWriter("log", true))
                {
                    file.WriteLine("{0:HH-mm-ss-fffffff} {1}", DateTime.Now, log);
                }
            }
        }

        private static void UpdateTitle()
        {
            if (processingMethod == Method.ThreadPool)
            {
                ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOC);
                ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
                ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIOC);
                Console.Title = "maxWorker: " + maxWorker +
                                " minWorker: " + minWorker +
                                " availableWorker: " + availableWorker +
                                " maxIOC: " + maxIOC +
                                " minIOC: " + minIOC +
                                " availableIOC: " + availableIOC +
                                " In Queue: " + InQueue +
                                " Done: " + done +
                                " Running Tasks: " + runningTasks +
                                " Already Finished: " + alreadyFound;;
            }
            else
            {
                Console.Title = "WorkQueue: " + WorkQueue.Count +
                                " ActiveQueue: " + ActiveQueue.Count +
                                " In Queue: " + InQueue +
                                " Done: " + done +
                                " Running Tasks: " + runningTasks +
                                " Already Finished: " + alreadyFound;
            }
        }

        private static void ProcessPAAConvert(object input)
        {
            runningTasks++;
            UpdateTitle();
            Tuple<string, string, string> data = (Tuple<string, string, string>) input;
            string filePath = data.Item1;
            LOG("Process File: " + filePath);
            //get raw pixel color data in ARGB32 format
            string ext = Path.GetExtension(filePath);

            bool isPAC = ext != null && ext.Equals(".pac", StringComparison.OrdinalIgnoreCase);
            FileStream paaStream = File.OpenRead(filePath);
            PAA paa = new PAA(paaStream, isPAC);
            byte[] pixels = PAA.GetARGB32PixelData(paa, paaStream);

            //We use WPF stuff here to create the actual image file, so this is Windows only

            //create a BitmapSource
            List<Color> colors = paa.Palette.Colors.Select(c => Color.FromRgb(c.R8, c.G8, c.B8)).ToList();
            BitmapPalette bitmapPalette = colors.Count > 0 ? new BitmapPalette(colors) : null;

            BitmapSource bms = BitmapSource.Create(paa.Width, paa.Height, 300, 300, PixelFormats.Bgra32, bitmapPalette,
                pixels, paa.Width * 4);

            //save as png
            FileStream pngStream = File.OpenWrite(data.Item2);
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bms));
            pngEncoder.Save(pngStream);

            int size = GetPreviewSize(data.Item2);
            int originalWidth = paa.Width;
            int originalHeight = paa.Height;
            float percentWidth = size / (float) originalWidth;
            float percentHeight = size / (float) originalHeight;
            float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
            int width = (int) (originalWidth * percent);
            int height = (int) (originalHeight * percent);

            FileStream pngStreamPreview = File.OpenWrite(data.Item3);
            PngBitmapEncoder pngEncoderPreview = new PngBitmapEncoder();
            BitmapFrame bmf = BitmapFrame.Create(bms);

            if (height != bms.PixelHeight && width != bms.PixelWidth)
            {
                bmf = FastResize(bmf, width, height);
            }

            pngEncoderPreview.Frames.Add(bmf);
            pngEncoderPreview.Save(pngStreamPreview);
            GC.Collect();
            done++;
            runningTasks--;
            UpdateTitle();
            LOG("Processing Done: " + filePath);
        }

        private static void WorkerThread()
        {
            while (true)
            {
                if (WorkQueue.Count == 0) continue;

                lock (WorkQueue)
                {
                    ActiveQueue.AddRange(WorkQueue);
                    WorkQueue.Clear();
                }

                Parallel.ForEach(ActiveQueue, ProcessPAAConvert);
                GC.Collect();
                ActiveQueue.Clear();
                Thread.Sleep(10);
            }
        }

        private static BitmapFrame FastResize(BitmapFrame bfPhoto, int nWidth, int nHeight)
        {
            TransformedBitmap tbBitmap = new TransformedBitmap(bfPhoto,
                new ScaleTransform(nWidth / bfPhoto.Width, nHeight / bfPhoto.Height, 0, 0));
            return BitmapFrame.Create(tbBitmap);
        }

        private enum Method
        {
            Parallel,
            ThreadPool,
            Synchronise
        }
    }
}