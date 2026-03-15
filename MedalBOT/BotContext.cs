using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

        public Random Random { get; set; }

        private readonly object _voicedLock = new();
        public void SaveVoiced(string file = "voiced.txt")
        {
            lock (_voicedLock)
            {
                File.WriteAllLines(file, VoicedUsers.Select(kv => $"{kv.Key} {kv.Value}"));
            }
        }
    }

    public class ScheduledMessage
    {
        public string Message { get; }
        public int IntervalMinutes { get; }

        public ScheduledMessage(string message, int interval)
        {
            Message = message;
            IntervalMinutes = interval;
        }
    }
}