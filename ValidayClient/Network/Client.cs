using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Default TCP client implementation.
    /// Implements IDisposable because it owns a Socket.
    /// </summary>
    public class Client : IClient, IDisposable
    {
        /// <inheritdoc/>
        public bool IsRun => _isRunning;

        /// <inheritdoc/>
        public IReadOnlyCollection<IManager> Managers { get; private set; }

        /// <inheritdoc/>
        public event Action<byte[]> OnRecivedData = delegate { };

        /// <inheritdoc/>
        public event Action<byte[]> OnSendedData = delegate { };

        /// <inheritdoc/>
        public event Action OnConnected = delegate { };

        /// <inheritdoc/>
        public event Action OnDisconnected = delegate { };

        private bool _isRunning;
        private bool _disposed;
        private bool _hideSocketError;
        private string _ip;
        private int _port;
        private int _bufferSize;
        private Socket? _socket;
        private IList<IManager> _managers;
        private ILogger _logger;

        /// <summary>
        /// Creates a client with default settings.
        /// </summary>
        public Client() 
            : this(
                  ClientSettings.Default, 
                  hideSocketError: true) 
        { }

        /// <summary>
        /// Creates a client with explicit settings.
        /// </summary>
        /// <param name="settings">Configuration to use.</param>
        /// <param name="hideSocketError">When true, non-critical socket errors are suppressed from the log.</param>
        public Client(
            ClientSettings settings,
            bool hideSocketError)
        {
            _hideSocketError = hideSocketError;
            _ip = settings.Ip;
            _port = settings.Port;
            _bufferSize = settings.BufferSize;
            _logger = settings.Logger;
            _managers = new List<IManager>();
            Managers = new ReadOnlyCollection<IManager>(_managers);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a manager with the same name is already registered,
        /// or if the client is already running.
        /// </exception>
        public virtual void RegistrationManager(IManager manager)
        {
            if (_isRunning)
                throw new InvalidOperationException(
                    $"Cannot register manager [{manager.Name}] after the client has connected.");

            bool alreadyExists = _managers.Any(m => m.Name == manager.Name);

            if (alreadyExists)
            {
                _logger?.Log(
                    $"Registration manager failed! Manager [{manager.Name}] already registered!",
                    LogType.Warning);

                throw new InvalidOperationException(
                    $"Registration manager failed! Manager [{manager.Name}] already registered!");
            }

            _managers.Add(manager);
            Managers = new ReadOnlyCollection<IManager>(_managers);
        }

        /// <inheritdoc/>
        public virtual void Connect()
        {
            try
            {
                _logger?.Log($"Connecting to [{_ip}:{_port}]...", LogType.Info);

                _socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                foreach (IManager manager in _managers)
                    manager.Start();

                _socket.BeginConnect(
                    new IPEndPoint(IPAddress.Parse(_ip), _port),
                    OnConnect,
                    null);
            }
            catch (Exception exception)
            {
                _logger?.Log($"Connect to [{_ip}:{_port}] failed! {exception.Message}", LogType.CriticalError);
            }
        }

        /// <inheritdoc/>
        public virtual void Disconnect()
        {
            try
            {
                foreach (IManager manager in _managers)
                    manager.Stop();

                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                    _isRunning = false;
                }

                OnDisconnected.Invoke();

                _logger?.Log($"Disconnected from [{_ip}:{_port}].", LogType.Info);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log($"Disconnect failed! {exception.Message}", LogType.Error);
            }
        }

        /// <inheritdoc/>
        public virtual void SendToServer(IServerCommand serverCommand)
        {
            if (_socket == null || !_isRunning)
                return;

            try
            {
                byte[] rawData = serverCommand.GetRawData();

                _socket.BeginSend(
                    rawData, 0, rawData.Length,
                    SocketFlags.None,
                    OnDataSent,
                    null);

                OnSendedData.Invoke(rawData);

                _logger?.Log(
                    $"Send data [{rawData.Length} bytes] to [{_ip}:{_port}]",
                    LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log($"Send to [{_ip}:{_port}] failed! {exception.Message}", LogType.Warning);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _socket?.Dispose();
                _socket = null;
            }
            catch { }
        }

        private void OnConnect(IAsyncResult asyncResult)
        {
            try
            {
                _socket!.EndConnect(asyncResult);
                _isRunning = true;

                OnConnected.Invoke();

                _logger?.Log($"Connected to [{_ip}:{_port}] success!", LogType.Info);

                _socket.BeginReceive(
                    Array.Empty<byte>(), 0, 0,
                    SocketFlags.None,
                    OnDataReceived,
                    null);
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
                _socket!.EndReceive(asyncResult);

                byte[] buffer = new byte[_bufferSize];
                int received = _socket.Receive(buffer, buffer.Length, SocketFlags.None);

                if (received == 0)
                {
                    Disconnect();

                    return;
                }

                if (received < buffer.Length)
                    Array.Resize(ref buffer, received);

                OnRecivedData.Invoke(buffer);

                _socket.BeginReceive(
                    Array.Empty<byte>(), 0, 0,
                    SocketFlags.None,
                    OnDataReceived,
                    null);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data receive from [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Error);

                Disconnect();
            }
        }

        private void OnDataSent(IAsyncResult asyncResult)
        {
            try
            {
                _socket!.EndSend(asyncResult);

                _logger?.Log($"Data sent to [{_ip}:{_port}] success!", LogType.Low);
            }
            catch (Exception exception)
            {
                if (!_hideSocketError)
                    _logger?.Log(
                        $"Data sent to [{_ip}:{_port}] failed! {exception.Message}",
                        LogType.Error);
            }
        }
    }
}