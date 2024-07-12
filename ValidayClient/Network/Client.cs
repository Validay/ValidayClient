using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network.Commands.Interfaces;
using ValidayClient.Network.Interfaces;

namespace ValidayClient.Network
{
    /// <summary>
    /// Default client
    /// </summary>
    public class Client : IClient
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsRun => _isRunning;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyCollection<IManager> Managers
        {
            get => _managers
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyDictionary<short, Type> ClientCommandsMap
        {
            get => _clientCommandsMap.ToDictionary(
                command => command.Key,
                command => command.Value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<byte[]>? OnRecivedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action<byte[]>? OnSendedData;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action? OnConnected;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event Action? OnDisconnected;

        private bool _isRunning;
        private bool _hideSocketError;
        private string _ip;
        private int _port;
        private int _bufferSize;
        private Socket _socket;
        private IList<IManager> _managers;
        private ILogger _logger;
        private Dictionary<short, Type> _clientCommandsMap;

        /// <summary>
        /// Default client constructor
        /// </summary>
        public Client() 
            : this(ClientSettings.Default,
                  true)
        { }

        /// <summary>
        /// Constructor with explicit parameters
        /// </summary>
        /// <param name="clientSettings">Client parameters</param>
        /// <param name="hideSocketError">Hide no critical socket errors</param>
        public Client(
            ClientSettings clientSettings,
            bool hideSocketError)
        {
            _hideSocketError = hideSocketError;
            _ip = clientSettings.Ip;
            _port = clientSettings.Port;
            _bufferSize = clientSettings.BufferSize;
            _logger = clientSettings.Logger;
            _clientCommandsMap = new Dictionary<short, Type>();
            _managers = new List<IManager>();
            _socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <exception cref="InvalidOperationException">Already exist manager exception</exception>
        public virtual void RegistrationManager(IManager manager)
        {
            bool hasExisting = _managers.FirstOrDefault(existManager => existManager.Name == manager.Name) != null;

            if (hasExisting)
            {
                _logger?.Log(
                    $"Registration manager failed! Manager [{manager.Name}] already registration!",
                    LogType.Warning);

                throw new InvalidOperationException($"Registration manager failed! Manager [{manager.Name}] already registration!");
            }

            _managers.Add(manager);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Connect()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, _port);

                _managers.ToList()
                    .ForEach(manager =>
                    {
                        manager.Start();
                    });

                _socket.BeginConnect(ipEndPoint, new AsyncCallback(OnConnect), null);

                _isRunning = true;

                _logger?.Log(
                    $"Connected to [{_ip}:{_port}] success!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Connect to [{_ip}:{_port}] failed! {exception.Message}",
                    LogType.Error);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SendToServer(IServerCommand serverCommand)
        {
            try
            {
                byte[] rawData = serverCommand.GetRawData();

                _socket.BeginSend(
                    rawData,
                    0,
                    rawData.Length,
                    SocketFlags.None,
                    new AsyncCallback(OnDataSent),
                    null);

                OnSendedData?.Invoke(rawData);

                _logger?.Log(
                    $"Send {rawData.Length} bytes to [{_ip}:{_port}] success!",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (_hideSocketError)
                    _logger?.Log(
                        $"Send data to [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Low);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Disconnect()
        {
            _managers.ToList()
                .ForEach(manager =>
                {
                    manager.Stop();
                });

            _socket?.Close();
            _socket?.Dispose();
            OnDisconnected?.Invoke();

            _isRunning = false;

            _logger?.Log(
                $"Disconnected from [{_ip}:{_port}]!",
                LogType.Info);
        }

        private void OnConnect(IAsyncResult asyncResult)
        {
            try
            {
                _socket?.EndConnect(asyncResult);
                _socket?.BeginReceive(
                    new byte[] { },
                    0,
                    0,
                    SocketFlags.None,
                    new AsyncCallback(OnDataReceived),
                    null);

                OnConnected?.Invoke();

                _logger?.Log(
                    $"Connect to [{_ip}:{_port}] success!",
                    LogType.Info);
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Connect to [{_ip}:{_port}] failed! {exception.Message}",
                    LogType.Error);
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            try
            {
                _socket.EndReceive(asyncResult);

                byte[] buffer = new byte[_bufferSize];
                int receive = _socket.Receive(
                    buffer,
                    buffer.Length,
                    SocketFlags.None);

                if (receive < buffer.Length)
                    Array.Resize(
                        ref buffer,
                        receive);

                OnRecivedData?.Invoke(buffer);
                _socket.BeginReceive(
                   new byte[] { },
                   0,
                   0,
                   SocketFlags.None,
                   new AsyncCallback(OnDataReceived),
                   _socket);
            }
            catch (Exception exception)
            {
                if (_hideSocketError)
                    _logger?.Log(
                        $"Data receive from [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Low);

                Disconnect();
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            try
            {
                _socket.EndSend(asyncResult);
            }
            catch (Exception exception)
            {
                Disconnect();

                if (_hideSocketError)
                    _logger?.Log(
                        $"Data send to [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Low);
            }
        }
    }
}