using System;
using System.Net;
using System.Net.Sockets;
using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;

namespace ValidayClient.Network
{
    /// <summary>
    /// Client configuration.
    /// Declared as a class (not struct) because it contains reference-type fields
    /// (ILogger, byte[]) — copying a struct would share those references silently.
    /// </summary>
    public class ClientSettings
    {
        /// <summary>
        /// IP address of the server to connect to
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Port of the server to connect to
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Receive buffer size in bytes
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Maximum read depth for a single packet (framing guard)
        /// </summary>
        public int MaxDepthReadPacket { get; set; }

        /// <summary>
        /// Byte sequence that marks the beginning of a new packet
        /// </summary>
        public byte[] MarkerStartPacket { get; set; }

        /// <summary>
        /// Logger used by the client
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Ready-to-use default settings (localhost:8888)
        /// </summary>
        public static ClientSettings Default => new ClientSettings(
            ip: "127.0.0.1",
            port: 8888,
            bufferSize: 1024,
            maxDepthReadPacket: 64,
            markerStartPacket: new byte[] { 1, 2, 3 },
            logger: new ConsoleLogger(LogType.Info));

        /// <summary>
        /// Creates and validates client settings.
        /// </summary>
        /// <exception cref="FormatException">Thrown when any parameter is out of range or invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public ClientSettings(
            string ip,
            int port,
            int bufferSize,
            int maxDepthReadPacket,
            byte[] markerStartPacket,
            ILogger logger)
        {
            if (!IsValidIpAddress(ip))
                throw new FormatException($"{nameof(ClientSettings)}: invalid IP address '{ip}'.");

            if (port < 0 || port > 65535)
                throw new FormatException($"{nameof(ClientSettings)}: port must be 0–65535, got {port}.");

            if (bufferSize < 0)
                throw new FormatException($"{nameof(ClientSettings)}: bufferSize must be >= 0.");

            if (maxDepthReadPacket < 0)
                throw new FormatException($"{nameof(ClientSettings)}: maxDepthReadPacket must be >= 0.");

            if (markerStartPacket == null || markerStartPacket.Length == 0)
                throw new FormatException($"{nameof(ClientSettings)}: markerStartPacket must not be empty.");

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            Ip = ip;
            Port = port;
            BufferSize = bufferSize;
            MaxDepthReadPacket = maxDepthReadPacket;
            MarkerStartPacket = markerStartPacket;
            Logger = logger;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress parsed))
                return false;

            return parsed.AddressFamily == AddressFamily.InterNetwork
                || parsed.AddressFamily == AddressFamily.InterNetworkV6;
        }
    }
}