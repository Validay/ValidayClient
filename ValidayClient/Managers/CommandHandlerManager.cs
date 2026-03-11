using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network;
using ValidayClient.Network.Commands;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Network.Interfaces;

namespace ValidayClient.Managers
{
    /// <summary>
    /// Manager that maps incoming command IDs to handler types and executes them via a pool.
    /// Implements ICommandRegistry so other managers can inspect the command map
    /// without depending on this concrete type.
    /// </summary>
    public class CommandHandlerManager : IManager, ICommandRegistry
    {
        /// <inheritdoc/>
        public string Name => nameof(CommandHandlerManager);

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<ushort, Type> CommandsMap
            => new ReadOnlyDictionary<ushort, Type>(_clientCommandsMap);

        private Dictionary<ushort, Type> _clientCommandsMap;
        private ICommandPool<ushort, IClientCommand> _commandPool;
        private IConverterId<ushort> _converterId;
        private IClient? _client;
        private ILogger? _logger;

        /// <summary>
        /// Creates the manager and self-registers into the client.
        /// </summary>
        public CommandHandlerManager(
            IClient client,
            ILogger logger)
                : this(
                      client,
                      logger,
                      new UshortConverterId(),
                      new Dictionary<ushort, Type>())
        { }

        /// <summary>
        /// Creates the manager with explicit dependencies.
        /// </summary>
        public CommandHandlerManager(
            IClient client,
            ILogger logger,
            IConverterId<ushort> converterId,
            Dictionary<ushort, Type> clientCommandsMap)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client),
                    $"{nameof(CommandHandlerManager)}: client is null!");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger),
                    $"{nameof(CommandHandlerManager)}: logger is null!");

            _clientCommandsMap = clientCommandsMap;
            _converterId = converterId;
            _commandPool = new CommandPool<ushort, IClientCommand>();
            _client = client;
            _logger = logger;

            _client.RegisterManager(this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (IsActive)
                return;

            if (_client == null)
            {
                _logger?.Log(
                    $"{nameof(CommandHandlerManager)}: client is null, cannot start.",
                    LogType.Warning);

                return;
            }

            IsActive = true;
            _client.OnReceivedData += OnDataReceived;

            _logger?.Log($"{nameof(CommandHandlerManager)} started!", LogType.Info);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_client == null)
                return;

            IsActive = false;
            _client.OnReceivedData -= OnDataReceived;

            _logger?.Log($"{nameof(CommandHandlerManager)} stopped!", LogType.Info);
        }

        /// <summary>
        /// Registers a command type for the given ID.
        /// </summary>
        /// <typeparam name="T">Command type that implements IClientCommand.</typeparam>
        /// <param name="id">Unique numeric ID for this command.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the ID or the type is already registered.
        /// </exception>
        public virtual void RegisterCommand<T>(ushort id)
            where T : IClientCommand
        {
            if (_clientCommandsMap.ContainsKey(id))
                throw new InvalidOperationException(
                    $"CommandsMap already contains id = {id}!");

            if (_clientCommandsMap.ContainsValue(typeof(T)))
                throw new InvalidOperationException(
                    $"CommandsMap already contains type {typeof(T).Name}!");

            _clientCommandsMap.Add(id, typeof(T));
        }

        private void OnDataReceived(byte[] data)
        {
            if (_client == null)
                return;

            try
            {
                ushort commandId = _converterId.Convert(data);

                if (!_clientCommandsMap.ContainsKey(commandId))
                {
                    _logger?.Log($"Unknown command id={commandId}", LogType.Warning);
                    return;
                }

                IClientCommand command = _commandPool.GetCommand(commandId, _clientCommandsMap);

                command.Execute(data);

                _commandPool.ReturnCommandToPool(commandId, command, _clientCommandsMap);
            }
            catch (Exception ex)
            {
                _logger?.Log($"Command handling failed: {ex.Message}", LogType.Error);
            }
        }
    }
}
