using System;

namespace MedalBot.Services
{
    public static class HostmaskTracker
    {
        public static void UpdateHostmask(BotContext ctx, string line)
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

            // Auto-voice if ident has medal
            string ident = ExtractIdent(hostmask);
            if (!string.IsNullOrEmpty(ident) && ctx.VoicedUsers.ContainsKey(ident))
                ctx.Writer?.WriteLine($"PRIVMSG ChanServ :voice {ctx.Channel} {nick}");
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