using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Arcus.Utilities;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Arcus.Converters
{
    /// <summary>
    ///     Static utility class containing conversion methods for converting <see cref="IPAddress" /> objects into something
    ///     else
    /// </summary>
    public static class IPAddressConverters
    {
        #region integral conversion

        /// <summary>
        ///     Convert a valid netmask (encoded as <see cref="IPAddress" />) into a CIDR route prefix
        ///     Only valid for IPv4 netmasks
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The netmask must be a contiguous sequence of leading 1-bits per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
        ///         The resulting integer is the CIDR prefix length per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        /// <param name="netmask">the netmask to convert</param>
        /// <returns>the route prefix</returns>
        /// <exception cref="InvalidOperationException"><paramref name="netmask" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">not a valid netmask</exception>
        public static int NetmaskToCidrRoutePrefix(this IPAddress netmask)
        {
            #region defense

            if (netmask == null)
            {
                throw new ArgumentNullException(nameof(netmask));
            }

            if (!netmask.IsValidNetMask())
            {
                throw new InvalidOperationException("not a valid netmask");
            }

            #endregion // end: defense

            var netmaskBytes = netmask.GetAddressBytes();
            var routingPrefix = IPAddressUtilities.IPv4BitCount;

            for (var i = 0; i < IPAddressUtilities.IPv4BitCount; i++)
            {
                var bitMask = (byte)(0x80 >> (i % 8)); // bit mask built from current bit position in byte

                if ((netmaskBytes[i / 8] & bitMask) != 0) // if set bits are common
                {
                    continue;
                }

                routingPrefix = i;
                break;
            }

            return routingPrefix;
        }

        #endregion

        #region string conversion

        /// <summary>
        ///     IPv6 to Base85 (will return empty string for non ipv6 addresses) AKA Ascii85
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Encoding defined in <see href="https://www.rfc-editor.org/rfc/rfc1924">RFC 1924</see>
        ///         (an April Fools Day joke, but implemented here regardless).
        ///     </para>
        /// </remarks>
        /// <param name="ipAddress">the ip address to convert</param>
        /// <returns>Ascii85/Base85 representation of IPv6 Address, or an empty string on failure</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: MaybeNull]
#endif
        public static string ToBase85String(this IPAddress ipAddress)
        {
            if (ipAddress?.IsIPv6() != true)
            {
                return null;
            }

            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~";
            var chars = new char[20];
            var addressBytes = ipAddress.GetAddressBytes();

#if NET8_0_OR_GREATER
            ulong high = 0;
            ulong low = 0;
            for (var i = 0; i < 8; i++)
            {
                high = (high << 8) | addressBytes[i];
            }

            for (var i = 8; i < 16; i++)
            {
                low = (low << 8) | addressBytes[i];
            }

            var value128 = new UInt128(high, low);
            for (var i = 19; i >= 0; i--)
            {
                (value128, var remainder) = UInt128.DivRem(value128, 85);
                chars[i] = alphabet[(int)remainder];
            }
#else
            var leBytes = new byte[17]; // zero-initialized; index 16 stays 0 (sign byte)
            for (var i = 0; i < 16; i++)
            {
                leBytes[i] = addressBytes[15 - i];
            }

            var value = new BigInteger(leBytes);
            for (var i = 19; i >= 0; i--)
            {
                value = BigInteger.DivRem(value, 85, out var remainder);
                chars[i] = alphabet[(int)remainder];
            }
#endif

            return new string(chars);
        }

        /// <summary>
        ///     Represent address as dotted quad (if not ipv6 simply return stringified version of input)
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         IPv4 dotted-quad notation (four decimal octets separated by dots) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
        ///     </para>
        ///     <para>
        ///         For IPv6 addresses the first 96 bits (6 hextets) are rendered in compressed IPv6 notation —
        ///         the longest consecutive run of zero-valued hextets is collapsed to <c>::</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc5952#section-4">RFC 5952 §4</see> — followed
        ///         by the trailing 32 bits as an IPv4 dotted-quad suffix.
        ///     </para>
        /// </remarks>
        /// <param name="ipAddress">the ip address to convert</param>
        /// <returns>dotted quad version of the given address</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ipAddress" /> is <see langword="null" />.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(ipAddress))]
#endif
        public static string ToDottedQuadString(this IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                return null;
            }

            if (!ipAddress.IsIPv6())
            {
                return ipAddress.ToString();
            }

#if NET8_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[16];
            ipAddress.TryWriteBytes(bytes, out _);
            Span<ushort> hextets = stackalloc ushort[6];
#else
            var bytes = ipAddress.GetAddressBytes();
            var hextets = new ushort[6];
#endif

            for (var i = 0; i < 6; i++)
            {
                hextets[i] = (ushort)((bytes[i * 2] << 8) | bytes[(i * 2) + 1]);
            }

            // Find the longest consecutive run of zero hextets for :: compression
            var bestStart = -1;
            var bestLen = 0;
            var runStart = -1;
            var runLen = 0;

            for (var i = 0; i < 6; i++)
            {
                if (hextets[i] == 0)
                {
                    if (runStart < 0)
                    {
                        runStart = i;
                    }

                    runLen++;
                    if (runLen > bestLen)
                    {
                        bestStart = runStart;
                        bestLen = runLen;
                    }
                }
                else
                {
                    runStart = -1;
                    runLen = 0;
                }
            }

            var sb = new System.Text.StringBuilder(45);

#if NET8_0_OR_GREATER
            Span<char> hexBuf = stackalloc char[4];
#endif

            if (bestLen >= 1)
            {
                for (var i = 0; i < bestStart; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(':');
                    }

#if NET8_0_OR_GREATER
                    hextets[i].TryFormat(hexBuf, out var hexLen, "x");
                    sb.Append(hexBuf[..hexLen]);
#else
                    sb.Append(hextets[i].ToString("x"));
#endif
                }

                sb.Append("::");

                var afterStart = bestStart + bestLen;
                for (var i = afterStart; i < 6; i++)
                {
                    if (i > afterStart)
                    {
                        sb.Append(':');
                    }

#if NET8_0_OR_GREATER
                    hextets[i].TryFormat(hexBuf, out var hexLen, "x");
                    sb.Append(hexBuf[..hexLen]);
#else
                    sb.Append(hextets[i].ToString("x"));
#endif
                }

                if (afterStart < 6)
                {
                    sb.Append(':');
                }
            }
            else
            {
                for (var i = 0; i < 6; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(':');
                    }

#if NET8_0_OR_GREATER
                    hextets[i].TryFormat(hexBuf, out var hexLen, "x");
                    sb.Append(hexBuf[..hexLen]);
#else
                    sb.Append(hextets[i].ToString("x"));
#endif
                }

                sb.Append(':');
            }

            sb.Append(bytes[12]);
            sb.Append('.');
            sb.Append(bytes[13]);
            sb.Append('.');
            sb.Append(bytes[14]);
            sb.Append('.');
            sb.Append(bytes[15]);

            return sb.ToString();
        }

        /// <summary>
        ///     Short hex value
        /// </summary>
        /// <param name="ipAddress">the ip address to convert</param>
        /// <returns>Hex version of the given IP Address</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ipAddress" /> is <see langword="null" />.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(ipAddress))]
