using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MedalBot.Services
{
    public class AutoMessageService
    {
        private readonly BotContext _ctx;
        private readonly List<MessageRunner> _runners = new();
        private FileSystemWatcher _watcher;

        public AutoMessageService(BotContext ctx)
        {
            _ctx = ctx;
        }

        public void Start()
        {
            Stop(); // stop previous runners
            StartMessageLoops();

            _watcher = new FileSystemWatcher(".", "messages.txt")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watcher.Changed += OnMessagesFileChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            foreach (var runner in _runners)
                runner.Cancel();
            _runners.Clear();

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void Reload()
        {
            OnMessagesFileChanged(this, null);
        }

        private void StartMessageLoops()
        {
            foreach (var msg in _ctx.ScheduledMessages)
            {
                var runner = new MessageRunner(msg, _ctx);
                _runners.Add(runner);
                runner.Start();
            }
        }

        private void OnMessagesFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Console.WriteLine("[AutoMsg] messages.txt changed, reloading...");
                Stop();
                _ctx.ReloadMessages?.Invoke();
                StartMessageLoops();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoMsg] Error reloading messages: {ex.Message}");
            }
        }

        private class MessageRunner
        {
            private readonly ScheduledMessage _msg;
            private readonly BotContext _ctx;
            private CancellationTokenSource _cts;

            public MessageRunner(ScheduledMessage msg, BotContext ctx)
            {
                _msg = msg;
                _ctx = ctx;
            }

            public void Start()
            {
                _cts = new CancellationTokenSource();
                Task.Run(RunLoop);
            }

            public void Cancel()
            {
                _cts?.Cancel();
            }

            private async Task RunLoop()
            {
                int intervalMs = _msg.IntervalMinutes * 60000;
                var token = _cts.Token;
                var nextSendTime = DateTime.UtcNow.AddMilliseconds(intervalMs);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        int delay = (int)Math.Max((nextSendTime - DateTime.UtcNow).TotalMilliseconds, 0);
                        await Task.Delay(delay, token);

                        _ctx.Writer?.WriteLine($"PRIVMSG {_ctx.Channel} :{_msg.Text}");
                        Console.WriteLine($"[AutoMsg] Sent: {_msg.Text}");

                        nextSendTime = nextSendTime.AddMilliseconds(intervalMs);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AutoMsg] Error sending message: {ex.Message}");
                        nextSendTime = DateTime.UtcNow.AddMilliseconds(intervalMs); // skip to next interval
                    }
                }
            }
        }
    }
}
