using System;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network.Interfaces;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayClient.Managers
{
    /// <summary>
    /// Manager for handle and execute commands
    /// </summary>
    public class CommandHandlerManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name { get => nameof(CommandHandlerManager); }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private IClient? _client;
        private ILogger? _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Initialize(
            IClient client,
            ILogger logger)
        {
            _client = client;
            _logger = logger;

            if (_client == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Client is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Logger is null!");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (_client == null)
            {
                _logger?.Log(
                    $"{nameof(CommandHandlerManager)}: _client is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _client.OnRecivedData += OnDataReceived;

            _logger?.Log(
                $"{nameof(CommandHandlerManager)} started!",
                LogType.Info);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Stop()
        {
            if (_client == null)
                return;

            IsActive = false;

            _client.OnRecivedData -= OnDataReceived;

            _logger?.Log(
                 $"{nameof(CommandHandlerManager)} stopped!",
                LogType.Info);
        }

        private void OnDataReceived(byte[] data)
        {
            if (_client == null)
                return;

            short commandId = BitConverter.ToInt16(data, 0);

            if (_client.ClientCommandsMap.TryGetValue(
                commandId,
                out Type commandType))
            {
                if (commandType == null)
                    return;

                var command = Activator.CreateInstance(commandType)
                    as IClientCommand;

                command?.Execute(
                    _client.Managers,
                    data);
            }
        }
    }
}