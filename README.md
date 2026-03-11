# ValidayClient

![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/Validay/ValidayClient)
![GitHub commit activity](https://img.shields.io/github/commit-activity/t/Validay/ValidayClient)
![GitHub last commit](https://img.shields.io/github/last-commit/Validay/ValidayClient)

Lightweight TCP socket client library for .NET. Designed around a command-handler pattern: incoming byte streams are routed to typed command handlers through a thread-safe object pool.

## Features

- Async TCP socket connection (`BeginConnect` / `BeginReceive`)
- Command pool — command instances are reused, no per-message allocation
- Manager system — extend behavior by registering independent managers
- Pluggable logger (`ILogger`) and ID converter (`IConverterId<T>`)
- IPv4 and IPv6 support

## Installation

Install via NuGet:

```
dotnet add package ValidayClient
```

## Quick start

### 1. Implement your commands

Commands received **from** the server implement `IClientCommand`:

```csharp
public class ChatMessageCommand : IClientCommand
{
    public void Execute(byte[] rawData)
    {
        string message = Encoding.UTF8.GetString(rawData, 2, rawData.Length - 2);
        Console.WriteLine($"[Chat] {message}");
    }
}
```

Commands sent **to** the server implement `IServerCommand`:

```csharp
public class PingCommand : IServerCommand
{
    public byte[] GetRawData()
    {
        return BitConverter.GetBytes((ushort)1);
    }
}
```

### 2. Configure and create the client

```csharp
ClientSettings settings = new ClientSettings(
    ip: "127.0.0.1",
    port: 8888,
    bufferSize: 1024,
    maxDepthReadPacket: 64,
    markerStartPacket: new byte[] { 1, 2, 3 },
    logger: new ConsoleLogger(LogType.Info));

IClient client = new Client(settings, hideSocketError: true);
```

Or use defaults (localhost:8888, 1024-byte buffer):

```csharp
IClient client = new Client();
```

### 3. Register managers

```csharp
ILogger logger = new ConsoleLogger(LogType.Info);

CommandHandlerManager commandHandler = new CommandHandlerManager(client, logger);
commandHandler.RegisterCommand<ChatMessageCommand>(id: 42);
```

Managers register themselves into the client automatically.

### 4. Subscribe to events (optional)

```csharp
client.OnConnected    += () => Console.WriteLine("Connected!");
client.OnDisconnected += () => Console.WriteLine("Disconnected.");
client.OnReceivedData += data => Console.WriteLine($"Received {data.Length} bytes");
client.OnSentData     += data => Console.WriteLine($"Sent {data.Length} bytes");
```

### 5. Connect

```csharp
client.Connect();
```

### 6. Send commands

```csharp
client.SendToServer(new PingCommand());
```

### 7. Disconnect

```csharp
client.Disconnect();
```

## Custom managers

Implement `IManager` to add your own logic (e.g. heartbeat, reconnect, state sync):

```csharp
public class HeartbeatManager : IManager
{
    public string Name => nameof(HeartbeatManager);
    public bool IsActive { get; private set; }

    private readonly IClient _client;

    public HeartbeatManager(IClient client)
    {
        _client = client;
        _client.RegisterManager(this);
    }

    public void Start()
    {
        IsActive = true;
        // start heartbeat timer...
    }

    public void Stop()
    {
        IsActive = false;
        // stop heartbeat timer...
    }
}
```

## Log levels

| Level           | Usage                           |
|-----------------|---------------------------------|
| `Low`           | High-frequency internal events  |
| `Info`          | Connection lifecycle            |
| `Warning`       | Unknown commands, soft errors   |
| `Error`         | Recoverable failures            |
| `CriticalError` | Fatal connection errors         |

Supply a custom `ILogger` to redirect output to any logging framework.

## Roadmap

- [ ] Packet framing with `MarkerStartPacket` / `MaxDepthReadPacket`
- [ ] Reconnect policy
