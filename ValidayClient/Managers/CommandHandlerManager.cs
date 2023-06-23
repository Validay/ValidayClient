using System;
using System.Collections.Generic;
using System.Linq;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network.Interfaces;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Network.Commands;
using ValidayClient.Network;

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

        /// <summary>
        /// Get all client commands
        /// </summary>
        public IReadOnlyDictionary<ushort, Type> ClientCommandsMap
        {
            get => _clientCommandsMap.ToDictionary(
                command => command.Key,
                command => command.Value);
        }

        private Dictionary<ushort, Type> _clientCommandsMap;
        private ICommandPool<ushort, IClientCommand> _commandClientPool;
        private IConverterId<ushort> _converterId;
        private IClient? _client;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">Instance client where register this manager</param>
        /// <param name="logger">Instance logger fot this manager</param>
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
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="client">Instance client where register this manager</param>
        /// <param name="logger">Instance logger fot this manager</param>
        /// <param name="converterId">Converter id from bytes</param>
        /// <param name="clientCommandsMap">Client commands</param>
        /// <exception cref="NullReferenceException">Exception null parameters</exception>
        public CommandHandlerManager(
            IClient client,
            ILogger logger,
            IConverterId<ushort> converterId,
            Dictionary<ushort, Type> clientCommandsMap)
        {
            _commandClientPool = new CommandPool<ushort, IClientCommand>();
            _converterId = converterId;
            _clientCommandsMap = clientCommandsMap;
            _client = client;
            _logger = logger;

            if (_client == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Client is null!");

            if (_logger == null)
                throw new NullReferenceException($"{nameof(CommandHandlerManager)}: Logger is null!");

            _client.RegistrationManager(this);
        }

        /// <summary>
        /// Registration new command type
        /// </summary>
        /// <typeparam name="T">Type command</typeparam>
        /// <param name="id">Id command</param>
        /// <exception cref="InvalidOperationException">Already exist command exception</exception>
        public virtual void RegistrationCommand<T>(ushort id)
            where T : IClientCommand
        {
            if (_clientCommandsMap == null)
                _clientCommandsMap = new Dictionary<ushort, Type>();

            if (_clientCommandsMap.ContainsKey(id))
                throw new InvalidOperationException($"ClientCommandsMap already exists this id = {id}!");

            if (_clientCommandsMap.ContainsValue(typeof(T)))
                throw new InvalidOperationException($"ClientCommandsMap already exists this type {nameof(T)}!");

            _clientCommandsMap.Add(id, typeof(T));
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

            ushort commandId = _converterId.Convert(data);
            IClientCommand command = _commandClientPool.GetCommand(
                commandId,
                _clientCommandsMap);

            command.Execute(
                _client.Managers,
                data);

            _commandClientPool.ReturnCommandToPool(
                commandId,
                command,
                _clientCommandsMap);
        }
    }
}