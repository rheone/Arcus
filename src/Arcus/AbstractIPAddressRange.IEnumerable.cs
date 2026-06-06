using System.Collections;
using System.Collections.Generic;
using System.Net;
using Arcus.Math;
using Arcus.Utilities;

namespace Arcus
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> implementation of <see cref="IEnumerable{T}"/>
    /// </content>
    public abstract partial class AbstractIPAddressRange
    {
        #region IEnumerable / IEnumerable<IPAddress>

        /// <inheritdoc />
        public IEnumerator<IPAddress> GetEnumerator()
        {
            var limitWrap = BigEndianBitWrapper.FromBytes(
                IPAddressMath
                    .Min(this.Tail, this.IsIPv4 ? IPAddressUtilities.IPv4MaxAddress : IPAddressUtilities.IPv6MaxAddress)
                    .GetAddressBytes()
            );

            var current = BigEndianBitWrapper.FromBytes(this.Head.GetAddressBytes());

            while (current.CompareTo(limitWrap) <= 0)
            {
                yield return new IPAddress(current.ToBytes());

                if (!current.TryAdd(1, out var next))
                {
                    break;
                }

                current = next;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion // end: IEnumerable
    }
}
