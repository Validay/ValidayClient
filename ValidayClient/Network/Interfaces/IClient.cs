using System;
using System.Collections.Generic;
using ValidayClient.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;

namespace ValidayClient.Network.Interfaces
{
    /// <summary>
    /// Interface for client
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Is the client running
        /// </summary>
        bool IsRun { get; }

        /// <summary>
        /// Managers colection 
        /// </summary>
        IReadOnlyCollection<IManager> Managers { get; }

        /// <summary>
        /// Get all client commands
        /// </summary>
        IReadOnlyDictionary<short, Type> ClientCommandsMap { get; }

        /// <summary>
        /// Event for recived data from server
        /// </summary>
        event Action<byte[]> OnRecivedData;

        /// <summary>
        /// Event for sended data to server
        /// </summary>
        event Action<byte[]> OnSendedData;

        /// <summary>
        /// Event for connect to server
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// Event for disconnect from server
        /// </summary>
        event Action OnDisconnected;

        /// <summary>
        /// Registration new manager
        /// </summary>
        /// <typeparam name="T">Manager type</typeparam>
        void RegistrationManager<T>()
            where T : IManager;

        /// <summary>
        /// Connect to server
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send data to server
        /// </summary>
        /// <param name="rawData">Raw data</param>
        void SendToServer(byte[] rawData);
    }
}
