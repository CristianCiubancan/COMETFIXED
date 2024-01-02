using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Comet.Shared
{
    public enum LogLevel
    {
        Info,
        Debug,
        Warning,
        Error,
        Exception,
        DeadLoop,
        Cheat,
        Action,
        Socket
    }

    public enum LogFolder
    {
        SystemLog,
        GameLog,
        Root,
        AutoPatch
    }

    internal struct LogFile
    {
        public LogFolder Folder;
        public string Path;
        public string Filename;
        public DateTime Date;
    }

    public static class Log
    {
        static Log()
        {
            RefreshFolders();
        }

        public static Task WriteLogAsync(Exception ex)
        {
            return WriteLogAsync(LogLevel.Exception, ex.ToString());
        }

        public static Task WriteLogAsync(string message, params object[] values)
        {
            return WriteLogAsync(LogLevel.Info, message, values);
        }

        public static async Task WriteLogAsync(LogLevel level, string message, params object[] values)
        {
            await WriteLogAsync(DefaultFileName, level, message, values);
        }

        public static async Task WriteLogAsync(string file, LogLevel level, string message, params object[] values)
        {
            RefreshFolders();

            if (level == LogLevel.Action)
                file = "GameAction";

            message = string.Format(message, values);
            message = $"{DateTime.Now:HH:mm:ss.fff} [{level,-10}] - {message}";

            await WriteToFile(file, LogFolder.SystemLog, message);

            if (level != LogLevel.Action)
            {
                switch (level)
                {
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Exception:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                }

                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static async Task GmLogAsync(string file, string message, params object[] values)
        {
            RefreshFolders();

            if (values.Length > 0)
                message = string.Format(message, values);

            message = $"{DateTime.Now:HHmmss.fff} - {message}";

            await WriteToFile(file, LogFolder.GameLog, message);
        }

        public static async Task WriteToFile(string file, LogFolder folder, string value)
        {
            DateTime now = DateTime.Now;
            if (!Files.TryGetValue(file, out LogFile fileHandle))
                Files.Add(file, fileHandle = CreateHandle(file, folder));

            if (fileHandle.Date.Year != now.Year
                || fileHandle.Date.DayOfYear != now.DayOfYear)
            {
                fileHandle.Date = now;
                fileHandle.Path = Path.Combine(GetDirectory(folder), $"{file}.log");
            }

            try
            {
                await using var fWriter = new FileStream(fileHandle.Path, FileMode.Append, FileAccess.Write,
                                                         FileShare.Write, 4096);
                await using var writer = new StreamWriter(fWriter);
                await writer.WriteLineAsync(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static LogFile CreateHandle(string file, LogFolder folder)
        {
            if (Files.ContainsKey(file))
                return Files[file];

            DateTime now = DateTime.Now;
            var logFile = new LogFile
            {
                Date = now,
                Filename = $"{now:YYYYMMdd)} - {file}.log",
                Path = Path.Combine(GetDirectory(folder), $"{file}.log"),
                Folder = folder
            };
            return logFile;
        }

        private static string GetDirectory(LogFolder folder)
        {
            DateTime now = DateTime.Now;
            //return string.Join(Path.DirectorySeparatorChar.ToString(), ".", $"{folder}", $"{now.Year:0000}",
            //                   $"{now.Month:00}", $"{now.Day:00}");
            if (folder == LogFolder.Root)
            {
                return Environment.CurrentDirectory;
            }
            if (folder == LogFolder.AutoPatch)
            {
                return Path.Combine(Environment.CurrentDirectory, "AutoPatch", "logs");
            }
            return Path.Combine(Environment.CurrentDirectory, $"{folder}", $"{now.Year:0000}", $"{now.Month:00}", $"{now.Day:00}");
        }

        private static void RefreshFolders()
        {
            foreach (LogFolder eVal in Enum.GetValues(typeof(LogFolder)).Cast<LogFolder>())
                if (!Directory.Exists(GetDirectory(eVal)))
                    Directory.CreateDirectory(GetDirectory(eVal));
        }

        private static readonly Dictionary<string, LogFile> Files = new();

        public static string DefaultFileName = "Server";
    }
}