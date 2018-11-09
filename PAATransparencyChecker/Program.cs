using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BIS.Core;
using BIS.PAA;

namespace PAATransparencyChecker
{
    class Program
    {
        private static string baseDir = "P://a3";
        private static readonly Dictionary<string, bool> types = new Dictionary<string, bool>();
        private static void Main(string[] args)
        {
            CheckSubDirs(baseDir);
        }

        private static void CheckSubDirs(string path)
        {
            ScanImagesFiles(path);
            List<string> folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).ToList();
            folders.Sort();
            //Parallel.ForEach(folders, CheckSubDirs);
            foreach (string folder in folders)
            {
                CheckSubDirs(folder);
            }
        }

        private static void ScanImagesFiles(string path)
        {
            List<string> files = Directory.GetFiles(path, "*.paa", SearchOption.TopDirectoryOnly).ToList();
            files.AddRange(Directory.GetFiles(path, "*.pac", SearchOption.TopDirectoryOnly).ToList());
            //Parallel.ForEach(files, CheckFile);
            foreach (string file in files)
            {
                CheckFile(file);
            }
        }

        private static void CheckFile(string file)
        {
            string ext = Path.GetExtension(file);
            bool isPAC = ext != null && ext.Equals(".pac", StringComparison.OrdinalIgnoreCase);
            FileStream paaStream = File.OpenRead(file);
            PAA paa = new PAA(paaStream, isPAC);
            string[] splitName = Path.GetFileNameWithoutExtension(file)?.ToLower().Split('_');
            string type = splitName.Last();
            bool requireTransparency = paa.Palette.Colors.All(color => color.A8 != byte.MaxValue);
            if (types.ContainsKey(type) && types[type]) return;
            types.Add(type, requireTransparency);
            Console.WriteLine("Require Transparency: " + file + " Type: " + type);
        }
    }
}
