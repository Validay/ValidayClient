# ValidayClient
  
  ### Source light client
  
  ![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/Validay/ValidayClient)
  ![GitHub commit activity](https://img.shields.io/github/commit-activity/t/Validay/ValidayClient)
  ![GitHub last commit](https://img.shields.io/github/last-commit/Validay/ValidayClient)

## Roadmap:
  - [ ] Caching commands

## How to use:
### 1. Create client settings
<pre>
ClientSettings settings = new ClientSettings
{
    IP = "127.0.0.1",
    BufferSize = 1024,
    Port = 8888,
    ManagerFactory = new ManagerFactory(),
    Logger = new ConsoleLogger(LogType.Info),
};
</pre>

### 2. Create instance client
<pre>
IClient client = new Client(
  settings, 
  true);
</pre>

### 3. Registration managers
<pre>
client.RegistrationManager&ltSomeManager>();
</pre>

### 4. Connect to server!
<pre>
client.Connect();
</pre>
