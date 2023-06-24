using System;
using System.Net;
using System.Net.Sockets;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;

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
        /// Maximum depth for reading packet in client network stream
        /// </summary>
        public int MaxDepthReadPacket { get; set; }

        /// <summary>
        /// Marker for detect start new packet
        /// </summary>
        public byte[] MarkerStartPacket { get; set; }

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
            MaxDepthReadPacket = 64,
            MarkerStartPacket = new byte[]
            {
                1,
                2,
                3
            },
            Logger = new ConsoleLogger(LogType.Info)
        };

        /// <summary>
        /// Default constructor client settings
        /// </summary>
        /// <param name="ip">Ip address server for connecting</param>
        /// <param name="port">Port server for connecting</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <param name="maxDepthReadPacket">Maximum depth for reading packet in client network stream</param>
        /// <param name="markerStartPacket">Marker for detect start new packet</param>
        /// <param name="logger">Logger for client</param>
        /// <exception cref="FormatException">Invalid parameters</exception>
        public ClientSettings(
            string ip,
            int port,
            int bufferSize,
            int maxDepthReadPacket,
            byte[] markerStartPacket,
            ILogger logger)
        {
            if (bufferSize < 0
                || logger == null
                || port < 0
                || port > 65535
                || maxDepthReadPacket < 0
                || markerStartPacket.Length == 0
                || !IsValidIpAddress(ip))
                throw new FormatException($"{nameof(ClientSettings)} create failed! Invalid parameters");

            Ip = ip;
            Port = port;
            BufferSize = bufferSize;
            MaxDepthReadPacket = maxDepthReadPacket;
            MarkerStartPacket = markerStartPacket;
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