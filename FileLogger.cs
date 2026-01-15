using System;
using System.IO;

namespace Flight_Reservations
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;

        public FileLogger(string fileName = "app_logs.txt")
        {
            _filePath = fileName;
        }

        public void LogInfo(string message) => WriteToFile("INFO", message);
        public void LogWarning(string message) => WriteToFile("WARNING", message);
        public void LogError(string message, Exception ex = null) 
        {
            string details = ex != null ? $" | Exception: {ex.Message}" : "";
            WriteToFile("ERROR", message + details);
        }

        private void WriteToFile(string level, string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_filePath, logEntry + Environment.NewLine);
            }
            catch 
            {
                // Dacă nu putem scrie în log, nu vrem să crăpe aplicația
            }
        }
    }
}