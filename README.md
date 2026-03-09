<div align="center">

# 📡 ValidayClient

**Lightweight, extensible TCP client for .NET**

[![.NET](https://img.shields.io/badge/.NET-netstandard2.1-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-8.0-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-xUnit-blue?style=flat-square)](https://xunit.net)

---

*Designed to pair with [ValidayServer](https://github.com/your-org/ValidayServer).*

</div>

---

## 📋 Table of Contents

- [Features](#-features)
- [Quick Start](#-quick-start)
- [Architecture](#-architecture)
- [Commands](#-commands)
- [Managers](#-managers)
- [Configuration](#-configuration)
- [Logging](#-logging)

---

## ✨ Features

- 🔌 **Async TCP** — non-blocking connect and receive loop
- 🧩 **Manager system** — attach any number of independent managers to the client
- 📦 **Command pool** — reuse command instances to reduce GC pressure
- ⚙️ **Fully configurable** — IP, port, buffer size and more
- 🔍 **ICommandRegistry** — managers can inspect registered commands without coupling to `CommandHandlerManager`
- 🧪 **Testable** — clean interfaces throughout, xUnit test suite included

---

## 🚀 Quick Start

### 1. Create a command

```csharp
using ValidayClient.Network.Commands.Interfaces;

// Incoming command — handles a packet sent by the server
public class ChatMessageCommand : IClientCommand
{
    public void Execute(byte[] rawData)
    {
        string message = Encoding.UTF8.GetString(rawData, 2, rawData.Length - 2);
        Console.WriteLine($"Server says: {message}");
    }
}
```

```csharp
// Outgoing command — builds a packet to send to the server
public class PingServerCommand : IServerCommand
{
    public byte[] GetRawData()
    {
        byte[] id      = BitConverter.GetBytes((ushort)1);
        byte[] payload = Encoding.UTF8.GetBytes("ping");
        return id.Concat(payload).ToArray();
    }
}
```

### 2. Connect to the server

```csharp
using ValidayClient.Network;
using ValidayClient.Managers;
using ValidayClient.Network.Interfaces;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;

IClient client = new Client();
ILogger logger = new ConsoleLogger(LogType.Info);

// Register managers before connecting
CommandHandlerManager commandHandler = new CommandHandlerManager(client, logger);

// Register incoming command handlers
commandHandler.RegistrationCommand<ChatMessageCommand>(1);

// Connect
client.Connect();

while (client.IsRun) { }

client.Disconnect();
```

### 3. Subscribe to events

```csharp
client.OnConnected    += ()      => Console.WriteLine("Connected!");
client.OnDisconnected += ()      => Console.WriteLine("Disconnected.");
client.OnRecivedData  += data    => Console.WriteLine($"Got {data.Length} bytes");
client.OnSendedData   += data    => Console.WriteLine($"Sent {data.Length} bytes");
```

### 4. Send data to the server

```csharp
client.SendToServer(new PingServerCommand());
```

---

## 🏗️ Architecture

```
ValidayClient
├── Network
│   ├── IClient            ← main contract
│   ├── Client             ← TCP implementation (IDisposable)
│   ├── ClientSettings     ← typed configuration (class, not struct)
│   └── UshortConverterId  ← converts first 2 bytes of packet to command ID
│
├── Managers
│   ├── IManager              ← Start / Stop / IsActive / Name
│   ├── ICommandRegistry      ← read-only view of registered commands
│   └── CommandHandlerManager ← routes packets to IClientCommand handlers
│
└── Commands
    ├── IClientCommand   ← Execute(byte[] rawData) — handles incoming packets
    ├── IServerCommand   ← GetRawData() — builds outgoing packets
    └── CommandPool      ← thread-safe object pool per command type
```

### Data flow

```
TCP socket
    │
    ▼
Client.OnDataReceived
    │  fires
    ▼
IClient.OnRecivedData
    │
    └──► CommandHandlerManager
              │  converts first 2 bytes → ushort command ID
              │  looks up IClientCommand in CommandsMap
              ▼
         IClientCommand.Execute(rawData)
              │
              ▼
         CommandPool.ReturnCommandToPool(...)
```

---

## 📨 Commands

### Incoming — `IClientCommand`

Handles packets received **from the server**:

```csharp
public interface IClientCommand
{
    void Execute(byte[] rawData);
}
```

The first **2 bytes** of every packet are treated as a `ushort` command ID. The rest is payload.

**Register before calling `client.Connect()`:**

```csharp
commandHandler.RegistrationCommand<ChatMessageCommand>(1);
commandHandler.RegistrationCommand<PlayerMoveCommand>(2);
commandHandler.RegistrationCommand<PongCommand>(3);
```

Rules:
- Each ID must be unique
- Each command type can only be registered once
- Registration after `Connect()` throws `InvalidOperationException`

### Outgoing — `IServerCommand`

Builds packets to send **to the server**:

```csharp
public interface IServerCommand
{
    byte[] GetRawData();
}
```

```csharp
public class MoveCommand : IServerCommand
{
    private readonly float _x, _y;

    public MoveCommand(float x, float y)
    {
        _x = x;
        _y = y;
    }

    public byte[] GetRawData()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)2); // command ID
        bw.Write(_x);
        bw.Write(_y);
        return ms.ToArray();
    }
}

// usage:
client.SendToServer(new MoveCommand(1.5f, 3.0f));
```

---

## 🧩 Managers

Managers attach to the client's event bus. Any class implementing `IManager` can be registered.

```csharp
public interface IManager
{
    string Name    { get; }
    bool   IsActive { get; }
    void   Start();
    void   Stop();
}
```

### Built-in managers

| Manager | Purpose |
|---|---|
| `CommandHandlerManager` | Routes received packets to registered `IClientCommand` handlers |

### Custom manager example

```csharp
public class HeartbeatManager : IManager
{
    public string Name     => nameof(HeartbeatManager);
    public bool   IsActive { get; private set; }

    private readonly IClient _client;
    private Timer? _timer;

    public HeartbeatManager(IClient client)
    {
        _client = client;
        _client.RegistrationManager(this);
    }

    public void Start()
    {
        IsActive = true;
        _timer = new Timer(_ =>
        {
            if (_client.IsRun)
                _client.SendToServer(new PingServerCommand());
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public void Stop()
    {
        IsActive = false;
        _timer?.Dispose();
        _timer = null;
    }
}
```

> **Note:** `ICommandRegistry` lets your managers inspect registered commands without depending on `CommandHandlerManager` directly:
> ```csharp
> var registry = client.Managers.OfType<ICommandRegistry>().FirstOrDefault();
> bool known = registry?.CommandsMap.ContainsKey(commandId) ?? false;
> ```

---

## ⚙️ Configuration

```csharp
var settings = new ClientSettings(
    ip:                 "192.168.1.10",     // server IP
    port:               7777,               // server port
    bufferSize:         4096,               // receive buffer bytes
    maxDepthReadPacket: 64,                 // packet read depth guard
    markerStartPacket:  new byte[] { 0xFF, 0xFE }, // packet start marker
    logger:             new ConsoleLogger(LogType.Info));

IClient client = new Client(settings, hideSocketError: false);
```

| Parameter | Default | Description |
|---|---|---|
| `Ip` | `127.0.0.1` | Server address to connect to |
| `Port` | `8888` | Server port (0–65535) |
| `BufferSize` | `1024` | Receive buffer in bytes |
| `MaxDepthReadPacket` | `64` | Framing guard depth |
| `MarkerStartPacket` | `{1,2,3}` | Packet boundary marker |

---

## 📝 Logging

Implement `ILogger` to plug in any logging backend:

```csharp
public interface ILogger
{
    void Log(string message, LogType logType);
}
```

`ConsoleLogger` is included out of the box. Filter by level:

```csharp
// Only Warning and above will be printed
var logger = new ConsoleLogger(LogType.Warning);
```

| Level | When to use |
|---|---|
| `Low` | Verbose / per-packet traces |
| `Info` | Connect / disconnect events |
| `Warning` | Unexpected but recoverable events |
| `Error` | Socket failures |
| `CriticalError` | Client cannot connect |
