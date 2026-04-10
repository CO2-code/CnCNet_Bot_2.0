using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class SayCommand : ICommand
    {
        public string Name => "Say";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            // !say and !sayto are Discord-only commands (Discord ? IRC only)
            // Not available from IRC channel
            return (false, null);
        }

        public bool TryHandleDiscordSay(BotContext ctx, string message)
        {
            if (!message.StartsWith("!say "))
                return false;

            if (!ctx.RelayDiscordToIrc)
                return false;

            string text = message.Substring(5);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            ctx.Writer?.WriteLine($"PRIVMSG {ctx.Channel} :{text}");
            ctx.Logger?.Log($"[DISCORD SAY] {text}");
            return true;
        }

        public bool TryHandleDiscordSayTo(BotContext ctx, string message)
        {
            if (!message.StartsWith("!sayto "))
                return false;

            if (!ctx.RelayDiscordToIrc)
                return false;

            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return false;

            string targetNick = parts[1];
            string text = string.Join(" ", parts.Skip(2));
            if (string.IsNullOrWhiteSpace(text))
                return false;

            ctx.Writer?.WriteLine($"PRIVMSG {targetNick} :{text}");
            ctx.Logger?.Log($"[DISCORD SAYTO] Sent PM to {targetNick}: {text}");
            return true;
        }
    }
}


