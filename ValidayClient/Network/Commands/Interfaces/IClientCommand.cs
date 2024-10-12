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
        /// <param name="rawData">Raw data bytes</param>
        void Execute(byte[] rawData);
    }
}
