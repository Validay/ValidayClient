using System;
using System.Collections.Generic;
using ValidayClient.Managers.Interfaces;
using ValidayClient.Network.Commands.Interfaces;

namespace ValidayClient.Network.Interfaces
{
    /// <summary>
    /// Interface for a client that connects to a server.
    /// Note: the command map lives on CommandHandlerManager (via ICommandRegistry),
    /// not here — IClient should not be aware of command routing details.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Is the client currently connected
        /// </summary>
        bool IsRun { get; }

        /// <summary>
        /// Registered managers collection
        /// </summary>
        IReadOnlyCollection<IManager> Managers { get; }

        /// <summary>
        /// Fires when data is received from the server
        /// </summary>
        event Action<byte[]> OnReceivedData;

        /// <summary>
        /// Fires when data is sent to the server
        /// </summary>
        event Action<byte[]> OnSentData;

        /// <summary>
        /// Fires when connected to the server
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// Fires when disconnected from the server
        /// </summary>
        event Action OnDisconnected;

        /// <summary>
        /// Register a manager. Must be called before Connect().
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a manager with the same name is already registered,
        /// or if the client is already running.
        /// </exception>
        void RegisterManager(IManager manager);

        /// <summary>
        /// Connect to the server
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send a command to the server
        /// </summary>
        void SendToServer(IServerCommand serverCommand);
    }
}
