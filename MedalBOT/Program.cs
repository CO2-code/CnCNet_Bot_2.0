using MedalBot.Commands;
using MedalBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

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
            var badWordsSection = IniReader.Read(iniPath, "BadWords");

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
                BadWords = badWordsSection.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.ToLower())
                    .ToList(),
                Random = new Random()             
            };

            ctx.Discord = new DiscordService(discordSection.GetValueOrDefault("Webhook", ""));
            ctx.Logger = new LoggingService(discordSection.GetValueOrDefault("Webhook", ""));

            ulong channelId = ulong.TryParse(discordSection.GetValueOrDefault("ChannelId", "0"), out var cid) ? cid : 0;
            var discordBot = new DiscordBotService(ctx, channelId);
            _ = discordBot.Start(discordSection.GetValueOrDefault("Token", ""));

            ctx.ReloadMessages = () => LoadMessages(ctx);

            LoadVoiced(ctx);
            LoadMessages(ctx);
            ctx.LoadMutedIds();

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

                CheckExpiredMutes(ctx);

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

                await HostmaskTracker.UpdateHostmask(ctx, line);

                // Parse WHO responses (IRC 352 code)
                if (line.Contains(" 352 "))
                {
                    ParseWhoResponse(ctx, line);
                }

                // Capture ChanServ/SpamServ NOTICE replies (sent to bot nick for service requests)
                if (line.Contains("NOTICE") && (line.Contains("ChanServ") || line.Contains("SpamServ")) && !string.IsNullOrWhiteSpace(ctx.CurrentServiceRequestId))
                {
                    string noticeContent = MessageParser.GetMessage(line);
                    if (!string.IsNullOrWhiteSpace(noticeContent))
                    {
                        string requestId = ctx.CurrentServiceRequestId;
                        
                        // Initialize buffer if needed
                        if (!ctx.ServiceResponseBuffer.ContainsKey(requestId))
                            ctx.ServiceResponseBuffer[requestId] = new System.Collections.Generic.List<string>();
                        
                        // Add to buffer
                        ctx.ServiceResponseBuffer[requestId].Add(noticeContent);
                        ctx.Logger?.Log($"[SERVICE] Buffering response ({ctx.ServiceResponseBuffer[requestId].Count} lines): {noticeContent}");
                        
                        // Reset timeout - update last received time
                        ctx.ServiceResponseTimeouts[requestId] = DateTime.UtcNow;
                    }
                    continue;
                }

                // Check for timed-out service responses (2 second timeout)
                const int timeoutMs = 2000;
                var expiredRequests = ctx.ServiceResponseTimeouts
                    .Where(kvp => (DateTime.UtcNow - kvp.Value).TotalMilliseconds > timeoutMs)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var requestId in expiredRequests)
                {
                    if (ctx.PendingServiceRequests.TryGetValue(requestId, out var requestInfo) && 
                        ctx.ServiceResponseBuffer.TryGetValue(requestId, out var responses) && 
                        responses.Count > 0)
                    {
                        string requesterNick = requestInfo.RequesterNick;
                        bool isDiscord = requestInfo.IsDiscord;

                        ctx.Logger?.Log($"[SERVICE TIMEOUT] Response complete: {responses.Count} lines for {requesterNick}");
                        
                        string fullResponse = string.Join("\n", responses);

                        if (isDiscord)
                        {
                            ctx.Logger?.Log($"[SERVICE RELAY] Sending to Discord...");
                            await ctx.Discord?.SendMessage($"**{requesterNick}**:\n```\n{fullResponse}\n```");
                        }
                        else
                        {
                            foreach (var resp in responses)
                            {
                                writer.WriteLine($"PRIVMSG {ctx.Channel} :{resp}");
                            }
                        }

                        // Clean up
                        ctx.PendingServiceRequests.Remove(requestId);
                        ctx.ServiceResponseBuffer.Remove(requestId);
                        ctx.ServiceResponseTimeouts.Remove(requestId);
                        ctx.CurrentServiceRequestId = null;
                    }
                }

                if (line.Contains(" QUIT "))
                {
                    string quitter = MessageParser.GetNick(line);
                    if (!string.IsNullOrWhiteSpace(quitter))
                    {
                        ctx.RemoveNick(quitter);
                        ctx.Logger?.Log($"[NICK CLEANUP] Removed {quitter} from mute mappings on QUIT");
                    }
                }

                if (line.Contains(" NICK "))
                {
                    string oldNick = MessageParser.GetNick(line);
                    string newNick = MessageParser.GetNewNick(line);
                    if (!string.IsNullOrWhiteSpace(oldNick) && !string.IsNullOrWhiteSpace(newNick))
                    {
                        ctx.UpdateNick(oldNick, newNick);
                    }
                }

                if (line.Contains(" JOIN "))
                {
                    string joiner = MessageParser.GetNick(line);
                    if (!string.IsNullOrWhiteSpace(joiner))
                    {
                        string systemId = ctx.GetSystemId(joiner);
                        if (!string.IsNullOrWhiteSpace(systemId) && ctx.IsMuted(systemId))
                        {
                            writer.WriteLine($"NOTICE {joiner} :MUTE_ADD {systemId}");
                            ctx.Logger?.Log($"[MUTED NOTIFIED] {joiner} joined while muted (systemId: {systemId})");
                        }
                    }
                }

                if (line.Contains("PRIVMSG"))
                {
                    string sender = MessageParser.GetNick(line);
                    string message = MessageParser.GetMessage(line);

                    string senderSystemId = ctx.GetSystemId(sender);
                    if (!string.IsNullOrWhiteSpace(senderSystemId) && ctx.IsMuted(senderSystemId))
                    {
                        writer.WriteLine($"NOTICE {sender} :You are muted.");
                        ctx.Logger?.Log($"[MUTED BLOCKED] {sender} (systemId: {senderSystemId}) attempted: {message}");
                        continue;
                    }

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
                        string clean = message;

                        // remove IRC color codes
                        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\x03\d{0,2}", "");
                        clean = clean.Replace("\x02", "").Replace("\x0F", "");

                        var badWords = ctx.BadWords;

                        foreach (var bad in badWords)
                        {
                            if (string.IsNullOrWhiteSpace(bad)) continue;

                            if (clean.IndexOf(bad, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Console.WriteLine($"[WARNING] {sender}: {clean}");

                                await ctx.Discord?.SendMessage($"[IRC WARNING] {sender}: {clean}");
                                break;
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

        private static void CheckExpiredMutes(BotContext ctx)
        {
            var expiredMutes = new System.Collections.Generic.List<string>();
            foreach (var systemId in ctx.TimedMutes.Keys.ToList())
            {
                if (!ctx.IsTimedMute(systemId, out _))
                {
                    expiredMutes.Add(systemId);
                }
            }

            foreach (var systemId in expiredMutes)
            {
                ctx.TimedMutes.Remove(systemId);
                ctx.Logger?.Log($"[MUTE EXPIRED] {systemId} mute expired");
            }
        }

        private static void ParseWhoResponse(BotContext ctx, string line)
        {
            // IRC 352 format: :server 352 botnick channel ident host server nick H :hops realname
            // Example: :server 352 bot #channel ~ident host.com server nick H :0 Real Name
            var parts = line.Split(' ');
            if (parts.Length < 8) return;

            string channel = parts[3];
            string ident = parts[4];
            string host = parts[5];
            string nick = parts[7];

            if (string.IsNullOrWhiteSpace(nick) || nick.StartsWith(":")) return;

            string hostmask = $"{ident}@{host}";
            ctx.CurrentHostmasks[nick] = hostmask;
            ctx.Logger?.Log($"[WHO] {nick}: {hostmask}");
        }
    }
}