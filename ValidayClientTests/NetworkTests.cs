using Xunit;
using ValidayClient.Network;
using ValidayClient.Managers;
using ValidayClient.Network.Interfaces;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;

namespace ValidayClientTests
{
    public class NetworkTests
    {
        [Fact]
        public void CreateDefaultClientSuccess()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);

            Assert.Empty(client.Managers);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                client,
                logger);

            Assert.NotNull(client);
            Assert.NotEmpty(client.Managers);
        }

        [Fact]
        public void CreateCustomClientSuccess()
        {
            ILogger logger = new ConsoleLogger(LogType.Info);
            ClientSettings clientSettings = new ClientSettings(
                "127.0.0.1",
                8888,
                1024,
                64,
                new byte[]
                {
                    1,
                    2,
                    3
                },
                new ConsoleLogger(LogType.Info));

            IClient client = new Client(
                clientSettings,
                true);

            Assert.Empty(client.Managers);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                client,
                logger);

            Assert.NotNull(client);
            Assert.NotEmpty(client.Managers);
        }

        [Fact]
        public void CreateClientSettingsInvalidParameters()
        {
            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "invalid ip",
                    8888,
                    1024,
                    64,
                    new byte[]
                    {
                        1,
                        2,
                        3
                    },
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    100000,
                    1024,
                    64,
                    new byte[]
                    {
                        1,
                        2,
                        3
                    },
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    8888,
                    -1,
                    64,
                    new byte[]
                    {
                        1,
                        2,
                        3
                    },
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    8888,
                    1024,
                    -1,
                    new byte[]
                    {
                        1,
                        2,
                        3
                    },
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    8888,
                    1024,
                    64,
                    new byte[0],
                    new ConsoleLogger(LogType.Info));
            });
        }
    }
}