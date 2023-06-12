using System;
using System.Net;
using System.Net.Sockets;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers;
using ValidayClient.Managers.Interfaces;

namespace ValidayClient.Network
{
    /// <summary>
    /// Client settings for connect server
    /// </summary>
    public struct ClientSettings
    {
        /// <summary>
        /// Ip address server for connecting
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Port server for connecting
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Buffer size
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Factory for creating managers
        /// </summary>
        public IManagerFactory ManagerFactory { get; set; }

        /// <summary>
        /// Logger for client
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Default client settings
        /// </summary>
        public static ClientSettings Default => new ClientSettings
        {
            Ip = "127.0.0.1",
            BufferSize = 1024,
            Port = 8888,
            ManagerFactory = new ManagerFactory(),
            Logger = new ConsoleLogger(LogType.Info)
        };

        /// <summary>
        /// Default constructor client settings
        /// </summary>
        /// <param name="ip">Ip address server for connecting</param>
        /// <param name="port">Port server for connecting</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <param name="managerFactory">Factory for creating managers</param>
        /// <param name="logger">Logger for client</param>
        /// <exception cref="FormatException">Invalid parameters</exception>
        public ClientSettings(
            string ip,
            int port,
            int bufferSize,
            IManagerFactory managerFactory,
            ILogger logger)
        {
            if (bufferSize < 0
                || managerFactory == null
                || logger == null
                || port < 0
                || port > 65535
                || !IsValidIpAddress(ip))
                throw new FormatException($"{nameof(ClientSettings)} create failed! Invalid parameters");

            Ip = ip;
            Port = port;
            BufferSize = bufferSize;
            ManagerFactory = managerFactory;
            Logger = logger;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress parsedIpAddress))
            {
                if (parsedIpAddress.AddressFamily == AddressFamily.InterNetwork
                    || parsedIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    return true;
            }

            return false;
        }
    }
}