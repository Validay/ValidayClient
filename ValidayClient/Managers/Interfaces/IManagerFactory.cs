namespace ValidayClient.Managers.Interfaces
{
    /// <summary>
    /// Main interface for client factory
    /// </summary>
    public interface IManagerFactory
    {
        /// <summary>
        /// Create new object manager
        /// </summary>
        /// <typeparam name="T">Type manager</typeparam>
        /// <returns>Client manager object type of T</returns>
        IManager CreateManager<T>()
            where T : IManager;
    }
}
