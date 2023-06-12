﻿using ValidayClient.Logging.Interfaces;
using ValidayClient.Network.Interfaces;

namespace ValidayClient.Managers.Interfaces
{
    /// <summary>
    /// Main interface for manager
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Name manager
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get active this manager
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Initialize this manager
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="logger">Logger</param>
        void Initialize(
            IClient client,
            ILogger logger);

        /// <summary>
        /// Starting this manager
        /// </summary>
        void Start();

        /// <summary>
        /// Stopping this manager
        /// </summary>
        void Stop();
    }
}