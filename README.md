Here’s your **clean rewritten README (full file)**:

---

# CnCNet_Bot_2.0 – IRC Utility Bot (C#)

## Overview

CnCNet_Bot_2.0 is a modular IRC bot built in C# for GameSurge / CnCNet channels.

It supports:

* Command-based architecture
* Admin controls
* Auto-voice system
* Scheduled messages
* Hostmask tracking

The bot is designed to be extendable and easy to maintain.

---

## Features

### Command System

* Each command is a separate `.cs` file inside `/Commands`
* Uses a central `CommandManager`
* No need to touch core logic when adding commands

---

### Admin System

* Admins are stored in `admins.txt`
* One nickname per line

Example:

```
N8Diaz
CO2
```

---

### Auto Voice System

* Admins can assign medals using:

```
!medal <nick> <type>
```

* The bot:

  * Saves hostmask in `voiced.txt`
  * Auto-voices user on join

Medal types:

```
Platinum = 1
Gold     = 2
Silver   = 3
```

---

### Scheduled Messages

* Messages loaded from `messages.txt`
* Automatically sent in channel

Format:

```
Message text <priority>
```

Priority → interval mapping:

```
1 → 50 min
2 → 100 min
3 → 150 min
default → 200 min
```

Example:

```
Welcome to the ladder! 1
Stay active and have fun! 2
```

---

### Hostmask Tracking

* Tracks nick ↔ hostmask mappings in real time
* Used for:

  * Auto voice
  * Future moderation features

---

### Auto Reload System

* Messages reload automatically every 24 hours
* No restart needed

---

## Folder Structure

```
MedalBot/
│
├── Program.cs
├── Commands/
│   ├── CommandManager.cs
│   ├── ICommand.cs
│   ├── GambleCommand.cs
│   ├── MedalCommand.cs
│
├── Services/
│   ├── BotContext.cs
│   ├── AutoMessageService.cs
│   ├── HostmaskTracker.cs
│
├── credentials.ini
├── admins.txt
├── voiced.txt
├── messages.txt
```

---

## Commands

| Command              | Description           | Example            | Access   |
| -------------------- | --------------------- | ------------------ | -------- |
| !gamble <options>    | Random choice         | !gamble Coke Pepsi | Everyone |
| !medal <nick> <type> | Add medal + autovoice | !medal N8Diaz Gold | Admin    |
| !unmedal <nick>      | Remove medal          | !unmedal CO2       | Admin    |

---

## Configuration Files

### credentials.ini

```
[IRC]
Nick=
User=
Pass=
Channel=
ChannelPass=
Server=
Port=
```

---

### admins.txt

* Admin list (one per line)

---

### voiced.txt

* Auto-generated
* Stores hostmasks for voice system

---

### messages.txt

```
Message text <priority>
```

---

## Adding New Commands

1. Create file in `/Commands`:

```
PingCommand.cs
```

2. Implement:

```csharp
public class PingCommand : ICommand
{
    public string Name => "ping";

    public (bool handled, string response) Process(BotContext ctx, string sender, string message, string fullLine)
    {
        if (!message.StartsWith("!ping", StringComparison.OrdinalIgnoreCase))
            return (false, null);

        return (true, $"{sender}, pong!");
    }
}
```

3. Register in `CommandManager.cs`:

```csharp
_commands.Add(new PingCommand());
```

---

## Build & Run

```
dotnet build
dotnet run
```

Or publish:

```
dotnet publish -c Release -r win-x86
```

---

## Notes

* Bot responds only to commands starting with `!`
* Works even with prefixed IRC formatting
* Run from same directory as `.txt` files
* Uses UTC internally

---

## Architecture Notes

### Current State

* Uses shared `BotContext` for state
* Services separated (AutoMessage, Hostmask)
* Commands isolated

### Known Limitations

* No full thread safety yet
* Scheduler is simple loop-based
* No persistence database (file-based only)

---

## Planned Features

* Discord integration (alerts + logs)
* Ident/Nick tracking system
* Moderation alerts (bad words detection)
* Seen system
* Replay / event tracking

---

## Credits

* Developed for CnCNet Red Alert 1 community
* Core system by CO2
* Architecture influence from N8Diaz bot framework

---

If you want next step → I’ll give you **Discord integration file (ready to drop in)**.
