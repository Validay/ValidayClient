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

            Assert.NotNull(client);
        }

        [Fact]
        public void CreateCustomClientSuccess()
        {
            var settings = new ClientSettings(
                "127.0.0.1", 8888, 1024, 64,
                new byte[] { 1, 2, 3 }, new ConsoleLogger(LogType.Info));

            IClient client = new Client(settings, true);

            Assert.NotNull(client);
        }

        [Fact]
        public void NewClient_IsRunFalse()
        {
            IClient client = new Client();

            Assert.False(client.IsRun);
        }

        [Fact]
        public void NewClient_ManagersEmpty()
        {
            IClient client = new Client();

            Assert.Empty(client.Managers);
        }

        [Fact]
        public void NewClient_CommandsMap_EmptyViaHandler()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var handler = new CommandHandlerManager(client, logger);

            Assert.Empty(handler.ClientCommandsMap);
        }

        [Fact]
        public void ClientSettingsDefault_HasExpectedValues()
        {
            ClientSettings settings = ClientSettings.Default;

            Assert.Equal("127.0.0.1", settings.Ip);
            Assert.Equal(8888, settings.Port);
            Assert.Equal(1024, settings.BufferSize);
            Assert.NotNull(settings.Logger);
        }

        [Fact]
        public void ClientSettings_IPv6_Success()
        {
            var settings = new ClientSettings(
                "::1", 9000, 512, 32,
                new byte[1], new ConsoleLogger(LogType.Info));

            Assert.Equal("::1", settings.Ip);
        }

        [Fact]
        public void ClientSettings_Port0_Success()
        {
            var settings = new ClientSettings(
                "127.0.0.1", 0, 512, 32,
                new byte[1], new ConsoleLogger(LogType.Info));

            Assert.Equal(0, settings.Port);
        }

        [Fact]
        public void ClientSettings_Port65535_Success()
        {
            var settings = new ClientSettings(
                "127.0.0.1", 65535, 512, 32,
                new byte[1], new ConsoleLogger(LogType.Info));

            Assert.Equal(65535, settings.Port);
        }

        [Fact]
        public void ClientSettings_BufferSize0_Success()
        {
            var settings = new ClientSettings(
                "127.0.0.1", 8888, 0, 32,
                new byte[1], new ConsoleLogger(LogType.Info));

            Assert.Equal(0, settings.BufferSize);
        }

        [Theory]
        [MemberData(nameof(InvalidClientSettingsData))]
        public void ClientSettings_InvalidParameters_ThrowsFormatException(
            string ip, int port, int buffer, int maxDepth, byte[] marker)
        {
            Assert.Throws<FormatException>(() =>
                new ClientSettings(ip, port, buffer, maxDepth,
                    marker, new ConsoleLogger(LogType.Info)));
        }

        public static IEnumerable<object[]> InvalidClientSettingsData()
        {
            var ip = "127.0.0.1";
            var port = 8888;
            var buf = 1024;
            var depth = 64;
            var marker = new byte[] { 1, 2, 3 };

            yield return new object[] { "not-an-ip", port, buf, depth, marker };
            yield return new object[] { "", port, buf, depth, marker };
            yield return new object[] { ip, 100000, buf, depth, marker };
            yield return new object[] { ip, -1, buf, depth, marker };
            yield return new object[] { ip, port, -1, depth, marker };
            yield return new object[] { ip, port, buf, -1, marker };
            yield return new object[] { ip, port, buf, depth, new byte[0] };
        }

        [Fact]
        public void RegistrationManager_AddsToCollection()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);

            new CommandHandlerManager(client, logger);

            Assert.Single(client.Managers);
        }

        [Fact]
        public void RegistrationManager_DuplicateName_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                IClient client = new Client();
                ILogger logger = new ConsoleLogger(LogType.Info);

                new CommandHandlerManager(client, logger);
                new CommandHandlerManager(client, logger);
            });
        }

        [Fact]
        public void RegistrationManager_ManagerAppearsInCollection()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);

            var manager = new CommandHandlerManager(client, logger);

            Assert.Contains(client.Managers, m => m.Name == manager.Name);
        }

        [Fact]
        public void CommandHandlerManager_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandHandlerManager(null!, new ConsoleLogger(LogType.Info)));
        }

        [Fact]
        public void CommandHandlerManager_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandHandlerManager(new Client(), null!));
        }

        [Fact]
        public void Manager_NewInstance_IsActiveFalse()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new CommandHandlerManager(client, logger);

            Assert.False(manager.IsActive);
        }

        [Fact]
        public void Manager_AfterStart_IsActiveTrue()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new CommandHandlerManager(client, logger);

            manager.Start();

            Assert.True(manager.IsActive);
        }

        [Fact]
        public void Manager_AfterStop_IsActiveFalse()
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var manager = new CommandHandlerManager(client, logger);

            manager.Start();
            manager.Stop();

            Assert.False(manager.IsActive);
        }

        [Fact]
        public void ConsoleLogger_LogLevelSetsCorrectly()
        {
            var logger = new ConsoleLogger(LogType.Error);

            Assert.Equal(LogType.Error, logger.LogLevel);
        }

        [Theory]
        [InlineData(LogType.Low)]
        [InlineData(LogType.Info)]
        [InlineData(LogType.Warning)]
        [InlineData(LogType.Error)]
        [InlineData(LogType.CriticalError)]
        public void ConsoleLogger_DoesNotThrow_ForAnyLogType(LogType logType)
        {
            var logger = new ConsoleLogger(LogType.Low);

            var ex = Record.Exception(() => logger.Log("test", logType));

            Assert.Null(ex);
        }

        [Fact]
        public void ConsoleLogger_MessageFilteredBelowLevel_DoesNotThrow()
        {
            var logger = new ConsoleLogger(LogType.CriticalError);

            var ex = Record.Exception(() => logger.Log("should be filtered", LogType.Low));

            Assert.Null(ex);
        }

        [Fact]
        public void UshortConverterId_ConvertsCorrectly()
        {
            var converter = new UshortConverterId();
            byte[] bytes = BitConverter.GetBytes((ushort)123);

            Assert.Equal((ushort)123, converter.Convert(bytes));
        }

        [Fact]
        public void UshortConverterId_Zero()
        {
            var converter = new UshortConverterId();
            byte[] bytes = BitConverter.GetBytes((ushort)0);

            Assert.Equal((ushort)0, converter.Convert(bytes));
        }

        [Fact]
        public void UshortConverterId_MaxValue()
        {
            var converter = new UshortConverterId();
            byte[] bytes = BitConverter.GetBytes(ushort.MaxValue);

            Assert.Equal(ushort.MaxValue, converter.Convert(bytes));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(255)]
        [InlineData(1000)]
        [InlineData(60000)]
        public void UshortConverterId_RoundTrip(ushort value)
        {
            var converter = new UshortConverterId();
            byte[] bytes = BitConverter.GetBytes(value);

            Assert.Equal(value, converter.Convert(bytes));
        }
    }
}