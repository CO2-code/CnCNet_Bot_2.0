using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MedalBot.Commands;
using MedalBot.Services;

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
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                // Use JsonSerializer for proper JSON encoding
                var payload = new { content };
                var json = JsonSerializer.Serialize(payload);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(_webhookUrl, data);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DISCORD ERROR] HTTP {response.StatusCode}: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCORD ERROR] Failed to send message: {ex.Message}");
            }
        }
    }
}
