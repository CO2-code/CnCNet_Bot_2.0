using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class MuteCommand : ICommand
    {
        public string Name => "Mute";

        public (bool handled, string response) Process(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (!message.StartsWith("!mute") && !message.StartsWith("!unmute"))
                return (false, null);

            var isAdmin = ctx.Admins.Contains(senderNick);
            if (!isAdmin)
                return (true, "You must be an admin to use mute commands.");

            if (message.StartsWith("!mute"))
            {
                var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return (true, "Usage: !mute <nick>");

                string targetNick = parts[1];
                string systemId = ctx.GetSystemId(targetNick);

                if (string.IsNullOrWhiteSpace(systemId))
                    return (true, $"User '{targetNick}' not found or has no systemId.");

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

            if (message.StartsWith("!unmute"))
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

            return (false, null);
        }
    }
}

