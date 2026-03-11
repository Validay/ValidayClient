using System;
using System.Text;

namespace ValidayClient.Network
{
    /// <summary>
    /// Sequential binary reader for incoming packets.
    /// All multi-byte values are read in little-endian order.
    /// </summary>
    public sealed class PacketReader
    {
        /// <summary>Current read position within the buffer.</summary>
        public int Position => _position;

        /// <summary>Number of bytes not yet read.</summary>
        public int Remaining => _data.Length - _position;

        private readonly byte[] _data;
        private int _position;

        /// <summary>Creates a reader starting at position 0.</summary>
        public PacketReader(byte[] data)
        {
            _data = data;
            _position = 0;
        }

        /// <summary>Creates a reader starting at a given offset.</summary>
        public PacketReader(byte[] data, int offset)
        {
            _data = data;
            _position = offset;
        }

        /// <summary>
        /// Advances past the 2-byte command ID at the start of the packet.
        /// Returns <c>this</c> for fluent chaining.
        /// </summary>
        public PacketReader SkipCommandId()
        {
            _position += sizeof(ushort);
            return this;
        }

        /// <summary>Advances the position by <paramref name="count"/> bytes.</summary>
        public PacketReader Skip(int count)
        {
            _position += count;
            return this;
        }

        /// <summary>Reads one byte.</summary>
        public byte ReadByte() => _data[_position++];

        /// <summary>Reads one byte as a boolean (0 = false, non-zero = true).</summary>
        public bool ReadBool() => _data[_position++] != 0;

        /// <summary>Reads a 2-byte signed integer.</summary>
        public short ReadShort()
        {
            short value = BitConverter.ToInt16(_data, _position);
            _position += sizeof(short);
            return value;
        }

        /// <summary>Reads a 2-byte unsigned integer.</summary>
        public ushort ReadUshort()
        {
            ushort value = BitConverter.ToUInt16(_data, _position);
            _position += sizeof(ushort);
            return value;
        }

        /// <summary>Reads a 4-byte signed integer.</summary>
        public int ReadInt()
        {
            int value = BitConverter.ToInt32(_data, _position);
            _position += sizeof(int);
            return value;
        }

        /// <summary>Reads a 4-byte unsigned integer.</summary>
        public uint ReadUint()
        {
            uint value = BitConverter.ToUInt32(_data, _position);
            _position += sizeof(uint);
            return value;
        }

        /// <summary>Reads an 8-byte signed integer.</summary>
        public long ReadLong()
        {
            long value = BitConverter.ToInt64(_data, _position);
            _position += sizeof(long);
            return value;
        }

        /// <summary>Reads a 4-byte IEEE 754 float.</summary>
        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(_data, _position);
            _position += sizeof(float);
            return value;
        }

        /// <summary>Reads an 8-byte IEEE 754 double.</summary>
        public double ReadDouble()
        {
            double value = BitConverter.ToDouble(_data, _position);
            _position += sizeof(double);
            return value;
        }

        /// <summary>
        /// Reads a UTF-8 string prefixed by a 2-byte length header.
        /// Format: [ushort byteCount][UTF-8 bytes]
        /// </summary>
        public string ReadString()
        {
            ushort length = ReadUshort();
            string value = Encoding.UTF8.GetString(_data, _position, length);
            _position += length;
            return value;
        }

        /// <summary>Reads exactly <paramref name="count"/> raw bytes.</summary>
        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            Array.Copy(_data, _position, result, 0, count);
            _position += count;
            return result;
        }

        /// <summary>
        /// Reads a byte array prefixed by a 2-byte length header.
        /// Format: [ushort byteCount][bytes]
        /// </summary>
        public byte[] ReadBytesWithLength()
        {
            ushort length = ReadUshort();
            return ReadBytes(length);
        }
    }
}
