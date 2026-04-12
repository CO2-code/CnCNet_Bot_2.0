using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class ChanServCommand : ICommand
    {
        public string Name => "ChanServ";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            // !blist is Discord-only, handled via ProcessDiscord
            // Only allow !addban, !delban, !tb, !nickban from IRC
            if (!message.StartsWith("!addban") && !message.StartsWith("!delban") && 
                !message.StartsWith("!tb") && !message.StartsWith("!nickban"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use ChanServ commands.");

            if (message.StartsWith("!addban"))
                return HandleAddBan(ctx, senderNick, message);

            if (message.StartsWith("!delban"))
                return HandleDelBan(ctx, senderNick, message);

            if (message.StartsWith("!tb"))
                return HandleTimedBan(ctx, senderNick, message);

            if (message.StartsWith("!nickban"))
                return HandleNickBan(ctx, senderNick, message);

            return (false, null);
        }

        public (bool handled, string response) ProcessDiscord(BotContext ctx, string senderNick, string message)
        {
            // Discord can use !blist and other commands
            if (!message.StartsWith("!blist") && !message.StartsWith("!addban") && 
                !message.StartsWith("!delban") && !message.StartsWith("!tb") && 
                !message.StartsWith("!nickban"))
                return (false, null);

            if (!ctx.Admins.Contains(senderNick))
                return (true, "You must be an admin to use ChanServ commands.");

            if (message.StartsWith("!blist"))
                return HandleBanList(ctx, senderNick, true);

            if (message.StartsWith("!addban"))
                return HandleAddBan(ctx, senderNick, message);

            if (message.StartsWith("!delban"))
                return HandleDelBan(ctx, senderNick, message);

            if (message.StartsWith("!tb"))
                return HandleTimedBan(ctx, senderNick, message);

            if (message.StartsWith("!nickban"))
                return HandleNickBan(ctx, senderNick, message);

            return (false, null);
        }

        private (bool, string) HandleBanList(BotContext ctx, string senderNick, bool isDiscord)
        {
            string commandId = $"blist_{DateTime.UtcNow.Ticks}_{senderNick}";
            ctx.TrackServiceRequest(commandId, senderNick, isDiscord);
            ctx.ServiceResponseTimeouts[commandId] = DateTime.UtcNow;
            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :bans {ctx.Channel}");
            ctx.Logger?.Log($"[CHANSERV] {senderNick} requested banlist for {ctx.Channel} (ID: {commandId})");
            return (false, null);
        }

        private (bool, string) HandleAddBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !addban <mask|nick> [reason]");

            string target = parts[1];
            string reason = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "No reason";

            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :addban {ctx.Channel} {target} {reason}");
            ctx.Logger?.Log($"[CHANSERV] {senderNick} added ban for {target} in {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleDelBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !delban <mask|nick>");

            string target = parts[1];

            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :delban {ctx.Channel} {target}");
            ctx.Logger?.Log($"[CHANSERV] {senderNick} removed ban for {target} in {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleTimedBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return (true, "Usage: !tb <mask|nick> <duration> [reason] (e.g., 15m, 1h, 7d)");

            string target = parts[1];
            string duration = parts[2];
            string reason = parts.Length > 3 ? string.Join(" ", parts.Skip(3)) : "Temporary ban";

            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :addtimedban {ctx.Channel} {target} {duration} {reason}");
            ctx.Logger?.Log($"[CHANSERV] {senderNick} added timed ban for {target} ({duration}) in {ctx.Channel}");
            return (false, null);
        }

        private (bool, string) HandleNickBan(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !nickban <nick> [reason]");

            string nick = parts[1];
            string reason = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Please change your nickname";

            string mask = $"*{nick}*!*@*";
            ctx.Writer?.WriteLine($"PRIVMSG ChanServ :addban {ctx.Channel} {mask} {reason}");
            ctx.Logger?.Log($"[CHANSERV] {senderNick} nick-banned {nick} in {ctx.Channel}");
            return (false, null);
        }
    }
}





