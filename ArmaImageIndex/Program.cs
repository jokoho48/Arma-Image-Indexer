#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace ArmaImageIndex
{
    internal static class Program
    {
        internal static string baseDir = "P://a3";
        internal static string outputDir = Environment.CurrentDirectory + "//a3";
        internal static List<string> includeDir = new List<string>();
        internal static Method processingMethod = Method.Parallel;
        private static readonly object LogThreadMutex = new object();
        internal static int Threads = 128; //Environment.ProcessorCount;

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

                else if (arg.StartsWith("-o="))
                {
                    outputDir = arg.TrimStart("-o=".ToCharArray());
                }

                else if (arg.StartsWith("-method="))
                {
                    switch (arg.TrimStart("-method=".ToCharArray()).ToLower())
                    {
                        case "parallel":
                        case "p":
                            processingMethod = Method.Parallel;
                            break;
                        case "thread":
                        case "t":
                            processingMethod = Method.Thread;
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
                else if (arg.StartsWith("-threads="))
                {
                    Threads = int.Parse(arg.TrimStart("-threads=".ToCharArray()));
                }
                else if (arg.StartsWith("-limit"))
                {
                    includeDir.Add("P://a3//3den");
                    includeDir.Add("P://a3//ui_f");
                    includeDir.Add("P://a3//ui_f_bootcamp");
                    includeDir.Add("P://a3//ui_f_curator");
                    includeDir.Add("P://a3//ui_f_exp");
                    includeDir.Add("P://a3//ui_f_exp_a");
                    includeDir.Add("P://a3//ui_f_heli");
                    includeDir.Add("P://a3//ui_f_jets");
                    includeDir.Add("P://a3//ui_f_kart");
                    includeDir.Add("P://a3//ui_f_mark");
                    includeDir.Add("P://a3//ui_f_mp_mark");
                    includeDir.Add("P://a3//ui_f_orange");
                    includeDir.Add("P://a3//ui_f_patrol");
                    includeDir.Add("P://a3//ui_f_tank");


                    includeDir.Add("P://a3//modules_f");
                    includeDir.Add("P://a3//modules_f_beta");
                    includeDir.Add("P://a3//modules_f_bootcamp");
                    includeDir.Add("P://a3//modules_f_curator");
                    includeDir.Add("P://a3//modules_f_epb");
                    includeDir.Add("P://a3//modules_f_exp");
                    includeDir.Add("P://a3//modules_f_exp_a");
                    includeDir.Add("P://a3//modules_f_heli");
                    includeDir.Add("P://a3//modules_f_jets");
                    includeDir.Add("P://a3//modules_f_kart");
                    includeDir.Add("P://a3//modules_f_mark");
                    includeDir.Add("P://a3//modules_f_mp_mark");
                    includeDir.Add("P://a3//modules_f_orange");
                    includeDir.Add("P://a3//modules_f_patrol");
                    includeDir.Add("P://a3//modules_f_tank");
                    includeDir.Add("P://a3//modules_f_warlords");

                }
            }



            ImageProcessing.StartConverting();
            Console.WriteLine("Press Enter To Close");
            Console.Read();
        }

        internal static void LOG(string log)
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

        internal enum Method
        {
            Parallel,
            Thread,
            ThreadPool,
            Synchronise
        }
        public static bool IsIgnoredFolder(string path)
        {
            if (includeDir.Count == 0) return true;

            foreach (var item in includeDir)
            {
                if (path.IsSubPathOf(item))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(this string path, string baseDirPath)
        {
            string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .WithEnding("\\"));

            string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .WithEnding("\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase) || normalizedPath == normalizedBaseDirPath;
        }

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string WithEnding(this string str, string ending)
        {
            if (str == null)
                return ending;

            string result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int i = 0; i <= ending.Length; i++)
            {
                string tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending))
                    return tmp;
            }

            return result;
        }

        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="value">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        public static string Right(this string value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
            }

            return (length < value.Length) ? value.Substring(value.Length - length) : value;
        }
    }

}