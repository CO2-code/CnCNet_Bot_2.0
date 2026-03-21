using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MedalBot.Commands;

namespace MedalBot.Services
{
    public class DiscordBotService
    {
        private readonly BotContext _ctx;
        private readonly DiscordSocketClient _client;
        private ISocketMessageChannel _channel;
        private readonly ulong _channelId;

        public DiscordBotService(BotContext ctx, ulong channelId)
        {
            _ctx = ctx;
            _channelId = channelId;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent
            });

            _client.MessageReceived += OnMessageReceived;

            _client.Ready += async () =>
            {
                _channel = _client.GetChannel(_channelId) as ISocketMessageChannel;
            };
        }

        public async Task Start(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task SendIrcMessage(string sender, string message)
        {
            if (_channel == null) return;

            await _channel.SendMessageAsync($"[IRC] {sender}: {message}");
        }

        private async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Author.IsBot) return;

            string content = msg.Content;

            if (content.StartsWith("!"))
            {
                var commandManager = new CommandManager();
                string response = commandManager.TryProcess(_ctx, msg.Author.Username, content, content);

                if (!string.IsNullOrEmpty(response))
                    await msg.Channel.SendMessageAsync(response);
            }

            if (!_ctx.RelayDiscordToIrc) return;

            if (content.StartsWith("!say "))
            {
                string text = content.Substring(5);
                _ctx.Writer?.WriteLine($"PRIVMSG {_ctx.Channel} :[DC] {msg.Author.Username}: {text}");
            }
        }
    }
}