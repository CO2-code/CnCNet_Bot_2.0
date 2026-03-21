using MedalBot.Commands;
using MedalBot.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;

namespace MedalBot
{
    class Program
    {
        static async Task Main()
        {
            const string iniPath = "credentials.ini";

            if (!File.Exists(iniPath))
            {
                File.WriteAllText(iniPath,
@"[IRC]
Nick=
User=
Pass=
Channel=
ChannelPass=
Server=
Port=

[Admins]
Admin1=
Admin2=

[DISCORD]
Webhook=
Token=
");
                Console.WriteLine("⚠️ credentials.ini was missing. A template has been created. Fill it and restart the bot.");
            }

            var creds = IniReader.Read(iniPath, "IRC");
            var adminsSection = IniReader.Read(iniPath, "Admins");
            var discordSection = IniReader.Read(iniPath, "DISCORD");
            var commandsSection = IniReader.Read(iniPath, "Commands");

            var ctx = new BotContext
            {
                Server = creds.GetValueOrDefault("Server", "server"),
                Port = int.TryParse(creds.GetValueOrDefault("Port", "6667"), out var p) ? p : 6667,
                Nick = creds.GetValueOrDefault("Nick", "nick"),
                User = creds.GetValueOrDefault("User", "username"),
                Pass = creds.GetValueOrDefault("Pass", "Pass"),
                Channel = creds.GetValueOrDefault("Channel", "#channel"),
                ChannelPass = creds.GetValueOrDefault("ChannelPass", ""),
                VoicedUsers = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                CurrentHostmasks = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Admins = new System.Collections.Generic.HashSet<string>(
                    adminsSection.Values.Where(v => !string.IsNullOrWhiteSpace(v)),
                    StringComparer.OrdinalIgnoreCase),
                ScheduledMessages = new System.Collections.Generic.List<ScheduledMessage>(),
                CommandDescriptions = commandsSection,
                Random = new Random()             
            };

            ctx.Discord = new DiscordService(discordSection.GetValueOrDefault("Webhook", ""));
            ctx.Logger = new LoggingService(discordSection.GetValueOrDefault("Webhook", ""));

            var discordBot = new DiscordBotService(ctx);
            _ = discordBot.Start(discordSection.GetValueOrDefault("Token", ""));

            ctx.ReloadMessages = () => LoadMessages(ctx);

            LoadVoiced(ctx);
            LoadMessages(ctx);

            using var tcp = new TcpClient(ctx.Server, ctx.Port);
            using var reader = new StreamReader(tcp.GetStream());
            using var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true };
            ctx.Writer = writer;

            Console.WriteLine($"Connected to IRC server {ctx.Server}:{ctx.Port}");

            await ctx.Discord?.SendMessage($"Bot connected: {ctx.Nick}");

            writer.WriteLine($"NICK {ctx.Nick}");
            writer.WriteLine($"USER {ctx.User} 8 * :{ctx.User}");

            var commandManager = new CommandManager();
            var autoService = new AutoMessageService(ctx);
            autoService.Start();

            bool authed = false;
            bool joined = false;

            while (true)
            {
                string line = await reader.ReadLineAsync();
                if (line == null) continue;

                ctx.Logger?.Log(line);
                Console.WriteLine(line);

                if (line.StartsWith("PING"))
                {
                    writer.WriteLine($"PONG {line.Split(' ')[1]}");
                    continue;
                }

                if (!authed && line.Contains(" 001 "))
                {
                    await Task.Delay(1000);
                    writer.WriteLine($"PRIVMSG AuthServ@Services.GameSurge.net :AUTH {ctx.User} {ctx.Pass}");
                    Console.WriteLine("Sent AuthServ authentication...");
                    authed = true;
                    continue;
                }

                if (authed && !joined && line.Contains("is now your hidden host"))
                {
                    await Task.Delay(1500);
                    writer.WriteLine($"JOIN {ctx.Channel} {ctx.ChannelPass}");
                    Console.WriteLine($"Joined channel {ctx.Channel}");
                    joined = true;
                }

                HostmaskTracker.UpdateHostmask(ctx, line);

                if (line.Contains("PRIVMSG"))
                {
                    string sender = MessageParser.GetNick(line);
                    string message = MessageParser.GetMessage(line);

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        string response = commandManager.TryProcess(ctx, sender, message, line);
                        if (!string.IsNullOrEmpty(response))
                        {
                            writer.WriteLine($"PRIVMSG {ctx.Channel} :{sender}: {response}");
                            Console.WriteLine($"[Command] {sender}: {response}");
                        }
                    }

                    if (ctx.RelayIrcToDiscord && !string.IsNullOrWhiteSpace(message))
                    {
                        if (!message.StartsWith("!"))
                        {
                            if (message.Contains("badword"))
                            {
                                await ctx.Discord?.SendMessage($"[IRC WARNING] {sender}: {message}");
                            }
                        }
                    }
                }
            }
        }

        private static void LoadVoiced(BotContext ctx)
        {
            const string voicedFile = "voiced.txt";
            if (!File.Exists(voicedFile)) return;

            foreach (var line in File.ReadAllLines(voicedFile))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out int medal))
                    ctx.VoicedUsers[parts[0]] = medal;
            }
        }

        private static void LoadMessages(BotContext ctx)
        {
            const string messagesFile = "messages.txt";
            if (!File.Exists(messagesFile))
                File.WriteAllText(messagesFile, "Welcome to the channel! 1\nStay active and have fun! 2");

            var intervalSection = IniReader.Read("credentials.ini", "Intervals");

            ctx.ScheduledMessages.Clear();

            foreach (var line in File.ReadAllLines(messagesFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Trim().Split(' ');
                if (int.TryParse(parts[^1], out int priority))
                {
                    string msg = string.Join(' ', parts[..^1]);

                    int interval = intervalSection.TryGetValue(priority.ToString(), out var val) &&
                                   int.TryParse(val, out var parsed)
                                   ? parsed
                                   : int.TryParse(intervalSection.GetValueOrDefault("default", "200"), out var def) ? def : 200;

                    ctx.ScheduledMessages.Add(new ScheduledMessage(msg, interval));
                }
            }
        }
    }
}