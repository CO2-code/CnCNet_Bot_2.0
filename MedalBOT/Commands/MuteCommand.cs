using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class MuteCommand : ICommand
    {
        public string Name => "Mute";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (!message.StartsWith("!mute") && !message.StartsWith("!unmute") && !message.StartsWith("!mutelist"))
                return (false, null);

            var isAdmin = ctx.Admins.Contains(senderNick);
            if (!isAdmin)
                return (true, "You must be an admin to use mute commands.");

            if (message.StartsWith("!mutelist"))
            {
                return HandleMuteList(ctx);
            }

            if (message.StartsWith("!mute"))
            {
                return HandleMute(ctx, senderNick, message);
            }

            if (message.StartsWith("!unmute"))
            {
                return HandleUnmute(ctx, senderNick, message);
            }

            return (false, null);
        }

        private (bool, string) HandleMuteList(BotContext ctx)
        {
            var mutedUsers = ctx.GetMutedUsersList(ctx.NickToId);
            if (mutedUsers.Count == 0)
                return (true, "No muted users.");

            var list = mutedUsers.Select(kv => $"{kv.Key} {kv.Value}");
            return (true, $"Muted: {string.Join(", ", list)}");
        }

        private (bool, string) HandleMute(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !mute <nick> [minutes]");

            string targetNick = parts[1];
            string systemId = ctx.GetSystemId(targetNick);

            if (string.IsNullOrWhiteSpace(systemId))
                return (true, $"User '{targetNick}' not found or has no systemId.");

            bool isTimed = parts.Length > 2 && int.TryParse(parts[2], out int minutes) && minutes > 0;

            if (isTimed && int.TryParse(parts[2], out int durationMinutes) && durationMinutes > 0)
            {
                if (ctx.AddTimedMute(systemId, durationMinutes))
                {
                    ctx.SaveMutedIds();
                    ctx.Logger?.Log($"[MUTE_ADD_TIMED] {senderNick} muted {targetNick} for {durationMinutes}m (systemId: {systemId})");
                    ctx.Writer?.WriteLine($"NOTICE {targetNick} :MUTE_ADD {systemId}");
                    return (true, $"Muted {targetNick} for {durationMinutes} minutes ({systemId})");
                }
                else
                {
                    return (true, $"{targetNick} ({systemId}) is already muted.");
                }
            }
            else
            {
                if (ctx.AddMute(systemId))
                {
                    ctx.SaveMutedIds();
                    ctx.Logger?.Log($"[MUTE_ADD] {senderNick} muted {targetNick} (systemId: {systemId})");
                    ctx.Writer?.WriteLine($"NOTICE {targetNick} :MUTE_ADD {systemId}");
                    return (true, $"Muted {targetNick} ({systemId})");
                }
                else
                {
                    return (true, $"{targetNick} ({systemId}) is already muted.");
                }
            }
        }

        private (bool, string) HandleUnmute(BotContext ctx, string senderNick, string message)
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (true, "Usage: !unmute <nick>");

            string targetNick = parts[1];
            string systemId = ctx.GetSystemId(targetNick);

            if (string.IsNullOrWhiteSpace(systemId))
                return (true, $"User '{targetNick}' not found or has no systemId.");

            if (ctx.RemoveMute(systemId))
            {
                ctx.SaveMutedIds();
                ctx.Logger?.Log($"[MUTE_REMOVE] {senderNick} unmuted {targetNick} (systemId: {systemId})");
                ctx.Writer?.WriteLine($"NOTICE {targetNick} :MUTE_REMOVE {systemId}");
                return (true, $"Unmuted {targetNick} ({systemId})");
            }
            else
            {
                return (true, $"{targetNick} ({systemId}) was not muted.");
            }
        }
    }
}


