using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XKeenMihomoGenerator.Services;

public static class Logger {
    private static readonly object lockObject = new object();
    private const long MaxFileSize = 1L * 1024 * 1024 * 1024;

    private static readonly Lazy<string> lazyPath = new Lazy<string>(GetLogPath, true);
    private static string LogPath => lazyPath.Value;

    private static readonly bool isDebuging = true;
    
    private static string GetLogPath() {
        string path;
        
        if (isDebuging)
            path = Path.Combine(AppContext.BaseDirectory, "Logs");
        else {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(appData, "XKeenMihomoGenerator", "Logs");
        }            

        try {
            Directory.CreateDirectory(path);
        }
        catch { }

        return path;
    }

    private static string GetCurrentFile(string prefix) {
        string? currentFile = null;
        var allFiles = new List<string>();

        foreach (string file in Directory.EnumerateFiles(LogPath, $"{prefix}_*.log"))
            allFiles.Add(file);

        allFiles.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal));

        if (allFiles.Any()) {
            string candidate = allFiles.First();
            var fileInfo = new FileInfo(candidate);

            if (fileInfo.Length < MaxFileSize)
                currentFile = candidate;
        }

        if (currentFile == null) {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            currentFile = Path.Combine(LogPath, $"{prefix}_{timestamp}.log");
        }

        return currentFile;
    }

    public static void Log(object message, bool newLine = true) {
        lock (lockObject) {
            bool isException = message is Exception;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string prefix = isException ? "ERROR" : "ACTION";
            string currentFile = GetCurrentFile(prefix);

            string logText;

            if (isException) {
                var ex = (Exception)message;
                string stackTrace = ex.ToString();
                logText = $"[{timestamp}] -> {ex.Message}\n{stackTrace}{(newLine ? "\n\n" : "")}";
            }
            else
                logText = $"[{timestamp}] {prefix}: {message}{(newLine ? "\n\n" : "")}";

            try {
                File.AppendAllText(currentFile, logText, System.Text.Encoding.UTF8);
            }
            catch { }
        }
    }
}
