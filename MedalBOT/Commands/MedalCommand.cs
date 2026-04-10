using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class MedalCommand : ICommand
    {
        private static readonly System.Collections.Generic.Dictionary<string, int> MedalMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "platinum", 1 },
            { "gold", 2 },
            { "silver", 3 }
        };

        public string Name => "medal";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (string.IsNullOrWhiteSpace(message)) return (false, null);

            int bang = message.IndexOf('!');
            if (bang >= 0) message = message.Substring(bang);

            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return (false, null);

            string cmd = parts[0].ToLowerInvariant();

            if (cmd == "!medallist") return (true, BuildMedalList(ctx));

            if (cmd != "!medal" && cmd != "!unmedal") return (false, null);

            if (!ctx.Admins.Contains(senderNick.ToLowerInvariant()))
                return (true, $"⚠️ {senderNick}: only admins can use this command.");

            return cmd == "!medal" ? HandleMedal(ctx, parts, senderNick) : HandleUnmedal(ctx, parts, senderNick);
        }

        private static (bool, string) HandleMedal(BotContext ctx, string[] parts, string senderNick)
        {
            if (parts.Length < 3)
                return (true, "Usage: !medal <nick> <Platinum|Gold|Silver>");

            string targetNick = parts[1];
            string medalType = parts[2];

            if (!MedalMap.TryGetValue(medalType, out int medalValue))
                return (true, "Invalid medal type. Use: Platinum, Gold, or Silver.");

            if (!ctx.CurrentHostmasks.TryGetValue(targetNick, out string hostmask) || string.IsNullOrWhiteSpace(hostmask))
            {
                ctx.Writer?.WriteLine($"WHO {targetNick}");
                ctx.Logger?.Log($"[MEDAL] Requesting WHO for {targetNick}");
                return (true, $"Fetching user info for {targetNick}...");
            }

            string ident = ExtractIdent(hostmask);
            if (string.IsNullOrEmpty(ident)) return (true, "⚠️ Could not extract ident from hostmask.");

            ctx.VoicedUsers[ident] = medalValue;
            ctx.SaveVoiced();

            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :voice {ctx.Channel} {targetNick}");
            Console.WriteLine($"[Medal] Saved {ident} => {medalValue} and voiced {targetNick}");

            string emoji = medalValue switch
            {
                1 => "🏆",
                2 => "🥇",
                3 => "🥈",
                _ => "🎖️"
            };

            return (true, $"✅ {senderNick} awarded {medalType.ToUpper()} {emoji} to {targetNick}.");
        }

        private static (bool, string) HandleUnmedal(BotContext ctx, string[] parts, string senderNick)
        {
            if (parts.Length < 2) return (true, "Usage: !unmedal <nick>");

            string targetNick = parts[1];

            if (!ctx.CurrentHostmasks.TryGetValue(targetNick, out string hostmask) || string.IsNullOrWhiteSpace(hostmask))
            {
                ctx.Writer?.WriteLine($"WHO {targetNick}");
                ctx.Logger?.Log($"[UNMEDAL] Requesting WHO for {targetNick}");
                return (true, $"Fetching user info for {targetNick}...");
            }

            string ident = ExtractIdent(hostmask);
            if (string.IsNullOrEmpty(ident)) return (true, "⚠️ Could not extract ident from hostmask.");

            if (!ctx.VoicedUsers.Remove(ident))
                return (true, $"⚠️ {targetNick} had no recorded medal.");

            ctx.SaveVoiced();
            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :devoice {ctx.Channel} {targetNick}");
            Console.WriteLine($"[Medal] Removed {ident} and de-voiced {targetNick}");

            return (true, $"❌ {senderNick} removed the medal from {targetNick}.");
        }

        private static string BuildMedalList(BotContext ctx)
        {
            if (ctx.VoicedUsers == null || ctx.VoicedUsers.Count == 0) return "No medalled players yet.";

            var items = ctx.VoicedUsers.Select(kv =>
            {
                string name = kv.Value switch
                {
                    1 => "Platinum",
                    2 => "Gold",
                    3 => "Silver",
                    _ => "Unknown"
                };
                var nickEntry = ctx.NickToId.FirstOrDefault(n => ExtractIdent(ctx.CurrentHostmasks.GetValueOrDefault(n.Key) ?? "") == kv.Key);
                string display = string.IsNullOrWhiteSpace(nickEntry.Key) ? kv.Key : $"{nickEntry.Key} ~{kv.Key}";
                return $"{display}({name})";
            });

            return $"🏅 Medallist: {string.Join(", ", items)}";
        }

        private static string ExtractIdent(string hostmask)
        {
            if (string.IsNullOrWhiteSpace(hostmask)) return null;
            int at = hostmask.IndexOf('@');
            if (at <= 0) return hostmask;
            return hostmask.Substring(0, at);
        }
    }
}