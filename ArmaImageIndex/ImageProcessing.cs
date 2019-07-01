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

        internal static void StartConverting()
        {
            CheckSubDirs(Program.baseDir);
            isDoneDir = true;
            switch (Program.processingMethod)
            {
                case Program.Method.ThreadPool:
                {
                    while (done != InQueue)
                    {
                        UpdateTitle();
                        Thread.Sleep(10);
                    }

                    break;
                }
                case Program.Method.Parallel:
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            isDoneConvert = true;
        }

        private static void CheckSubDirs(string path)
        {
            ScanImagesFiles(path);
            Program.LOG("Folder Found: " + path);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            foreach (string folder in folders)
            {
                CheckSubDirs(folder);
                if (Program.processingMethod == Program.Method.ThreadPool)
                {
                    Thread.Sleep(1);
                }
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

            foreach (string file in files)
            {
                string newFile = file.Replace(Program.baseDir, "");
                newFile = Path.Combine(Program.outputDir, newFile);
                string filePNG = newFile.Replace("paa", "png");
                filePNG = filePNG.Replace(Program.baseDir, "");
                string filePNGPreview = newFile.Replace(".paa", "_preview.png");
                filePNGPreview = filePNGPreview.Replace(Program.baseDir, "");

                Directory.CreateDirectory(Path.GetDirectoryName(filePNG) ?? throw new FileNotFoundException());

                if (File.Exists(filePNG))
                {
                    alreadyFound++;
                    continue;
                }

                Program.LOG("File Found: " + file);

                WorkTask data = new WorkTask(file, filePNG, filePNGPreview);

                InQueue++;
                switch (Program.processingMethod)
                {
                    case Program.Method.ThreadPool:
                        ThreadPool.QueueUserWorkItem(ProcessPAAConvert, data);
                        break;
                    case Program.Method.Parallel:
                        lock (WorkQueue)
                        {
                            WorkQueue.Enqueue(data);
                        }

                        break;
                    default:
                        ProcessPAAConvert(data);
                        break;
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

            Program.LOG("File Format not Found Using Default: " + name + " Path: " + path);
            return 128;
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
                ;
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

            WorkTask data = (WorkTask) input;
            string filePath = data.inputPath;
            Program.LOG("Process File: " + filePath);
            //get raw pixel color data in ARGB32 format
            string ext = Path.GetExtension(filePath);

            bool isPac = ext != null && ext.Equals(".pac", StringComparison.OrdinalIgnoreCase);
            FileStream paaStream = File.OpenRead(filePath);
            PAA paa = new PAA(paaStream, isPac);
            byte[] pixels = PAA.GetARGB32PixelData(paa, paaStream);
            //We use WPF stuff here to create the actual image file, so this is Windows only

            //create a BitmapSource
            List<Color> colors = paa.Palette.Colors.Select(c => Color.FromRgb(c.R8, c.G8, c.B8)).ToList();
            BitmapPalette bitmapPalette = colors.Count > 0 ? new BitmapPalette(colors) : null;

            BitmapSource bms = BitmapSource.Create(paa.Width, paa.Height, 300, 300, PixelFormats.Bgra32, bitmapPalette,
                pixels, paa.Width * 4);

            //save as png
            FileStream pngStream = File.OpenWrite(data.outputPath);
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bms));
            pngEncoder.Save(pngStream);

            int size = GetPreviewSize(data.outputPath);
            int originalWidth = paa.Width;
            int originalHeight = paa.Height;
            float percentWidth = size / (float) originalWidth;
            float percentHeight = size / (float) originalHeight;
            float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
            int width = (int) (originalWidth * percent);
            int height = (int) (originalHeight * percent);

            FileStream pngStreamPreview = File.OpenWrite(data.previewOutputPath);
            PngBitmapEncoder pngEncoderPreview = new PngBitmapEncoder();
            BitmapFrame bmf = BitmapFrame.Create(bms);

            if (height != bms.PixelHeight && width != bms.PixelWidth)
            {
                bmf = FastResize(bmf, width, height);
            }

            pngEncoderPreview.Frames.Add(bmf);
            pngEncoderPreview.Save(pngStreamPreview);

            // Memory Cleanup
            paaStream.Dispose();
            pngStream.Dispose();
            pngStreamPreview.Dispose();
            GC.Collect();

            done++;
            runningTasks--;
            UpdateTitle();
            Program.LOG("Processing Done: " + filePath);
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

                ActiveQueue.Clear();

                GC.Collect();

                Thread.Sleep(10);
            }
        }

        private static BitmapFrame FastResize(BitmapSource bfPhoto, int nWidth, int nHeight)
        {
            TransformedBitmap tbBitmap = new TransformedBitmap(bfPhoto,
                new ScaleTransform(nWidth / bfPhoto.Width, nHeight / bfPhoto.Height, 0, 0));
            return BitmapFrame.Create(tbBitmap);
        }

        internal static bool HasFinished()
        {
            return isDoneDir && isDoneConvert;
        }
    }
}