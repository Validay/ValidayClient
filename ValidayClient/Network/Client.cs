using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers.Interfaces;
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
        private int _maxDepthReadPacket;
        private byte[] _buffer;
        private Socket _socket;
        private IList<IManager> _managers;
        private ILogger _logger;
        private IManagerFactory _managerFactory;
        private Dictionary<short, Type> _clientCommandsMap;

        private readonly byte[] _markerStartPacket;

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
            _buffer = new byte[clientSettings.BufferSize];
            _logger = clientSettings.Logger;
            _managerFactory = clientSettings.ManagerFactory;
            _clientCommandsMap = new Dictionary<short, Type>();
            _managers = new List<IManager>();
            _socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            _markerStartPacket = new byte[]
            {
                1,
                2,
                3
            };
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public virtual void RegistrationManager<T>()
            where T : IManager
        {
            try
            {
                IManager newManager = _managerFactory.CreateManager<T>();
                bool hasExisting = _managers.FirstOrDefault(manager => manager.Name == newManager.Name) != null;

                if (hasExisting)
                {
                    _logger?.Log(
                        $"Registration manager failed! Manager [{newManager.Name}] already registration!",
                        LogType.Warning);

                    return;
                }

                newManager.Initialize(
                    this,
                    _logger);

                _managers.Add(newManager);
            }
            catch (Exception exception)
            {
                _logger?.Log(
                    $"Registration manager failed! {exception.Message}",
                    LogType.Error);
            }
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

            manager.Initialize(
                this,
                _logger);

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
        public void SendToServer(byte[] rawData)
        {
            try
            {
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
                    _buffer,
                    0,
                    _buffer.Length,
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
                int bytesRead = _socket.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    byte[] receivedData = new byte[bytesRead];

                    Array.Copy(
                        _buffer,
                        receivedData, 
                        bytesRead);

                    ProcessReceivedData(receivedData);

                    if (asyncResult.CompletedSynchronously)
                    {
                        while (bytesRead > 0)
                        {
                            ProcessReceivedData(receivedData);

                            bytesRead = _socket.Receive(_buffer);
                        }
                    }
                    else
                    {
                        _socket.BeginReceive(
                            _buffer,
                            0,
                            _buffer.Length,
                            SocketFlags.None,
                            new AsyncCallback(OnDataReceived),
                            _socket);
                    }
                }
                else
                {
                    Disconnect();
                }             
            }
            catch (Exception exception)
            {
                Disconnect();

                if (_hideSocketError)
                    _logger?.Log(
                        $"Data receive from [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Low);
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

        private void ProcessReceivedData(
            byte[] receivedData,
            int depth = 0)
        {
            int startMarkerIndex = FindSequence(
                receivedData,
                _markerStartPacket);

            if (startMarkerIndex != -1)
            {
                if (receivedData.Length >= startMarkerIndex + _markerStartPacket.Length + sizeof(int))
                {
                    byte[] sizeIndicator = new byte[sizeof(int)];

                    Array.Copy(
                        receivedData,
                        startMarkerIndex + _markerStartPacket.Length,
                        sizeIndicator,
                        0,
                        sizeof(int));

                    int packetSize = BitConverter.ToInt32(
                        sizeIndicator,
                        0);

                    if (receivedData.Length >= startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize)
                    {
                        byte[] packetData = new byte[packetSize];

                        Array.Copy(
                            receivedData,
                            startMarkerIndex + _markerStartPacket.Length + sizeof(int),
                            packetData,
                            0,
                            packetSize);

                        OnRecivedData?.Invoke(packetData);

                        _logger?.Log(
                            $"Received data [{packetSize} bytes] from server",
                            LogType.Low);

                        byte[] remainingData = new byte[receivedData.Length - (startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize)];

                        Array.Copy(
                            receivedData,
                            startMarkerIndex + _markerStartPacket.Length + sizeof(int) + packetSize,
                            remainingData,
                            0,
                            remainingData.Length);

                        if (remainingData.Length > 0)
                        {
                            if (depth < _maxDepthReadPacket)
                            {
                                ProcessReceivedData(
                                    remainingData,
                                    depth++);
                            }
                            else
                            {
                                _logger?.Log(
                                    $"Packet read depth exceeded!",
                                    LogType.Warning);

                                Disconnect();
                            }
                        }
                    }
                }
            }
        }

        private int FindSequence(
            byte[] data,
            byte[] sequence)
        {
            for (int i = 0; i <= data.Length - sequence.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < sequence.Length; j++)
                {
                    if (data[i + j] != sequence[j])
                    {
                        found = false;

                        break;
                    }
                }

                if (found)
                    return i;
            }

            return -1;
        }
    }
}