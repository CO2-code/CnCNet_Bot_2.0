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

        private async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            string content = message.Content;

            if (!content.StartsWith("!")) return;

            var commandManager = new CommandManager();
            string response = commandManager.TryProcess(_ctx, message.Author.Username, content, content);

            if (!string.IsNullOrEmpty(response))
            {
                await message.Channel.SendMessageAsync(response);
            }

            if (!_ctx.RelayDiscordToIrc) return;

            if (message.Content.StartsWith("!say "))
            {
                string text = message.Content.Substring(5);

                _ctx.Writer?.WriteLine($"PRIVMSG {_ctx.Channel} :[DC] {message.Author.Username}: {text}");
            }
        }
    }
}