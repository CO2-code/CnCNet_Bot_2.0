using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MedalBot.Services;

namespace MedalBot
{
    public class BotContext
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Nick { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string Channel { get; set; }
        public string ChannelPass { get; set; }

        public StreamWriter Writer { get; set; }

        public Dictionary<string, int> VoicedUsers { get; set; }
        public Dictionary<string, string> CurrentHostmasks { get; set; }
        public HashSet<string> Admins { get; set; }
        public List<ScheduledMessage> ScheduledMessages { get; set; }

        public Action? ReloadMessages { get; set; }

        public Random Random { get; set; }

        public DiscordService? Discord { get; set; }

        public bool RelayIrcToDiscord { get; set; } = true;
        public bool RelayDiscordToIrc { get; set; } = true;

        public LoggingService Logger { get; set; }

        public List<string> BadWords { get; set; }

        public Dictionary<string, Dictionary<string, DateTime>> IdentHistory { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> CommandDescriptions { get; set; }

        private readonly object _voicedLock = new();
        public void SaveVoiced(string file = "voiced.txt")
        {
            lock (_voicedLock)
            {
                File.WriteAllLines(file, VoicedUsers.Select(kv => $"{kv.Key} {kv.Value}"));
            }
        }
    }
}