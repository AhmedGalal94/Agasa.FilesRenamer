using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FilesRenamerConsoleApp
{
    class Program
    {
        static ILogger Logger;
        static void Main(string[] args)
        {

            InitalizeLogger();
            ColorConsole.WriteWrappedHeader("Files Renamer Tool", '*', ConsoleColor.Green);
            ColorConsole.WriteLine("1- You can choose target directory path or will scan current directory");
            ColorConsole.WriteLine("2- enter the keyword to match it in all sub directories and files and replace it with new one");
            ColorConsole.WriteEmbeddedColorLine("be careful matching is [red] not case sensitive [/red] as it's natural of OS Naming", ConsoleColor.DarkCyan);
            ColorConsole.WriteEmbeddedColorLine("You will find logs of success porcess under [yellow]Logs[/yellow] folder", ConsoleColor.DarkCyan);
            ColorConsole.WriteEmbeddedColorLine("[you can enter [yellow]Ctrl-c[/yellow] to exist.]", ConsoleColor.DarkCyan);


        ChooseDirectory:
            ColorConsole.Write("\nDirectory path to be scanned [hit enter to use current directory]:");
            var enteredDirectory = Console.ReadLine();
            enteredDirectory = string.IsNullOrWhiteSpace(enteredDirectory) ? Directory.GetCurrentDirectory() : enteredDirectory;


            if (!Directory.Exists(enteredDirectory))
            {
                ColorConsole.WriteError("Directory not found !!");
                goto ChooseDirectory;
            }

        ChooseKeyword:
            ColorConsole.Write("\nThe keyword:");
            var keyword = Console.ReadLine();


            if (string.IsNullOrWhiteSpace(keyword))
            {
                ColorConsole.WriteError("keyword can't be empty or whitespaces!!");
                goto ChooseKeyword;
            }

            string[] allfiles = Directory.GetFiles(enteredDirectory, $"*{keyword}*", SearchOption.AllDirectories);
            string[] allDirectories = Directory.GetDirectories(enteredDirectory, $"*{keyword}*", SearchOption.AllDirectories).OrderByDescending(a=>a.Split("\\").Count()).ToArray();
            if (allfiles.Length > 0 || allDirectories.Length > 0)
                ColorConsole.WriteSuccess($"({allfiles.Length}) Files and ({allDirectories.Length}) Directories Names contain the keyword {keyword}");
            else
            {
                ColorConsole.WriteWarning($"no such a file or directory with name contains {keyword} is found,please try another keyword");
                goto ChooseKeyword;
            }


        ListMatchedFilesAndDirectories:
            ColorConsole.WriteEmbeddedColorLine("\nList matched files and directories ? [yellow](Y/N)[/yellow] . ");
            var ListMatchedFilesAndDirectories = Console.ReadKey(true);

            if (ListMatchedFilesAndDirectories.Key == ConsoleKey.Y)
            {
                if (allDirectories.Length > 0)
                    ColorConsole.WriteSuccess($"({allDirectories.Length}) Directories found :");
                foreach (var dir in allDirectories)
                {
                    ColorConsole.WriteLine(dir, ConsoleColor.DarkCyan);
                }

                if (allfiles.Length > 0)
                    ColorConsole.WriteSuccess($"({allfiles.Length}) files found:");
                foreach (var file in allfiles)
                {
                    ColorConsole.WriteLine(file, ConsoleColor.Cyan);
                }

            }
            else if (ListMatchedFilesAndDirectories.Key != ConsoleKey.N)
            {
                goto ListMatchedFilesAndDirectories;
            }


        ChooseNewKeyword:
            ColorConsole.Write("\nThe new keyword:");
            var newKeyword = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(newKeyword))
            {
                ColorConsole.WriteError("new keyword can't be empty or whitespaces !!");
                goto ChooseNewKeyword;
            }
            if(keyword.Equals(newKeyword,StringComparison.InvariantCultureIgnoreCase))
            {
                ColorConsole.WriteError("keyword new keyword can't be the same !!");
                goto ChooseNewKeyword;
            }


        ReplaceConfirmed:
            ColorConsole.WriteEmbeddedColorLine($"\nReplace keyword [DarkCyan]{keyword}[/DarkCyan] with [DarkCyan]{newKeyword}[/DarkCyan] in All ({allfiles.Length}) files and ({allDirectories.Length}) directories under [DarkCyan]{enteredDirectory}[/DarkCyan] [yellow](Y/N)[/yellow].");
            var keywordReplaceConfirmed = Console.ReadKey(true);
            if (keywordReplaceConfirmed.Key == ConsoleKey.Y)
            {
                ProcessStartCountDown("Replace Names", 5);
                Logger.Information($"Srart Process of Replacing [{keyword}] with [{newKeyword}] for All files and directories under [{enteredDirectory}]");
                int affectedFilesCount = 0;
                int affectedDirecoriesCount = 0;
                foreach (var file in allfiles)
                    affectedFilesCount = affectedFilesCount + (ReplaceKeywordInFileName(file, keyword, newKeyword) ? 1 : 0);
                foreach (var directory in allDirectories)
                    affectedDirecoriesCount = affectedDirecoriesCount + (ReplaceKeywordInDirectoryName(directory, keyword, newKeyword) ? 1 : 0);

                ColorConsole.WriteWrappedHeader("Process end successfully", '-', ConsoleColor.Green, ConsoleColor.DarkGreen);
                ColorConsole.WriteInfo($"({allfiles.Length}) of ({affectedFilesCount}) Files Names Affected");
                ColorConsole.WriteInfo($"({allDirectories.Length}) of {affectedDirecoriesCount} Direcories Names Affected");
                ColorConsole.WriteLine("");
                Logger.Information($"Finish Process of Replacing keyword: {keyword} with {newKeyword} for All files and directories under {enteredDirectory}");

            }
            else if (keywordReplaceConfirmed.Key != ConsoleKey.N)
            {
                goto ReplaceConfirmed;
            }

            ColorConsole.WriteEmbeddedColorLine("\nStart another process or [yellow]Ctrl-c[/yellow] to exist.\n");
            Console.ReadKey(true);
            goto ChooseDirectory;

        }



        public static bool ReplaceKeywordInDirectoryName(string directoryPath, string keyword, string newKeyword)
        {
            try
            {
                DirectoryInfo fi = new DirectoryInfo(directoryPath);
                if (fi.Exists)
                {
                    var directoryName = fi.Name;
                    var newDirectoryName = fi.Name.Replace(keyword, newKeyword,StringComparison.InvariantCultureIgnoreCase);
                    ColorConsole.WriteInfo($"Replacing directory name [{directoryName}] to [{newDirectoryName}] ...");
                    var newPath = Path.Combine(fi.Parent.FullName, newDirectoryName);
                    fi.MoveTo(newPath);
                    Logger.Information($"Replacing directory: {directoryPath} with {newPath}");
                    return true;
                }
                else
                {
                    ColorConsole.WriteError($"Directory: {directoryPath} not found !!");
                    Logger.Warning($"Directory: {directoryPath} not found !!");
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError($"error occurs while rename directory {directoryPath}");
                ColorConsole.WriteError($"Exception: {ex.Message}");
                Logger.Error($"Exception occurs while rename directory: {directoryPath}: \n Exception :{ex.Message}");
            }
            return false;
        }

        public static bool ReplaceKeywordInFileName(string filePath, string keyword, string newKeyword)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Exists)
                {
                    var fileName = fi.Name;
                    var newFileName = fi.Name.Replace(keyword, newKeyword,StringComparison.InvariantCultureIgnoreCase);
                    ColorConsole.WriteInfo($"Replacing file name [{fileName}] to [{newFileName}] ...");
                    var newPath = Path.Combine(fi.DirectoryName, newFileName);
                    fi.MoveTo(newPath);
                    Logger.Information($"Replacing file: {filePath} with {newPath}");
                    return true;
                }
                else
                {
                    ColorConsole.WriteError($"file: {filePath} not found !!");
                    Logger.Warning($"file: {filePath} not found !!");
                }
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError($"error occurs while rename file: {filePath}");
                ColorConsole.WriteError($"Exception: {ex.Message}");
                Logger.Error($"Exception occurs while rename file: {filePath}: \n Exception :{ex.Message}");
            }
            return false;
        }

        public static void ProcessStartCountDown(string ProcessName, int timeInSec)
        {
            int timer = timeInSec;
            while (timer > 0)
            {
                var message = string.Format("\r{0} will start in {1} sec...", ProcessName, timer);
                ColorConsole.Write(message, ConsoleColor.Blue);
                Thread.Sleep(1000);
                timer--;
            }
            Console.WriteLine();
        }


        public static void InitalizeLogger()
        {
             Logger  = Log.Logger = new LoggerConfiguration().WriteTo.File($"Logs/log.txt").CreateLogger();
        }
    }
}
