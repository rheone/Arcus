using System.Net;
using Arcus.Utilities;
using static System.Net.Sockets.AddressFamily;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Arcus.Math
{
    /// <summary>
    ///     Static utility class containing mathematical methods on <see cref="IPAddress" /> objects
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         IPv4 address arithmetic operates within the 32-bit unsigned address space per
    ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
    ///         IPv6 address arithmetic operates within the 128-bit unsigned address space per
    ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see>.
    ///         Overflow or underflow beyond the respective address space boundaries throws
    ///         <see cref="InvalidOperationException"/>.
    ///     </para>
    /// </remarks>
    public static class IPAddressMath
    {
        #region basic arithmetic operations

        /// <summary>
        ///     Increment IPv4 or IPv6 value
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For IPv4, overflow beyond <c>255.255.255.255</c> (upper bound per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>) throws
        ///         <see cref="InvalidOperationException"/>.
        ///         For IPv6, overflow beyond <c>ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff</c> (upper bound per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see>) throws
        ///         <see cref="InvalidOperationException"/>.
        ///     </para>
        /// </remarks>
        /// <param name="input">the ip address to affect</param>
        /// <param name="delta">the increment value, may be negative</param>
        /// <returns>the incremented ip address</returns>
        /// <exception cref="InvalidOperationException">could not increment input</exception>
        /// <exception cref="InvalidOperationException">Increment caused address underflow</exception>
        /// <exception cref="InvalidOperationException">Increment caused address overflow</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
        public static IPAddress Increment(this IPAddress input, long delta = 1)
        {
            #region defense

            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(input.AddressFamily))
            {
                throw new ArgumentException(
                    $"must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(input)
                );
            }

            #endregion // end: defense

            if (delta == 0)
            {
                return input;
            }

            var wrapper = BigEndianBitWrapper.FromBytes(input.GetAddressBytes());
            if (!wrapper.TryAdd(delta, out var result))
            {
                throw new InvalidOperationException(
                    delta > 0 ? "increment would overflow maximum size of ip address" : "could not increment address"
                );
            }

            return new IPAddress(result.ToBytes());
        }

        /// <summary>
        ///     Try to increment the given address
        /// </summary>
        /// <param name="input">the <see cref="IPAddress"/> to increment</param>
        /// <param name="address">the resulting <see cref="IPAddress"/> post increment</param>
        /// <param name="delta">the amount to increment by</param>
        /// <returns>true on success</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryIncrement(IPAddress input, [NotNullWhen(true)] out IPAddress address, long delta = 1)
#else
        public static bool TryIncrement(IPAddress input, out IPAddress address, long delta = 1)
#endif
        {
            if (input == null)
            {
                address = null;
                return false;
            }

            try
            {
                address = input.Increment(delta);
                return true;
            }
            catch
            {
                address = null;
                return false;
            }
        }

        #endregion

        #region comparisons

        /// <summary>
        ///     Is Equal
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>true if <paramref name="left"/> is logically equal to <paramref name="left"/></returns>
        public static bool IsEqualTo(this IPAddress left, IPAddress right)
        {
            return ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && left.Equals(right));
        }

        /// <summary>
        ///     Greater Than
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>true if <paramref name="left"/> is logically greater than <paramref name="left"/></returns>
        public static bool IsGreaterThan(this IPAddress left, IPAddress right)
        {
            if (
                ReferenceEquals(left, right)
                || (!ReferenceEquals(left, null) && left.Equals(right))
                || left == null
                || right == null
                || left.AddressFamily != right.AddressFamily
            )
            {
                return false;
            }

            return BigEndianBitWrapper
                    .FromBytes(left.GetAddressBytes())
                    .CompareTo(BigEndianBitWrapper.FromBytes(right.GetAddressBytes())) > 0;
        }

        /// <summary>
        ///     Greater Than or equal
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>true if <paramref name="left"/> is logically greater than or equal to <paramref name="left"/></returns>
        public static bool IsGreaterThanOrEqualTo(this IPAddress left, IPAddress right)
        {
            return ReferenceEquals(left, right)
                || (!ReferenceEquals(left, null) && left.Equals(right))
                || (
                    left?.AddressFamily == right?.AddressFamily
                    && BigEndianBitWrapper
                        .FromBytes(left.GetAddressBytes())
                        .CompareTo(BigEndianBitWrapper.FromBytes(right.GetAddressBytes())) >= 0
                );
        }

        /// <summary>
        ///     Less Than
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>true if <paramref name="left"/> is logically less than <paramref name="left"/></returns>
        /// <exception cref="InvalidOperationException">Address families must be InterNetwork or InternetworkV6</exception>
        public static bool IsLessThan(this IPAddress left, IPAddress right)
        {
            if (ReferenceEquals(left, right) || left == null || right == null || left.AddressFamily != right.AddressFamily)
            {
                return false;
            }

            return BigEndianBitWrapper
                    .FromBytes(left.GetAddressBytes())
                    .CompareTo(BigEndianBitWrapper.FromBytes(right.GetAddressBytes())) < 0;
        }

        /// <summary>
        ///     Less Than or equal
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>true if <paramref name="left"/> is logically less than or equal to <paramref name="left"/></returns>
        public static bool IsLessThanOrEqualTo(this IPAddress left, IPAddress right)
        {
            return ReferenceEquals(left, right)
                || (!ReferenceEquals(left, null) && left.Equals(right))
                || (
                    left?.AddressFamily == right?.AddressFamily
                    && BigEndianBitWrapper
                        .FromBytes(left.GetAddressBytes())
                        .CompareTo(BigEndianBitWrapper.FromBytes(right.GetAddressBytes())) <= 0
                );
        }

        /// <summary>
        ///     Determine if the <paramref name="input"/> occurs numerically between the given high and low IP addresses
        ///     Inclusivity contingent on inclusive bit
        /// </summary>
        /// <param name="input">IP address to test</param>
        /// <param name="low">low value</param>
        /// <param name="high">high value</param>
        /// <param name="inclusive">true if bounds are inclusive (defaults to true)</param>
        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="low" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="high" /> is <see langword="null" />.</exception>
        /// <returns>true if <paramref name="input"/> is between <paramref name="low"/> and <paramref name="high"/></returns>
        public static bool IsBetween(this IPAddress input, IPAddress low, IPAddress high, bool inclusive = true)
        {
            #region defense

            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (low is null)
            {
                throw new ArgumentNullException(nameof(low));
            }

            if (high is null)
            {
                throw new ArgumentNullException(nameof(high));
            }

            if (low.AddressFamily != high.AddressFamily || input.AddressFamily != low.AddressFamily)
            {
                throw new InvalidOperationException("address families do not match");
            }

            var lowAddressBytes = low.GetAddressBytes();
            var highAddressBytes = high.GetAddressBytes();
            var lowWrap = BigEndianBitWrapper.FromBytes(lowAddressBytes);
            var highWrap = BigEndianBitWrapper.FromBytes(highAddressBytes);
            if (lowWrap.CompareTo(highWrap) > 0)
            {
                throw new InvalidOperationException($"{nameof(low)} must not be greater than {nameof(high)}");
            }

            #endregion // end: defense

            if (ReferenceEquals(input, low) || input.Equals(low) || ReferenceEquals(input, high) || input.Equals(high))
            {
                return inclusive;
            }

            var inputWrap = BigEndianBitWrapper.FromBytes(input.GetAddressBytes());
            return inputWrap.CompareTo(lowWrap) > 0 && inputWrap.CompareTo(highWrap) < 0;
        }

        /// <summary>
        ///     Get Maximum <see cref="IPAddress" /> (based on bytes)
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>the largest of the two operands</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Address families must match</exception>
        public static IPAddress Max(IPAddress left, IPAddress right)
        {
            #region defense

            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (right.AddressFamily != left.AddressFamily)
            {
                throw new InvalidOperationException("Address families must match");
            }

            #endregion // end: defense

            return left.IsGreaterThan(right) ? left : right;
        }

        /// <summary>
        ///     Get Minimum IPAddress (based on bytes)
        /// </summary>
        /// <param name="left">first operand</param>
        /// <param name="right">second operand</param>
        /// <returns>the smallest of the two operands</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Address families must match</exception>
        public static IPAddress Min(IPAddress left, IPAddress right)
        {
            #region defense

            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (right.AddressFamily != left.AddressFamily)
            {
                throw new InvalidOperationException("Address families must match");
            }

            #endregion // end: defense

            return left.IsLessThan(right) ? left : right;
        }

        #endregion

        #region limit deduction

        /// <summary>
        ///     determine if IP address is at maximum value
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         IPv4 maximum is <c>255.255.255.255</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
        ///         IPv6 maximum is <c>ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see>.
        ///     </para>
        /// </remarks>
        /// <param name="address">the IP Address to test</param>
        /// <returns>true if the address is the maximum value</returns>
        /// <exception cref="InvalidOperationException">Address families must be InterNetwork or InternetworkV6</exception>
        /// <exception cref="ArgumentNullException"><paramref name="address" /> is <see langword="null" />.</exception>
        public static bool IsAtMax(this IPAddress address)
        {
            #region defense

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            #endregion // end: defense

            switch (address.AddressFamily)
            {
                case InterNetwork:
                    return address.Equals(IPAddressUtilities.IPv4MaxAddress);
                case InterNetworkV6:
                    return address.Equals(IPAddressUtilities.IPv6MaxAddress);
                default:
                    throw new ArgumentOutOfRangeException(nameof(address), address.AddressFamily, "unexpected address family");
            }
        }

        /// <summary>
        ///     determine if IP address is at minimum value
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         IPv4 minimum is <c>0.0.0.0</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
        ///         IPv6 minimum is <c>::</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see>.
        ///     </para>
        /// </remarks>
        /// <param name="address">the IP Address to test</param>
        /// <returns>true if the address is the minimum value</returns>
        /// <exception cref="InvalidOperationException">Address families must be InterNetwork or InternetworkV6</exception>
        /// <exception cref="ArgumentNullException"><paramref name="address" /> is <see langword="null" />.</exception>
        public static bool IsAtMin(this IPAddress address)
        {
            #region defense

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            #endregion // end: defense

            switch (address.AddressFamily)
            {
                case InterNetwork:
                    return address.Equals(IPAddressUtilities.IPv4MinAddress);
                case InterNetworkV6:
                    return address.Equals(IPAddressUtilities.IPv6MinAddress);
                default:
                    throw new ArgumentOutOfRangeException(nameof(address), address.AddressFamily, "unexpected address family");
            }
        }

        #endregion
    }
}
