using ValidayClient.Managers.Interfaces;
using System;

namespace ValidayClient.Managers
{
    /// <summary>
    /// Default manager factory
    /// </summary>
    public class ManagerFactory : IManagerFactory
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IManager CreateManager<T>()
            where T : IManager
        {
            return Activator.CreateInstance<T>();
        }
    }
}
