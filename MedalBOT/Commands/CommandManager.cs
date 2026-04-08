using System.Collections.Generic;

namespace MedalBot.Commands
{
    public class CommandManager
    {
        private readonly List<ICommand> _commands = new();

        public CommandManager()
        {
            _commands.Add(new GambleCommand());
            _commands.Add(new MedalCommand());
            _commands.Add(new CommandsCommand());
            _commands.Add(new MuteCommand());
        }

        public string TryProcess(BotContext ctx, string senderNick, string message, string fullLine)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            int bangIndex = message.IndexOf('!');
            if (bangIndex >= 0) message = message.Substring(bangIndex);

            foreach (var cmd in _commands)
            {
                var (handled, response) = cmd.Process(ctx, senderNick, message, fullLine);
                if (handled && !string.IsNullOrEmpty(response))
                    return response;
            }

            return null;
        }
    }
}