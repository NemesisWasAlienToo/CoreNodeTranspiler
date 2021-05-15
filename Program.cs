using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CoreNodeWidgetCompiler
{
    class Program
    {
        public static string STDIN { get; set; }
        public static string STDOUT { get; set; }
        public static bool WATCH { get; set; } = false;
        public static FileSystemWatcher Watcher { get; set; }
        public static void OnChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Console.WriteLine($"File Changed: {e.FullPath}");
            WidgetCompiler.CompileFile(e.FullPath, STDOUT);
        }

        public static void Main(string[] args)
        {
            if (args[0] == "-h" || args[0] == "--help")
            {
                /* Write executable usage instruction */
            }

            /* Initialize application state */
            STDOUT = (args.Where(arg => arg.Contains("stdout=")).FirstOrDefault() ?? "stdout=").Replace("stdout=", "");
            STDIN = (args.Where(arg => arg.Contains("stdin=")).FirstOrDefault() ?? "stdin=").Replace("stdin=", "");
            WATCH = args.Where(arg => arg == "-w").FirstOrDefault() != null;

            while (string.IsNullOrEmpty(STDIN))
            {
                Console.Write("Please enter the source folder : ");
                STDIN = Console.ReadLine();
            }

            while (string.IsNullOrEmpty(STDOUT))
            {
                Console.Write("Please enter the source folder : ");
                STDOUT = Console.ReadLine();
            }

            Console.WriteLine("Welcome to Core Node Compiler.");
            Console.WriteLine("All rights are reserved for Dremier Development team.");

            if (WATCH)
            {
                Watcher = new FileSystemWatcher(STDIN);

                /* Notify syscall events */
                Watcher.NotifyFilter = NotifyFilters.LastWrite;

                Watcher.Changed += OnChange;

                /* File formats to be watched */
                Watcher.Filters.Add("*.js");
                Watcher.Filters.Add("*.html");

                /* Do nt watch sub dirs */
                Watcher.IncludeSubdirectories = false;

                /* Enable watcher */
                Watcher.EnableRaisingEvents = true;

                Console.WriteLine("Watch mode enabled");
                Console.WriteLine("Watching directory (" + STDIN + ").");
            }

            while (WATCH){
                Console.WriteLine("Press Ctl+c to exit");
                Console.ReadKey();
            }

            Console.WriteLine("Searching for html files in source directory (" + STDIN + ").");

            List<string> SourceFiles = new List<string>();

            if (STDIN.EndsWith(".js") || STDIN.EndsWith(".html"))
            {
                SourceFiles.Add(STDIN);
            }
            else
            {
                SourceFiles = Directory.GetFiles(STDIN, "*.*").ToList();
            }

            Console.WriteLine("Found : " + SourceFiles.Count() + " File(s).");
            SourceFiles.ForEach(sf => Console.WriteLine(sf));

            SourceFiles.ForEach(sf =>
            {
                Console.WriteLine("Compiling file : " + sf);
                WidgetCompiler.CompileFile(sf, STDOUT);
            });

            Console.WriteLine("Job finished");
            Console.WriteLine("Thank you for using our transpiler");
        }
    }
}