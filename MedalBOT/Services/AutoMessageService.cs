using System;
using System.Threading;
using System.Threading.Tasks;

namespace MedalBot.Services
{
    public class ScheduledMessage
    {
        public string Text { get; }
        public int IntervalMinutes { get; }

        public ScheduledMessage(string text, int intervalMinutes)
        {
            Text = text;
            IntervalMinutes = intervalMinutes;
        }
    }

    public class AutoMessageService
    {
        private readonly BotContext _ctx;
        private CancellationTokenSource _cts;

        public AutoMessageService(BotContext ctx)
        {
            _ctx = ctx;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => RunLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task RunLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var msg in _ctx.ScheduledMessages)
                {
                    try
                    {
                        await Task.Delay(msg.IntervalMinutes * 60000, token);
                        _ctx.Writer?.WriteLine($"PRIVMSG {_ctx.Channel} :{msg.Text}");
                        Console.WriteLine($"[AutoMsg] Sent: {msg.Text}");
                    }
                    catch (TaskCanceledException) { return; }
                }

                // Reload messages once a day
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), token);
                    _ctx.ReloadMessages?.Invoke();
                }
                catch (TaskCanceledException) { return; }
            }
        }
    }
}