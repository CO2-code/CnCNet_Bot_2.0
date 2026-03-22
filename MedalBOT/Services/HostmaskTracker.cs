using System;
using System.Collections.Generic;
using System.Linq;

namespace MedalBot.Services
{
    public static class HostmaskTracker
    {
        public static async void UpdateHostmask(BotContext ctx, string line)
        {
            if (!line.StartsWith(":")) return;

            int bang = line.IndexOf('!');
            if (bang == -1) return;

            int spaceAfter = line.IndexOf(' ', bang);
            if (spaceAfter == -1) return;

            string hostmask = line.Substring(bang + 1, spaceAfter - bang - 1);
            string nick = MessageParser.GetNick(line);

            if (!string.IsNullOrWhiteSpace(nick) && !string.IsNullOrWhiteSpace(hostmask))
                ctx.CurrentHostmasks[nick] = hostmask;

            string ident = ExtractIdent(hostmask);

            if (!string.IsNullOrEmpty(ident) && ctx.VoicedUsers.ContainsKey(ident))
                ctx.Writer?.WriteLine($"PRIVMSG ChanServ :voice {ctx.Channel} {nick}");

            if (string.IsNullOrEmpty(ident) || string.IsNullOrEmpty(nick)) return;

            if (!ctx.IdentHistory.ContainsKey(ident))
                ctx.IdentHistory[ident] = new Dictionary<string, DateTime>();

            ctx.IdentHistory[ident][nick] = DateTime.UtcNow;

            if (ctx.IdentHistory[ident].Count >= 10)
            {
                var list = ctx.IdentHistory[ident]
                    .OrderByDescending(x => x.Value)
                    .Take(20);

                string report = $"[IDENT WARNING] {ident} used {ctx.IdentHistory[ident].Count} nicks:\n";

                foreach (var entry in list)
                    report += $"{entry.Key}: {entry.Value:yyyy-MM-dd HH:mm:ss}\n";

                await ctx.Discord?.SendMessage(report);
            }
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