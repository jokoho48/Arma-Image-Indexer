#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private static int max;
        private static int current;

        [STAThread]
        private static void Main(string[] args)
        {
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOC);
            ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
            ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIOC);
            Console.WriteLine("maxWorker: " + maxWorker);
            Console.WriteLine("minWorker: " + minWorker);
            Console.WriteLine("availableWorker: " + availableWorker);
            Console.WriteLine("maxIOC: " + maxIOC);
            Console.WriteLine("minIOC: " + minIOC);
            Console.WriteLine("availableIOC: " + availableIOC);
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
                }
            }

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
            while (current != max)
            {
                UpdateTitle();
                Thread.Sleep(10);
            }

            Console.Beep();
            Thread.Sleep(10);
            Console.Beep();
            Environment.Exit(0);
        }

        private static void CheckSubDirs(string path, TextWriter output)
        {
            ScanImagesFiles(path, output);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            foreach (string folder in folders)
            {
                CheckSubDirs(folder, output);
                Thread.Sleep(1);
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
                    continue;
                }

                Tuple<string, string, int>
                    data = new Tuple<string, string, int>(file, filePNG, GetPreviewSize(filePNG));
                max++;
                ThreadPool.QueueUserWorkItem(ProcessPAAConvert, data);
                data = new Tuple<string, string, int>(file, filePNGPreview, -1);
                max++;
                ThreadPool.QueueUserWorkItem(ProcessPAAConvert, data);
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
                file.WriteLine("{0:HH-mm-ss-fffffff} {1}", DateTime.Now, log);
            }
        }

        private static void UpdateTitle()
        {
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOC);
            ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
            ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIOC);
            Console.Title =  "maxWorker: " + maxWorker +
                             " minWorker: " + minWorker +
                             " availableWorker: " + availableWorker +
                             " maxIOC: " + maxIOC +
                             " minIOC: " + minIOC +
                             " availableIOC: " + availableIOC;
        }


        private static void ProcessPAAConvert(object input)
        {
            Tuple<string, string, int> data = (Tuple<string, string, int>) input;
            string filePath = data.Item1;
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


            int width = Math.Min(bms.PixelWidth, bms.PixelWidth / bms.PixelHeight * data.Item3);
            int height = Math.Min(bms.PixelHeight, data.Item3);
            if (data.Item3 != -1 && height != bms.PixelHeight && width != bms.PixelWidth)
            {
                //bms = new TransformedBitmap(bms, new ScaleTransform(width, height));
            }

            //save as png
            string pngFilePath = data.Item2;
            FileStream pngStream = File.OpenWrite(pngFilePath);
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bms));
            pngEncoder.Save(pngStream);
            current++;
        }
    }
}