using System;
using System.Collections.Generic;
using System.Linq;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network.Interfaces;
using ValidayClient.Network.Commands.Interfaces;

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
        public IReadOnlyDictionary<short, Type> ClientCommandsMap
        {
            get => _clientCommandsMap.ToDictionary(
                command => command.Key,
                command => command.Value);
        }

        private Dictionary<short, Type> _clientCommandsMap;
        private IClient? _client;
        private ILogger? _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandHandlerManager()
            : this(new Dictionary<short, Type>())
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="clientCommandsMap">Client commands</param>
        public CommandHandlerManager(Dictionary<short, Type> clientCommandsMap)
        {
            _clientCommandsMap = clientCommandsMap;
        }

        /// <summary>
        /// Registration new command type
        /// </summary>
        /// <typeparam name="T">Type command</typeparam>
        /// <param name="id">Id command</param>
        /// <exception cref="InvalidOperationException">Already exist command exception</exception>
        public virtual void RegistrationCommand<T>(short id)
            where T : IClientCommand
        {
            if (_clientCommandsMap == null)
                _clientCommandsMap = new Dictionary<short, Type>();

            if (_clientCommandsMap.ContainsKey(id))
                throw new InvalidOperationException($"ClientCommandsMap already exists this id = {id}!");

            if (_clientCommandsMap.ContainsValue(typeof(T)))
                throw new InvalidOperationException($"ClientCommandsMap already exists this type {nameof(T)}!");

            _clientCommandsMap.Add(id, typeof(T));
        }

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