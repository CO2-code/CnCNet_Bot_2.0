using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MedalBot.Services;

namespace MedalBot
{
    public class BotContext
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Nick { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string Channel { get; set; }
        public string ChannelPass { get; set; }

        public StreamWriter Writer { get; set; }

        public Dictionary<string, int> VoicedUsers { get; set; }
        public Dictionary<string, string> CurrentHostmasks { get; set; }
        public HashSet<string> Admins { get; set; }
        public List<ScheduledMessage> ScheduledMessages { get; set; }

        public Action? ReloadMessages { get; set; }

        public Random Random { get; set; }

        public DiscordService? Discord { get; set; }

        public bool RelayIrcToDiscord { get; set; } = true;
        public bool RelayDiscordToIrc { get; set; } = true;

        public LoggingService Logger { get; set; }

        public List<string> BadWords { get; set; }

        public HashSet<string> CurrentlyVoiced { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Dictionary<string, DateTime>> IdentHistory { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> CommandDescriptions { get; set; }

        public Dictionary<string, string> NickToId { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> MutedIds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, DateTime> TimedMutes { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _voicedLock = new();
        private readonly object _muteLock = new();
        private const string MutedIdsFile = "muted_ids.txt";

        public void SaveVoiced(string file = "voiced.txt")
        {
            lock (_voicedLock)
            {
                File.WriteAllLines(file, VoicedUsers.Select(kv => $"{kv.Key} {kv.Value}"));
            }
        }

        public void LoadMutedIds()
        {
            lock (_muteLock)
            {
                if (!File.Exists(MutedIdsFile))
                {
                    Logger?.Log($"[MUTE LOAD] No muted_ids.txt found, starting fresh");
                    return;
                }

                try
                {
                    var lines = File.ReadAllLines(MutedIdsFile);
                    int loadedCount = 0;

                    foreach (var line in lines)
                    {
                        string trimmed = line?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
                        {
                            MutedIds.Add(trimmed);
                            loadedCount++;
                        }
                    }

                    Logger?.Log($"[MUTE LOAD] Loaded {loadedCount} muted IDs");
                }
                catch (Exception ex)
                {
                    Logger?.Log($"[MUTE LOAD ERROR] Failed to load muted_ids.txt: {ex.Message}");
                }
            }
        }

        public void SaveMutedIds()
        {
            lock (_muteLock)
            {
                try
                {
                    var ids = MutedIds.OrderBy(x => x).ToList();
                    File.WriteAllLines(MutedIdsFile, ids);
                    Logger?.Log($"[MUTE SAVE] Saved {ids.Count} muted IDs");
                }
                catch (Exception ex)
                {
                    Logger?.Log($"[MUTE SAVE ERROR] Failed to save muted_ids.txt: {ex.Message}");
                }
            }
        }

        public string GetSystemId(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick)) return null;
            lock (_muteLock)
            {
                NickToId.TryGetValue(nick, out var id);
                return id;
            }
        }

        public void SetSystemId(string nick, string systemId)
        {
            if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(systemId))
                return;
            lock (_muteLock)
            {
                NickToId[nick] = systemId;
            }
        }

        public void UpdateNick(string oldNick, string newNick)
        {
            if (string.IsNullOrWhiteSpace(oldNick) || string.IsNullOrWhiteSpace(newNick))
                return;
            lock (_muteLock)
            {
                if (NickToId.TryGetValue(oldNick, out var systemId))
                {
                    NickToId.Remove(oldNick);
                    NickToId[newNick] = systemId;
                    Logger?.Log($"[NICK UPDATE] {oldNick} → {newNick} (systemId: {systemId})");
                }
            }
        }

        public void RemoveNick(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick)) return;
            lock (_muteLock)
            {
                NickToId.Remove(nick);
            }
        }

        public bool IsMuted(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId)) return false;
            lock (_muteLock)
            {
                return MutedIds.Contains(systemId);
            }
        }

        public bool AddMute(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId)) return false;
            lock (_muteLock)
            {
                return MutedIds.Add(systemId);
            }
        }

        public bool RemoveMute(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId)) return false;
            lock (_muteLock)
            {
                return MutedIds.Remove(systemId);
            }
        }

        public bool AddTimedMute(string systemId, int durationMinutes)
        {
            if (string.IsNullOrWhiteSpace(systemId) || durationMinutes <= 0) return false;
            lock (_muteLock)
            {
                if (!MutedIds.Add(systemId)) return false;
                TimedMutes[systemId] = DateTime.UtcNow.AddMinutes(durationMinutes);
                return true;
            }
        }

        public bool IsTimedMute(string systemId, out TimeSpan? remaining)
        {
            remaining = null;
            if (string.IsNullOrWhiteSpace(systemId)) return false;
            lock (_muteLock)
            {
                if (TimedMutes.TryGetValue(systemId, out var expireTime))
                {
                    var now = DateTime.UtcNow;
                    if (expireTime > now)
                    {
                        remaining = expireTime - now;
                        return true;
                    }
                    else
                    {
                        TimedMutes.Remove(systemId);
                        MutedIds.Remove(systemId);
                        return false;
                    }
                }
                return false;
            }
        }

        public Dictionary<string, string> GetMutedUsersList(Dictionary<string, string> nickToIdMap)
        {
            lock (_muteLock)
            {
                var result = new Dictionary<string, string>();
                foreach (var systemId in MutedIds)
                {
                    var nickEntry = nickToIdMap.FirstOrDefault(kv => kv.Value == systemId);
                    if (!string.IsNullOrWhiteSpace(nickEntry.Key))
                    {
                        if (CurrentHostmasks.TryGetValue(nickEntry.Key, out string hostmask))
                        {
                            string ident = ExtractIdent(hostmask);
                            result[nickEntry.Key] = ident ?? "unknown";
                        }
                    }
                }
                return result;
            }
        }

        private string ExtractIdent(string hostmask)
        {
            if (string.IsNullOrWhiteSpace(hostmask)) return null;
            int at = hostmask.IndexOf('@');
            if (at <= 0) return null;
            return hostmask.Substring(0, at);
        }
    }
}