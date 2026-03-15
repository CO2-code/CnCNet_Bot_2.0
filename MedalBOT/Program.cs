using MedalBot.Commands;
using MedalBot.Services;
using MedalBOT.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MedalBot
{
    class Program
    {
        static async Task Main()
        {
            var ctx = new BotContext
            {
                Server = "irc.gamesurge.net",
                Port = 6667,
                Nick = "Nick",
                User = "Username",
                Pass = "Pass",
                Channel = "#channel",
                ChannelPass = "channelpass",
                VoicedUsers = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                CurrentHostmasks = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Admins = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
                ScheduledMessages = new System.Collections.Generic.List<ScheduledMessage>(),
                Random = new Random()
            };

            LoadAdmins(ctx);
            LoadVoiced(ctx);
            LoadMessages(ctx);

            using var tcp = new TcpClient(ctx.Server, ctx.Port);
            using var reader = new StreamReader(tcp.GetStream());
            using var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true };
            ctx.Writer = writer;

            Console.WriteLine($"Connected to IRC server {ctx.Server}:{ctx.Port}");

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
                }
            }
        }

        private static void LoadAdmins(BotContext ctx)
        {
            const string adminsFile = "admins.txt";
            if (!File.Exists(adminsFile)) return;

            ctx.Admins = new System.Collections.Generic.HashSet<string>(
                File.ReadAllLines(adminsFile)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim()),
                StringComparer.OrdinalIgnoreCase);
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

            ctx.ScheduledMessages.Clear();

            foreach (var line in File.ReadAllLines(messagesFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Trim().Split(' ');
                if (int.TryParse(parts.Last(), out int priority))
                {
                    string msg = string.Join(' ', parts[..^1]);
                    int interval = priority switch
                    {
                        1 => 50,
                        2 => 100,
                        3 => 150,
                        _ => 200
                    };
                    ctx.ScheduledMessages.Add(new ScheduledMessage(msg, interval));
                }
            }
        }
    }
}