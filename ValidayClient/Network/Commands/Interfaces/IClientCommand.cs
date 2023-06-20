using System.Collections.Generic;
using ValidayClient.Managers.Interfaces;

namespace ValidayClient.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface client command
    /// </summary>
    public interface IClientCommand
    {
        /// <summary>
        /// Executed this client command
        /// </summary>
        /// <param name="managers">Collection manager</param>
        /// <param name="rawData">Raw data bytes</param>
        void Execute(
            IReadOnlyCollection<IManager> managers,
            byte[] rawData);
    }
}
