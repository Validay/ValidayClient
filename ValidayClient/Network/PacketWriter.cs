using System;
using System.Collections.Generic;
using System.Text;

namespace ValidayClient.Network
{
    /// <summary>
    /// Fluent binary builder for outgoing packets.
    /// All multi-byte values are written in little-endian order.
    /// </summary>
    public sealed class PacketWriter
    {
        private readonly ushort _commandId;
        private readonly List<byte> _payload;

        /// <param name="commandId">Command ID prepended to every built packet.</param>
        public PacketWriter(ushort commandId)
        {
            _commandId = commandId;
            _payload = new List<byte>();
        }

        /// <summary>Writes one byte.</summary>
        public PacketWriter WriteByte(byte value)
        {
            _payload.Add(value);
            return this;
        }

        /// <summary>Writes a boolean as one byte (true = 1, false = 0).</summary>
        public PacketWriter WriteBool(bool value)
        {
            _payload.Add(value ? (byte)1 : (byte)0);
            return this;
        }

        /// <summary>Writes a 2-byte signed integer.</summary>
        public PacketWriter WriteShort(short value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 2-byte unsigned integer.</summary>
        public PacketWriter WriteUshort(ushort value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 4-byte signed integer.</summary>
        public PacketWriter WriteInt(int value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 4-byte unsigned integer.</summary>
        public PacketWriter WriteUint(uint value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes an 8-byte signed integer.</summary>
        public PacketWriter WriteLong(long value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 4-byte IEEE 754 float.</summary>
        public PacketWriter WriteFloat(float value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes an 8-byte IEEE 754 double.</summary>
        public PacketWriter WriteDouble(double value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>
        /// Writes a UTF-8 string prefixed by a 2-byte length header.
        /// Format: [ushort byteCount][UTF-8 bytes]
        /// A null value is treated as an empty string.
        /// </summary>
        public PacketWriter WriteString(string? value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            _payload.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _payload.AddRange(bytes);
            return this;
        }

        /// <summary>Writes raw bytes without a length prefix.</summary>
        public PacketWriter WriteBytes(byte[] value)
        {
            _payload.AddRange(value);
            return this;
        }

        /// <summary>
        /// Writes raw bytes, optionally preceded by a 2-byte length header.
        /// </summary>
        public PacketWriter WriteBytes(byte[] value, bool lengthPrefixed)
        {
            if (lengthPrefixed)
                _payload.AddRange(BitConverter.GetBytes((ushort)value.Length));

            _payload.AddRange(value);
            return this;
        }

        /// <summary>
        /// Builds the final packet without framing.
        /// Format: [commandId : 2 bytes][payload]
        /// </summary>
        public byte[] Build()
        {
            byte[] result = new byte[sizeof(ushort) + _payload.Count];
            BitConverter.GetBytes(_commandId).CopyTo(result, 0);
            _payload.CopyTo(result, sizeof(ushort));
            return result;
        }

        /// <summary>
        /// Builds the final packet with a 2-byte length prefix.
        /// Format: [bodyLength : 2 bytes][commandId : 2 bytes][payload]
        /// Use this when the server is configured with LengthPrefixFramer.
        /// </summary>
        public byte[] BuildFramed()
        {
            byte[] body = Build();
            byte[] framed = new byte[sizeof(ushort) + body.Length];
            BitConverter.GetBytes((ushort)body.Length).CopyTo(framed, 0);
            body.CopyTo(framed, sizeof(ushort));
            return framed;
        }
    }
}
