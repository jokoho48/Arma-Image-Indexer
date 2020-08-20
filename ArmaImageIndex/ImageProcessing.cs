#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BIS.PAA;

#endregion

namespace ArmaImageIndex
{
    internal struct WorkTask
    {
        public readonly string inputPath;
        public readonly string outputPath;
        public readonly string previewOutputPath;

        public WorkTask(string inputPath, string outputPath, string previewOutputPath)
        {
            this.outputPath = outputPath;
            this.previewOutputPath = previewOutputPath;
            this.inputPath = inputPath;
        }
    }

    public static class ImageProcessing
    {
        internal static readonly Queue<object> WorkQueue = new Queue<object>();

        internal static readonly List<object> ActiveQueue = new List<object>();
        internal static int InQueue;
        internal static int done;
        internal static int runningTasks;
        internal static int alreadyFound;
        private static bool isDoneDir;
        private static bool isDoneConvert;
        private static readonly object DataLock = new object();

        internal static void Worker()
        {
            while (true)
            {
                object Task = null;
                lock (WorkQueue)
                    if (WorkQueue.Count != 0)
                        Task = WorkQueue.Dequeue();
                if (Task != null)
                    ProcessPAAConvert(Task);
                else
                    Thread.Sleep(100);
            }
        }

        internal static void StartConverting()
        {
            if (Program.processingMethod == Program.Method.Thread)
            {
                for (int i = 0; i < Program.Threads; i++)
                {
                    Thread t = new Thread(Worker);
                    t.Start();
                }
            }
            CheckSubDirs(Program.baseDir);
            isDoneDir = true;
            switch (Program.processingMethod)
            {
                case Program.Method.ThreadPool:
                    {
                        while (done + alreadyFound != InQueue)
                        {
                            UpdateTitle();
                            Thread.Sleep(10);
                        }

                        break;
                    }
                case Program.Method.Thread:
                    {
                        while (ActiveQueue.Count != 0 && WorkQueue.Count != 0)
                        {
                            UpdateTitle();
                            Thread.Sleep(10);
                        }

                        break;
                    }
                case Program.Method.Synchronise:
                    break;
                case Program.Method.Parallel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            isDoneConvert = true;
            SiteGeneration.StartGeneration(Program.outputDir);
        }

        private static void CheckSubDirs(string path)
        {
            ScanImagesFiles(path);
            Program.LOG("Folder Found: " + path);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            if (Program.processingMethod == Program.Method.Parallel) {
                Parallel.ForEach(folders, (folder) => {
                    if (!Program.IsIgnoredFolder(folder)) return;
                    CheckSubDirs(folder);
                });
            } else
            {
                foreach (string folder in folders)
                {
                    if (!Program.IsIgnoredFolder(folder)) continue;
                    CheckSubDirs(folder);
                }
            }
        }
        private static void PrepareProcessing(string file)
        {
            string newFile = file.Replace(Program.baseDir, Program.outputDir);
            string filePNG = newFile.Replace(".paa", ".png");
            filePNG = filePNG.Replace(".pac", ".png");
            filePNG = filePNG.Replace(Program.baseDir, "");
            string filePNGPreview = newFile.Replace(".paa", "_preview.png");
            filePNGPreview = filePNGPreview.Replace(".pac", "_preview.png");

            filePNGPreview = filePNGPreview.Replace(Program.baseDir, "");

            Directory.CreateDirectory(Path.GetDirectoryName(filePNG) ?? throw new FileNotFoundException());

            if (File.Exists(filePNG))
            {
                alreadyFound++;
                return;
            }

            Program.LOG("File Found: " + file);

            WorkTask data = new WorkTask(file, filePNG, filePNGPreview);

            InQueue++;
            switch (Program.processingMethod)
            {
                case Program.Method.ThreadPool:
                    ThreadPool.QueueUserWorkItem(ProcessPAAConvert, data);
                    break;
                case Program.Method.Thread:
                    lock (WorkQueue)
                        WorkQueue.Enqueue(data);
                    break;
                default:
                    ProcessPAAConvert(data);
                    break;
            }
        }
        private static void ScanImagesFiles(string path)
        {
            UpdateTitle();
            List<string> files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly).ToList();
            files.AddRange(Directory.GetFiles(path, "*.pac", SearchOption.TopDirectoryOnly).ToList());
            if (files.Count == 0)
            {
                return;
            }

            files.Sort();

            if (Program.processingMethod == Program.Method.Parallel)
            {
                Parallel.ForEach(files, (file) => { PrepareProcessing(file); });
            } else
            {
                foreach (string file in files)
                {
                    PrepareProcessing(file);
                }
            }
            
        }

