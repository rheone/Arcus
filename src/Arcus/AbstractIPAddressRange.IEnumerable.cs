using System.Collections;
using System.Net;
using System.Numerics;
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
        public IEnumerable<IPAddress> ToIPAddresses()
        {
            return EnumerateCore(BigInteger.One << this.MaxEnumerationExponent);
        }

        /// <inheritdoc />
#pragma warning disable S1133 // Do not forget to remove this deprecated code someday
        [Obsolete("Use ToIPAddresses() instead")]
#pragma warning restore S1133
        public IEnumerator<IPAddress> GetEnumerator()
        {
            return ToIPAddresses().GetEnumerator();
        }

        /// <inheritdoc />
#pragma warning disable CS0618 // Type or member is obsolete
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
#pragma warning restore CS0618

        private IEnumerable<IPAddress> EnumerateCore(BigInteger maxCount)
        {
            var limitWrap = BigEndianBitWrapper.FromBytes(
                IPAddressMath
                    .Min(this.Tail, this.IsIPv4 ? IPAddressUtilities.IPv4MaxAddress : IPAddressUtilities.IPv6MaxAddress)
                    .GetAddressBytes()
            );

            var current = BigEndianBitWrapper.FromBytes(this.Head.GetAddressBytes());

#if NET8_0_OR_GREATER
            var buffer = new byte[current.ByteWidth];
#endif

            BigInteger count = 0;
            while (current.CompareTo(limitWrap) <= 0)
            {
                if (count >= maxCount)
                {
                    throw new InvalidOperationException(
                        $"Enumeration limit of {maxCount} addresses (2^{this.MaxEnumerationExponent}) reached. "
                            + $"The range contains {this.Length} addresses, which exceeds this limit. "
                            + "Construct with a larger maxEnumerationExponent (0–128) to enumerate more."
                    );
                }

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
                count++;
            }
        }

        #endregion // end: IEnumerable
    }
}
