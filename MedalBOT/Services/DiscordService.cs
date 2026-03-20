using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MedalBot.Services
{
    public class DiscordService
    {
        private readonly string _webhookUrl;
        private readonly HttpClient _http;

        public DiscordService(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
            _http = new HttpClient();
        }

        public async Task SendMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl)) return;

            var json = $"{{\"content\":\"{Escape(content)}\"}}";
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                await _http.PostAsync(_webhookUrl, data);
            }
            catch { }
        }

        private string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}