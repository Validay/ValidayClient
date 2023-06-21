using Xunit;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Managers;

namespace ValidayClientTests
{
    public class ClientCommandTests
    {
        class TestClientCommandOne : IClientCommand
        {
            public void Execute(
                IReadOnlyCollection<IManager> managers,
                byte[] rawData)
            { }
        }

        class TestClientCommandTwo : IClientCommand
        {
            public void Execute(
                IReadOnlyCollection<IManager> managers,
                byte[] rawData)
            { }
        }

        [Fact]
        public void RegistrationCommandSuccess()
        {
            CommandHandlerManager commandHandler = new CommandHandlerManager();

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
                CommandHandlerManager commandHandler = new CommandHandlerManager();

                commandHandler.RegistrationCommand<TestClientCommandOne>(1);
                commandHandler.RegistrationCommand<TestClientCommandOne>(2);
            });
        }

        [Fact]
        public void RegistrationCommandInvalidOperationExceptionAlreadyExistId()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                CommandHandlerManager commandHandler = new CommandHandlerManager();

                commandHandler.RegistrationCommand<TestClientCommandOne>(1);
                commandHandler.RegistrationCommand<TestClientCommandTwo>(1);
            });
        }
    }
}