using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace MedalBot.Services
{
    public class LoggingService
    {
        private readonly string _logDir;
        private readonly string _webhookUrl;

        public LoggingService(string webhookUrl, string logDir = "logs")
        {
            _webhookUrl = webhookUrl;
            _logDir = logDir;
            Directory.CreateDirectory(_logDir);
        }

        private string GetFilePath()
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return Path.Combine(_logDir, $"{date}.log");
        }

        public void Log(string text)
        {
            string line = $"[{DateTime.UtcNow:HH:mm:ss}] {text}";
            File.AppendAllText(GetFilePath(), line + Environment.NewLine, Encoding.UTF8);
        }

        public async Task SendTodayLog()
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl)) return;

            string file = GetFilePath();
            if (!File.Exists(file)) return;

            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();

            content.Add(new StreamContent(File.OpenRead(file)), "file", Path.GetFileName(file));

            try
            {
                await client.PostAsync(_webhookUrl, content);
            }
            catch { }
        }
    }
}