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
                !message.StartsWith("!delbadword") && !message.StartsWith("!wordban") &&
                !message.StartsWith("!unwordban") && !message.StartsWith("!wordlist"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use SpamServ commands.");

            if (message.StartsWith("!badwords"))
                return HandleBadWordsList(ctx, senderNick);

            if (message.StartsWith("!addbadword"))
                return HandleAddBadWord(ctx, senderNick, message);

            if (message.StartsWith("!delbadword"))
                return HandleDelBadWord(ctx, senderNick, message);

            if (message.StartsWith("!wordban"))
                return HandleWordBan(ctx, senderNick, message);

            if (message.StartsWith("!unwordban"))
                return HandleUnwordBan(ctx, senderNick, message);

            if (message.StartsWith("!wordlist"))
                return HandleWordList(ctx, senderNick, false);

            return (false, null);
        }

        public (bool handled, string response) ProcessDiscord(BotContext ctx, string senderNick, string message)
        {
            if (!message.StartsWith("!badwords") && !message.StartsWith("!addbadword") && 
                !message.StartsWith("!delbadword") && !message.StartsWith("!wordban") &&
                !message.StartsWith("!unwordban") && !message.StartsWith("!wordlist"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use SpamServ commands.");

            if (message.StartsWith("!badwords"))
                return HandleBadWordsList(ctx, senderNick);

            if (message.StartsWith("!addbadword"))
                return HandleAddBadWord(ctx, senderNick, message);

            if (message.StartsWith("!delbadword"))
                return HandleDelBadWord(ctx, senderNick, message);

            if (message.StartsWith("!wordban"))
                return HandleWordBan(ctx, senderNick, message);

            if (message.StartsWith("!unwordban"))
                return HandleUnwordBan(ctx, senderNick, message);

            if (message.StartsWith("!wordlist"))
                return HandleWordList(ctx, senderNick, true);

            return (false, null);
        }

        private (bool, string) HandleBadWordsList(BotContext ctx, string senderNick)
        {
            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :listbadwords {ctx.Channel}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} requested badwords list for {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleWordList(BotContext ctx, string senderNick, bool isDiscord)
        {
            string commandId = $"wordlist_{DateTime.UtcNow.Ticks}_{senderNick}";
            ctx.TrackServiceRequest(commandId, senderNick, isDiscord);
            ctx.ServiceResponseTimeouts[commandId] = DateTime.UtcNow;
            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :listbadwords {ctx.Channel}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} requested badwords list for {ctx.Channel} (ID: {commandId})");
            return (false, null);
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
            return (false, null);
        }

        private (bool, string) HandleDelBadWord(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !delbadword <word>");

            string word = parts[1];

            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :delbadword {ctx.Channel} {word}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} removed bad word '{word}' from {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleWordBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !wordban <word> [reason]");

            string word = parts[1];
            string reason = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Spam";

            string wildcardWord = $"*{word}*";
            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :addbadword {ctx.Channel} {wildcardWord} {reason}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} word-banned '{word}' in {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleUnwordBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !unwordban <word>");

            string word = parts[1];
            string wildcardWord = $"*{word}*";

            ctx.Writer?.WriteLine($"PRIVMSG SpamServ :delbadword {ctx.Channel} {wildcardWord}");
            ctx.Logger?.Log($"[SPAMSERV] {senderNick} removed word ban for '{word}' in {ctx.Channel}");
            return (false, null);
        }
    }
}


