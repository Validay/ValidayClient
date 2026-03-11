using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ValidayClient.Network.Commands.Interfaces;

namespace ValidayClient.Network.Commands
{
    /// <summary>
    /// Thread-safe object pool for client commands.
    /// Commands are bucketed by their concrete type and reused across calls.
    /// </summary>
    /// <typeparam name="TId">Command ID type</typeparam>
    /// <typeparam name="TCommand">Command interface type</typeparam>
    public class CommandPool<TId, TCommand> : ICommandPool<TId, TCommand>
        where TCommand : class
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<TCommand>> _pool;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommandPool()
        {
            _pool = new ConcurrentDictionary<Type, ConcurrentBag<TCommand>>();
        }

        /// <inheritdoc/>
        /// <exception cref="KeyNotFoundException">Thrown when id is not present in commandsMap.</exception>
        public TCommand GetCommand(
            TId id,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.ContainsKey(id))
                throw new KeyNotFoundException($"Command with ID {id} not found in commands map.");

            Type commandType = commandsMap[id];
            ConcurrentBag<TCommand> bag = _pool.GetOrAdd(commandType, _ => new ConcurrentBag<TCommand>());

            return bag.TryTake(out TCommand? command)
                ? command
                : (TCommand)Activator.CreateInstance(commandType)!;
        }

        /// <inheritdoc/>
        /// <exception cref="KeyNotFoundException">Thrown when id is not present in commandsMap.</exception>
        public void ReturnCommandToPool(
            TId id,
            TCommand command,
            IDictionary<TId, Type> commandsMap)
        {
            if (!commandsMap.ContainsKey(id))
                throw new KeyNotFoundException($"Command with ID {id} not found in commands map.");

            Type commandType = commandsMap[id];
            _pool.GetOrAdd(commandType, _ => new ConcurrentBag<TCommand>()).Add(command);
        }
    }
}
