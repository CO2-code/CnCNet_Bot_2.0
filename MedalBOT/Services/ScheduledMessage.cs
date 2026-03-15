using System;

namespace MedalBot.Services
{
    public class ScheduledMessage
    {
        public string Text { get; set; }
        public int IntervalMinutes { get; set; }

        public ScheduledMessage(string text, int intervalMinutes)
        {
            Text = text;
            IntervalMinutes = intervalMinutes;
        }
    }
}