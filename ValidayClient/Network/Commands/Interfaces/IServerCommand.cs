namespace ValidayClient.Network.Commands.Interfaces
{
    /// <summary>
    /// Interface server command
    /// </summary>
    public interface IServerCommand
    {
        /// <summary>
        /// Id server command
        /// </summary>
        short Id { get; }

        /// <summary>
        /// Get raw data this server command
        /// </summary>
        /// <returns>bytes server command</returns>
        byte[] GetRawData();
    }
}
