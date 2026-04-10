using System;
using System.Linq;

namespace MedalBot.Commands
{
    public class HelpCommand : ICommand
    {
        public string Name => "help";

        public (bool handled, string response) Process(BotContext ctx, string sender, string message, string fullLine)
        {
            if (!message.Equals("!help", StringComparison.OrdinalIgnoreCase))
                return (false, null);

            if (!ctx.Admins.Contains(sender))
                return (true, "Admin only.");

            if (ctx.CommandDescriptions == null || ctx.CommandDescriptions.Count == 0)
                return (true, "No commands configured.");

            var lines = ctx.CommandDescriptions.Values
                .Where(v => !string.IsNullOrWhiteSpace(v));

            return (true, string.Join("\n", lines));
        }
    }
}
