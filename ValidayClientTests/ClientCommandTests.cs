using Xunit;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Managers;
using ValidayClient.Network.Interfaces;
using ValidayClient.Network;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Logging;
using ValidayClient.Network.Commands;

namespace ValidayClientTests
{
    public class ClientCommandTests
    {
        class TestClientCommandOne : IClientCommand
        {
            public void Execute(byte[] rawData)
            { }
        }

        class TestClientCommandTwo : IClientCommand
        {
            public void Execute(byte[] rawData)
            { }
        }

        [Fact]
        public void RegistrationCommandSuccess()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                client,
                logger);

            commandHandler.RegistrationCommand<TestClientCommandOne>(1);

            Type type = commandHandler.ClientCommandsMap[1];

            Assert.NotNull(type);
            Assert.NotNull(commandHandler.ClientCommandsMap);
            Assert.NotEmpty(commandHandler.ClientCommandsMap);
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistType()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IClient client = new Client();
                ILogger logger = new ConsoleLogger(LogType.Info);

                CommandHandlerManager commandHandler = new CommandHandlerManager(
                    client,
                    logger);

                commandHandler.RegistrationCommand<TestClientCommandOne>(1);
                commandHandler.RegistrationCommand<TestClientCommandOne>(2);
            });
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistId()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IClient client = new Client();
                ILogger logger = new ConsoleLogger(LogType.Info);

                CommandHandlerManager commandHandler = new CommandHandlerManager(
                    client,
                    logger);

                commandHandler.RegistrationCommand<TestClientCommandOne>(1);
                commandHandler.RegistrationCommand<TestClientCommandTwo>(1);
            });
        }

        [Fact]
        public void CheckCommandExistInPool()
        {
            ICommandPool<ushort, IClientCommand> commandClientPool = new CommandPool<ushort, IClientCommand>();
            IReadOnlyCollection<IManager> managers = new List<IManager>();
            IDictionary<ushort, Type> commandMap = new Dictionary<ushort, Type>()
            {
                {
                    1,
                    typeof(TestClientCommandOne)
                }
            };

            IClientCommand commandFirst = commandClientPool.GetCommand(
                1,
                commandMap);

            commandFirst.Execute(new byte[1]);
            commandClientPool.ReturnCommandToPool(
                1,
                commandFirst,
                commandMap);

            IClientCommand commandSecond = commandClientPool.GetCommand(
                1,
                commandMap);

            Assert.Equal(
                commandFirst,
                commandSecond);
        }
    }
}