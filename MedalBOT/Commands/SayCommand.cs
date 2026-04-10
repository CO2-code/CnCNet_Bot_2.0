using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class SayCommand : ICommand
    {
        public string Name => "Say";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (!message.StartsWith("!say"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use say commands.");

            if (message.StartsWith("!sayto "))
                return HandleSayTo(ctx, senderNick, message);

            if (message.StartsWith("!say "))
                return HandleSay(ctx, senderNick, message);

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
            return true;
        }

        private (bool, string) HandleSay(BotContext ctx, string senderNick, string message)
        {
            if (message.Length <= 5)
                return (true, "Usage: !say <message>");

            string text = message.Substring(5).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return (true, "Usage: !say <message>");

            ctx.Logger?.Log($"[SAY] {senderNick} said: {text}");
            return (true, text);
        }

        private (bool, string) HandleSayTo(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return (true, "Usage: !sayto <nick> <message>");

            string targetNick = parts[1];
            string text = string.Join(" ", parts.Skip(2));

            ctx.Writer?.WriteLine($"PRIVMSG {targetNick} :{text}");
            ctx.Logger?.Log($"[SAYTO] {senderNick} sent PM to {targetNick}: {text}");
            return (false, null);
        }
    }
}

