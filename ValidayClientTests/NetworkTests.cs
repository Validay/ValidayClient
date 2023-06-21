using Xunit;
using ValidayClient.Network;
using ValidayClient.Managers;
using ValidayClient.Network.Interfaces;
using ValidayClient.Logging;

namespace ValidayClientTests
{
    public class NetworkTests
    {
        [Fact]
        public void CreateDefaultClientSuccess()
        {
            IClient client = new Client();

            Assert.Empty(client.Managers);

            client.RegistrationManager<CommandHandlerManager>();

            Assert.NotNull(client);
            Assert.NotEmpty(client.Managers);
        }

        [Fact]
        public void CreateCustomClientSuccess()
        {
            ClientSettings clientSettings = new ClientSettings(
                "127.0.0.1",
                8888,
                1024,
                new ManagerFactory(),
                new ConsoleLogger(LogType.Info));

            IClient client = new Client(
                clientSettings,
                true);

            Assert.Empty(client.Managers);

            client.RegistrationManager<CommandHandlerManager>();

            Assert.NotNull(client);
            Assert.NotEmpty(client.Managers);
        }

        [Fact]
        public void RegistrationManagerInvalidOperationExceptionAlreadyExistType()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IClient client = new Client();
                CommandHandlerManager commandHandler = new CommandHandlerManager();

                client.RegistrationManager<CommandHandlerManager>();
                client.RegistrationManager(commandHandler);
            });
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
                    new ManagerFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    100000,
                    1024,
                    new ManagerFactory(),
                    new ConsoleLogger(LogType.Info));
            });

            Assert.Throws<FormatException>(() =>
            {
                ClientSettings clientSettings = new ClientSettings(
                    "127.0.0.1",
                    8888,
                    -1,
                    new ManagerFactory(),
                    new ConsoleLogger(LogType.Info));
            });
        }
    }
}