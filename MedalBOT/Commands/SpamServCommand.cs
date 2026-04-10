using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class SpamServCommand : ICommand
    {
        public string Name => "SpamServ";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (!message.StartsWith("!badwords") && !message.StartsWith("!addbadword") && 
                !message.StartsWith("!delbadword"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use SpamServ commands.");

            if (message.StartsWith("!badwords"))
                return HandleBadWordsList(ctx, senderNick);

            if (message.StartsWith("!addbadword"))
                return HandleAddBadWord(ctx, senderNick, message);

            if (message.StartsWith("!delbadword"))
                return HandleDelBadWord(ctx, senderNick, message);

            return (false, null);
        }

        private (bool, string) HandleBadWordsList(BotContext ctx, string senderNick)
        {
            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :listbadwords {ctx.Channel}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} requested badwords list for {ctx.Channel}");
            return (true, $"?? Requesting bad words list from SpamServ...");
        }

        private (bool, string) HandleAddBadWord(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !addbadword <word> [reason]");

            string word = parts[1];
            string reason = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Spam";

            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :addbadword {ctx.Channel} {word} {reason}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} added bad word '{word}' to {ctx.Channel}");
            return (true, $"?? Adding '{word}' to bad words list...");
        }

        private (bool, string) HandleDelBadWord(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !delbadword <word>");

            string word = parts[1];

            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :delbadword {ctx.Channel} {word}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} removed bad word '{word}' from {ctx.Channel}");
            return (true, $"? Removing '{word}' from bad words list...");
        }
    }
}
