using ValidayClient.Network.Commands.Interfaces;

namespace ValidayClient.Network.Commands
{
    /// <summary>
    /// Base class for outgoing commands (client → server).
    /// Subclasses write the payload into a <see cref="PacketWriter"/>
    /// without worrying about command ID or byte layout.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MoveCommand : ServerCommandBase
    /// {
    ///     public float X { get; set; }
    ///     public float Y { get; set; }
    ///
    ///     public MoveCommand() : base(commandId: 2) { }
    ///
    ///     protected override void Write(PacketWriter writer)
    ///     {
    ///         writer.WriteFloat(X).WriteFloat(Y);
    ///     }
    /// }
    ///
    /// // Usage:
    /// client.SendToServer(new MoveCommand { X = 10f, Y = 20f });
    /// </code>
    /// </example>
    public abstract class ServerCommandBase : IServerCommand
    {
        private readonly ushort _commandId;

        /// <param name="commandId">Command ID sent as the first 2 bytes of every packet.</param>
        protected ServerCommandBase(ushort commandId)
        {
            _commandId = commandId;
        }

        /// <inheritdoc/>
        public byte[] GetRawData()
        {
            PacketWriter writer = new PacketWriter(_commandId);
            Write(writer);
            return writer.Build();
        }

        /// <summary>
        /// Writes the command payload into <paramref name="writer"/>.
        /// The command ID is prepended automatically — do not write it here.
        /// </summary>
        protected abstract void Write(PacketWriter writer);
    }
}
