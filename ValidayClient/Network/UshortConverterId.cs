using System;
using ValidayClient.Network.Interfaces;

namespace ValidayClient.Network
{
    /// <summary>
    /// Converter bytes to id ushort type
    /// </summary>
    public class UshortConverterId : IConverterId<ushort>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ushort Convert(byte[] bytes)
        {
            return BitConverter.ToUInt16(bytes, 0);
        }
    }
}