#endif
        public static string ToHexString(this IPAddress ipAddress)
        {
            return ipAddress == null ? null : BigEndianBitWrapper.FromBytes(ipAddress.GetAddressBytes()).ToHexString();
        }

        /// <summary>
        ///     Convert an <see cref="IPAddress" /> to a numeric representation
        /// </summary>
        /// <param name="ipAddress">The ip address to convert</param>
        /// <returns>an integral representation of the IP address</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ipAddress" /> is <see langword="null" />.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(ipAddress))]
#endif
        public static string ToNumericString(this IPAddress ipAddress)
        {
            return ipAddress == null ? null : BigEndianBitWrapper.FromBytes(ipAddress.GetAddressBytes()).ToDecimalString();
        }

        /// <summary>
        ///     Convert to uncompressed IPv4/IPv6, adding zeros or expanding '::' where appropriate
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For IPv6, produces the fully-expanded form (no <c>::</c> compression, all hextets zero-padded to 4 digits),
        ///         which is the inverse of the compressed form defined in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc5952#section-4">RFC 5952 §4</see>.
        ///         For IPv4, produces three-digit zero-padded dotted-quad per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
        ///     </para>
        /// </remarks>
        /// <param name="ipAddress">the address to expand</param>
        /// <returns>the expanded for of IPv4/IPv6, or ToString() otherwise</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ipAddress" /> is <see langword="null" />.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(ipAddress))]
#endif
        public static string ToUncompressedString(this IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                return null;
            }

            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return IPv4ToString();
                case AddressFamily.InterNetworkV6:
                    return IPv6ToString();
                default:
                    return ipAddress.ToString(); // all else treat as to string
            }

            string IPv4ToString()
            {
                var octets = ipAddress.GetAddressBytes().Select(b => $"{b:D3}");

                return string.Join(".", octets); // join padded strings with '.' character
            }

            string IPv6ToString()
            {
                var addressBytes = ipAddress.GetAddressBytes();

                var hextets = Enumerable
                    .Range(0, IPAddressUtilities.IPv6HextetCount)
                    .Select(i => i * 2)
                    .Select(i => $"{addressBytes[i]:x2}{addressBytes[i + 1]:x2}");

                return string.Join(":", hextets);
            }
        }

        #endregion
    }
}
