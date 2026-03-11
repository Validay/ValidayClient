using Xunit;
using ValidayClient.Network;
using ValidayClient.Network.Interfaces;
using ValidayClient.Network.Commands;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Managers;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;

namespace ValidayClientTests
{
    public class ClientCommandTests
    {
        class TestCommandOne : IClientCommand
        {
            public bool WasExecuted { get; private set; }
            public byte[]? LastData { get; private set; }

            public void Execute(byte[] rawData)
            {
                WasExecuted = true;
                LastData = rawData;
            }
        }

        class TestCommandTwo : IClientCommand
        {
            public void Execute(byte[] rawData) { }
        }

        static (IClient client, CommandHandlerManager handler) MakeClient()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var handler = new CommandHandlerManager(client, logger);
            return (client, handler);
        }

        [Fact]
        public void RegisterCommand_Success_AppearsInMap()
        {
            var (_, handler) = MakeClient();

            handler.RegisterCommand<TestCommandOne>(1);

            Assert.True(handler.CommandsMap.ContainsKey(1));
            Assert.Equal(typeof(TestCommandOne), handler.CommandsMap[1]);
        }

        [Fact]
        public void RegisterCommand_MultipleCommands_AllPresentInMap()
        {
            var (_, handler) = MakeClient();

            handler.RegisterCommand<TestCommandOne>(1);
            handler.RegisterCommand<TestCommandTwo>(2);

            Assert.Equal(2, handler.CommandsMap.Count);
            Assert.Equal(typeof(TestCommandOne), handler.CommandsMap[1]);
            Assert.Equal(typeof(TestCommandTwo), handler.CommandsMap[2]);
        }

        [Fact]
        public void RegisterCommand_DuplicateId_ThrowsInvalidOperationException()
        {
            var (_, handler) = MakeClient();
            handler.RegisterCommand<TestCommandOne>(1);

            Assert.Throws<InvalidOperationException>(() =>
                handler.RegisterCommand<TestCommandTwo>(1));
        }

        [Fact]
        public void RegisterCommand_DuplicateType_ThrowsInvalidOperationException()
        {
            var (_, handler) = MakeClient();
            handler.RegisterCommand<TestCommandOne>(1);

            Assert.Throws<InvalidOperationException>(() =>
                handler.RegisterCommand<TestCommandOne>(2));
        }

