using System;
using System.Collections.Generic;

namespace ValidayClient.Managers.Interfaces
{
    /// <summary>
    /// Interface for accessing the registered command map.
    /// Allows other managers to inspect known commands
    /// without depending on the concrete CommandHandlerManager type.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// Read-only map of registered command IDs to their types
        /// </summary>
        IReadOnlyDictionary<ushort, Type> CommandsMap { get; }
    }
}