        private static void UpdateTitle()
        {
            if (Program.processingMethod == Program.Method.ThreadPool)
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
                                " Already Finished: " + alreadyFound;
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
            try
            {
                lock (DataLock)
                {
                    runningTasks++;
                    UpdateTitle();
                }

                WorkTask data = (WorkTask)input;
                string filePath = data.inputPath;
                Program.LOG("Process File: " + filePath);
                //get raw pixel color data in ARGB32 format
                string ext = Path.GetExtension(filePath);

                bool isPac = ext != null && ext.Equals(".pac", StringComparison.OrdinalIgnoreCase);

                using (FileStream paaStream = File.OpenRead(filePath))
                {
                    PAA paa = new PAA(paaStream, isPac);
                    byte[] pixels = PAA.GetARGB32PixelData(paa, paaStream);
                    //create a BitmapSource
                    List<Color> colors = paa.Palette.Colors.Select(c => Color.FromArgb(c.A8, c.R8, c.G8, c.B8)).ToList();
                    BitmapPalette bitmapPalette = colors.Count > 0 ? new BitmapPalette(colors) : null;
                    BitmapSource bms = BitmapSource.Create(paa.Width, paa.Height, 300, 300, PixelFormats.Bgra32, bitmapPalette,
                        pixels, paa.Width * 4);
                    using (FileStream pngStream = File.OpenWrite(data.outputPath))
                    {
                        PngBitmapEncoder pngEncoder = new PngBitmapEncoder();

                        BitmapFrame bmf = BitmapFrame.Create(bms);

                        int size = 256;
                        float percentWidth = Math.Min(size / (float)bmf.PixelWidth, 1);
                        float percentHeight = Math.Min(size / (float)bmf.PixelHeight, 1);
                        float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                        if (bmf.PixelWidth > size || bmf.PixelHeight > size)
                        {
                            bmf = FastResize(bmf, percent, percent);
                        }
                        pngEncoder.Frames.Add(bmf);
                        pngEncoder.Save(pngStream);

                        size = 64;
                        percentWidth = Math.Min(size / (float)bmf.PixelWidth, 1);
                        percentHeight = Math.Min(size / (float)bmf.PixelHeight, 1);
                        percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                        if (bmf.PixelWidth > size || bmf.PixelHeight > size)
                        {
                            using (FileStream pngStreamPreview = File.OpenWrite(data.previewOutputPath))
                            {

                                PngBitmapEncoder pngEncoderPreview = new PngBitmapEncoder();
                                bmf = FastResize(bmf, percent, percent);
                                pngEncoderPreview.Frames.Add(bmf);
                                pngEncoderPreview.Save(pngStreamPreview);
                            }
                        }
                    }
                }
                // Memory Cleanup
                Program.LOG("Processing Done: " + filePath);
            }
            catch (Exception)
            {
            }
            finally
            {
                lock (DataLock)
                {
                    done++;
                    runningTasks--;
                    UpdateTitle();
                }
                GC.Collect();
            }
        }

        private static BitmapFrame FastResize(BitmapSource bfPhoto, float nWidth, float nHeight)
        {
            TransformedBitmap tbBitmap = new TransformedBitmap(bfPhoto,
                new ScaleTransform(nWidth, nHeight));
            return BitmapFrame.Create(tbBitmap);
        }

        internal static bool HasFinished()
        {
            return isDoneDir && isDoneConvert;
        }
    }
}