        [Fact]
        public void RegisterCommand_MapIsReadOnly_CannotBeModifiedExternally()
        {
            var (_, handler) = MakeClient();
            handler.RegisterCommand<TestCommandOne>(1);

            var snapshot = handler.CommandsMap;

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<ushort, Type>)snapshot).Add(99, typeof(TestCommandTwo)));
        }

        [Fact]
        public void CommandHandlerManager_Name_IsCorrect()
        {
            var (_, handler) = MakeClient();

            Assert.Equal(nameof(CommandHandlerManager), handler.Name);
        }

        [Fact]
        public void CommandPool_GetCommand_ReturnsCorrectType()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IClientCommand cmd = pool.GetCommand(1, map);

            Assert.NotNull(cmd);
            Assert.IsType<TestCommandOne>(cmd);
        }

        [Fact]
        public void CommandPool_GetCommand_UnknownId_ThrowsKeyNotFoundException()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            Assert.Throws<KeyNotFoundException>(() => pool.GetCommand(99, map));
        }

        [Fact]
        public void CommandPool_AfterReturn_GetCommand_ReturnsSameInstance()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IClientCommand first = pool.GetCommand(1, map);
            pool.ReturnCommandToPool(1, first, map);
            IClientCommand second = pool.GetCommand(1, map);

            Assert.Same(first, second);
        }

        [Fact]
        public void CommandPool_WithoutReturn_GetCommand_ReturnsNewInstance()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IClientCommand first = pool.GetCommand(1, map);
            IClientCommand second = pool.GetCommand(1, map);

            Assert.NotSame(first, second);
        }

        [Fact]
        public void CommandPool_ReturnCommand_UnknownId_ThrowsKeyNotFoundException()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };
            var cmd = new TestCommandOne();

            Assert.Throws<KeyNotFoundException>(() =>
                pool.ReturnCommandToPool(99, cmd, map));
        }

        [Fact]
        public void CommandPool_MultipleTypes_IndependentPools()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type>
            {
                { 1, typeof(TestCommandOne) },
                { 2, typeof(TestCommandTwo) }
            };

            IClientCommand cmdOne = pool.GetCommand(1, map);
            IClientCommand cmdTwo = pool.GetCommand(2, map);

            Assert.IsType<TestCommandOne>(cmdOne);
            Assert.IsType<TestCommandTwo>(cmdTwo);
        }

        [Fact]
        public void CommandPool_ReturnMultiple_GetInSequence_BothReused()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };

            IClientCommand a = pool.GetCommand(1, map);
            IClientCommand b = pool.GetCommand(1, map);
            pool.ReturnCommandToPool(1, a, map);
            pool.ReturnCommandToPool(1, b, map);

            IClientCommand c = pool.GetCommand(1, map);
            IClientCommand d = pool.GetCommand(1, map);

            Assert.True(ReferenceEquals(c, a) || ReferenceEquals(c, b));
            Assert.True(ReferenceEquals(d, a) || ReferenceEquals(d, b));
            Assert.NotSame(c, d);
        }

        [Fact]
        public void CommandPool_ConcurrentGetReturn_DoesNotThrow()
        {
            var pool = new CommandPool<ushort, IClientCommand>();
            var map = new Dictionary<ushort, Type> { { 1, typeof(TestCommandOne) } };
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            {
                try
                {
                    var cmd = pool.GetCommand(1, map);
                    pool.ReturnCommandToPool(1, cmd, map);
                }
                catch (Exception ex) { exceptions.Add(ex); }
            }));

            Task.WaitAll(tasks.ToArray());

            Assert.Empty(exceptions);
        }

        [Fact]
        public void Command_Execute_SetsWasExecuted()
        {
            var cmd = new TestCommandOne();

            cmd.Execute(new byte[] { 1, 2, 3 });

            Assert.True(cmd.WasExecuted);
        }

        [Fact]
        public void Command_Execute_PassesCorrectData()
        {
            var cmd = new TestCommandOne();
            var data = new byte[] { 10, 20, 30 };

            cmd.Execute(data);

            Assert.Equal(data, cmd.LastData);
        }

        [Fact]
        public void Command_Execute_EmptyData_DoesNotThrow()
        {
            var cmd = new TestCommandOne();

            var ex = Record.Exception(() => cmd.Execute(Array.Empty<byte>()));

            Assert.Null(ex);
        }

        [Fact]
        public void Client_OnDisconnected_FiresOnDisconnect()
        {
            IClient client = new Client();
            bool fired = false;
            client.OnDisconnected += () => fired = true;

            client.Disconnect();

            Assert.True(fired);
        }

        [Fact]
        public void Client_OnDisconnected_MultipleSubscribers_AllFire()
        {
            IClient client = new Client();
            int count = 0;
            client.OnDisconnected += () => count++;
            client.OnDisconnected += () => count++;

            client.Disconnect();

            Assert.Equal(2, count);
        }

        [Fact]
        public void Client_Disconnect_SetsIsRunFalse()
        {
            IClient client = new Client();

            client.Disconnect();

            Assert.False(client.IsRun);
        }

        [Fact]
        public void Client_OnReceivedData_CanSubscribeAndUnsubscribe()
        {
            IClient client = new Client();
            int callCount = 0;
            Action<byte[]> handler = _ => callCount++;

            client.OnReceivedData += handler;
            client.OnReceivedData -= handler;

            Assert.Equal(0, callCount);
        }
    }
}
