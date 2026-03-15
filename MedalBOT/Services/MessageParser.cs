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