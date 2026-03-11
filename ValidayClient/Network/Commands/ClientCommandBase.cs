using ValidayClient.Network.Commands.Interfaces;

namespace ValidayClient.Network.Commands
{
    /// <summary>
    /// Typed base class for incoming commands (server → client).
    /// Subclasses define how to deserialize the packet into a payload object
    /// and how to handle it — mirroring ServerCommandBase&lt;TPayload&gt; on the server.
    /// </summary>
    /// <typeparam name="TPayload">Type that carries the deserialized packet data.</typeparam>
    /// <example>
    /// <code>
    /// public record ChatPayload(string Message);
    ///
    /// public class ChatCommand : ClientCommandBase&lt;ChatPayload&gt;
    /// {
    ///     protected override ChatPayload Read(PacketReader reader)
    ///         => new ChatPayload(reader.ReadString());
    ///
    ///     protected override void Handle(ChatPayload payload)
    ///         => Console.WriteLine(payload.Message);
    /// }
    /// </code>
    /// </example>
    public abstract class ClientCommandBase<TPayload> : IClientCommand
    {
        /// <inheritdoc/>
        public void Execute(byte[] rawData)
        {
            PacketReader reader = new PacketReader(rawData).SkipCommandId();
            TPayload payload = Read(reader);
            Handle(payload);
        }

        /// <summary>
        /// Deserializes the packet payload from <paramref name="reader"/>.
        /// The command ID has already been skipped when this is called.
        /// </summary>
        protected abstract TPayload Read(PacketReader reader);

        /// <summary>Processes the deserialized <paramref name="payload"/>.</summary>
        protected abstract void Handle(TPayload payload);
    }
}
