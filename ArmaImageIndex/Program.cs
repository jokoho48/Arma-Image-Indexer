#region

using System;
using System.IO;

#endregion

namespace ArmaImageIndex
{


    internal static class Program
    {
        internal static string baseDir = "P://a3";
        internal static string outputDir = Environment.CurrentDirectory;
        
        internal static Method processingMethod = Method.Parallel;
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
            ImageProcessing.StartConverting();
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
            ThreadPool,
            Synchronise
        }


    }
}