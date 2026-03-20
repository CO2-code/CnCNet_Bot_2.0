using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MedalBot.Commands;
using MedalBot.Services;

namespace MedalBot.Services
{
    public class DiscordBotService
    {
        private readonly BotContext _ctx;
        private readonly DiscordSocketClient _client;

        public DiscordBotService(BotContext ctx)
        {
            _ctx = ctx;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent
            });

            _client.MessageReceived += OnMessageReceived;
        }

        public async Task Start(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        private async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Author.IsBot) return;

            string content = msg.Content;

            Console.WriteLine($"[DISCORD] {msg.Author.Username}: {content}");

            // ===== !say (force send to IRC) =====
            if (content.StartsWith("!say ", StringComparison.OrdinalIgnoreCase))
            {
                string text = content.Substring(5).Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("DEBUG: !say triggered");

                    _ctx.Writer?.WriteLine($"PRIVMSG {_ctx.Channel} :[DC] {msg.Author.Username}: {text}");
                    _ctx.Writer?.Flush();
                }

                return;
            }

            // ===== COMMANDS =====
            if (!content.StartsWith("!")) return;

            var commandManager = new CommandManager();
            string response = commandManager.TryProcess(_ctx, msg.Author.Username, content, content);

            if (!string.IsNullOrEmpty(response))
            {
                await msg.Channel.SendMessageAsync(response);
            }
        }
    }
}