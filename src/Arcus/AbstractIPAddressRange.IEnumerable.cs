using System.Collections;
using System.Collections.Generic;
using System.Net;
using Arcus.Math;
using Arcus.Utilities;

namespace Arcus
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> implementation of <see cref="IEnumerable{IPAddress}"/>
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

#if NET8_0_OR_GREATER
            // Reuse a pre-allocated buffer to avoid one allocation per yielded address.
            // On NET8+ ToBytes(Span<byte>) writes in-place; IPAddress(ReadOnlySpan<byte>)
            // copies internally so one allocation per iteration is unavoidable.
            var buffer = new byte[current.ByteWidth];
#endif

            while (current.CompareTo(limitWrap) <= 0)
            {
#if NET8_0_OR_GREATER
                current.ToBytes(buffer);
                yield return new IPAddress(buffer);
#else
                yield return new IPAddress(current.ToBytes());
#endif

                if (!current.TryAdd(1, out var next))
                {
                    break;
                }

                current = next;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion // end: IEnumerable
    }
}
