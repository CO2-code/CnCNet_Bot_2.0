using System;

namespace MedalBot.Services
{
    public static class MessageParser
    {
        public static string GetNick(string line)
        {
            if (!line.StartsWith(":")) return "Unknown";
            int end = line.IndexOf('!');
            if (end > 1) return line.Substring(1, end - 1);
            return "Unknown";
        }

        public static string GetIdent(string line)
        {
            if (!line.StartsWith(":")) return null;
            int bang = line.IndexOf('!');
            if (bang == -1) return null;
            int at = line.IndexOf('@', bang);
            if (at == -1 || at <= bang + 1) return null;
            return line.Substring(bang + 1, at - bang - 1);
        }

        public static string GetNewNick(string line)
        {
            if (!line.Contains(" NICK ")) return null;
            int nickIdx = line.IndexOf(" NICK ");
            if (nickIdx == -1) return null;
            int colonIdx = line.IndexOf(':', nickIdx);
            if (colonIdx == -1) return null;
            string newNick = line.Substring(colonIdx + 1).Trim();
            return string.IsNullOrWhiteSpace(newNick) ? null : newNick;
        }

        public static string GetMessage(string line)
        {
            int idx = line.IndexOf("PRIVMSG");
            if (idx == -1) return "";
            int msgStart = line.IndexOf(':', idx);
            if (msgStart == -1) return "";

            string message = line[(msgStart + 1)..].Trim();

            int exclamation = message.IndexOf('!');
            if (exclamation != -1) message = message.Substring(exclamation);

            return message;
        }
    }
}