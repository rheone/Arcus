using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Arcus.Converters;
using Arcus.Math;
using Arcus.Utilities;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Arcus
{
    /// <content>
    ///     <see cref="Subnet"/> static factory methods
    /// </content>
    public partial class Subnet
    {
        private const string CouldNotInstantiateSubnetMessage = "could not instantiate subnet";

        /// <summary>
        ///     Regex pattern that matches valid partial IPv4 octet strings (1–4 dot-separated octets, each 0–255).
        ///     Applied with <see cref="RegexOptions.CultureInvariant"/>.
        /// </summary>
        public const string Ipv4OctetPartialPattern =
            @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){0,2}(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)?)?$";

        /// <summary>
        ///     Regex pattern matching the rough shape of a subnet string: an address part followed by an optional slash-prefixed integer.
        ///     Applied with <see cref="RegexOptions.CultureInvariant"/> and <see cref="RegexOptions.IgnoreCase"/>.
        /// </summary>
        public const string RoughSubnetStringPattern = @"^([\da-fA-F:.]+)(?:/([\d]+))?$";

#if NETSTANDARD2_0
        private static readonly Regex IPv4OctetPartialRegex = new(
            Ipv4OctetPartialPattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
#else
        private static Regex IPv4OctetPartialRegex => GetIPv4OctetPartialRegex();

        [GeneratedRegex(Ipv4OctetPartialPattern, RegexOptions.CultureInvariant)]
        private static partial Regex GetIPv4OctetPartialRegex();
#endif

#if NETSTANDARD2_0
        private static readonly Regex RoughSubnetRegex = new(
            RoughSubnetStringPattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
        );
#else
        private static Regex RoughSubnetRegex => GetRoughSubnetRegex();

        [GeneratedRegex(RoughSubnetStringPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex GetRoughSubnetRegex();
#endif

        #region static factory methods

        #region FromNetMask

        /// <summary>
        ///     Create a subnet from an IP Address and netmask
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The netmask must be a valid IPv4 subnet mask (contiguous leading 1-bits) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
        ///         IPv4 only; use <see cref="Subnet(IPAddress, int)"/> for IPv6.
        ///     </para>
        /// </remarks>
        /// <param name="address">the ip address</param>
        /// <param name="netmask">the net mask</param>
        /// <returns>The created subnet</returns>
        /// <exception cref="ArgumentNullException">ipAddress</exception>
        /// <exception cref="ArgumentNullException">netmask</exception>
        /// <exception cref="InvalidOperationException">the given IP Address is not IPv4</exception>
        /// <exception cref="InvalidOperationException">the given netmask is invalid</exception>
        public static Subnet FromNetMask(IPAddress address, IPAddress netmask)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (netmask is null)
            {
                throw new ArgumentNullException(nameof(netmask));
            }

            if (!address.IsIPv4())
            {
                throw new ArgumentException($"{nameof(address)} must be IPv4", nameof(address));
            }

            if (!netmask.IsValidNetMask())
            {
                throw new ArgumentException($"{nameof(netmask)} must be IPv4", nameof(netmask));
            }

            try
            {
                return new Subnet(address, netmask.NetmaskToCidrRoutePrefix());
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(CouldNotInstantiateSubnetMessage, e);
            }
        }

        /// <summary>
        ///     Try to create a subnet from an IP Address and netmask
        /// </summary>
        /// <param name="address">the ip address</param>
        /// <param name="netmask">the net mask</param>
        /// <param name="subnet">the created subnet or <see langword="null" /> on failure</param>
        /// <returns><see langword="true" /> on success</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryFromNetMask(IPAddress address, IPAddress netmask, [NotNullWhen(true)] out Subnet subnet)
#else
        public static bool TryFromNetMask(IPAddress address, IPAddress netmask, out Subnet subnet)
#endif
        {
            try
            {
                subnet = FromNetMask(address, netmask);
                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        #endregion // end: FromNetMask

        /// <summary>
        ///     Construct the smallest possible subnet that would contain both IP addresses encoded as bytes typically the address
        ///     specified are the Network and Broadcast addresses (lower and higher bounds) but this is not necessary. Addresses
        ///     *MUST* be the same address family (either Internetwork or InternetworkV6)
        /// </summary>
        /// <param name="lowAddressBytes">the lower address <see cref="byte" /> array</param>
        /// <param name="highAddressBytes">the high address <see cref="byte" /> array</param>
        /// <returns>The created <see cref="Subnet"/></returns>
        public static Subnet FromBytes(byte[] lowAddressBytes, byte[] highAddressBytes)
        {
            if (lowAddressBytes is null)
            {
                throw new ArgumentNullException(nameof(lowAddressBytes));
            }

            if (highAddressBytes is null)
            {
                throw new ArgumentNullException(nameof(highAddressBytes));
            }

            IPAddress lowAddress;
            try
            {
                lowAddress = new IPAddress(lowAddressBytes);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(
                    $"could not convert {nameof(lowAddressBytes)} to an IPAddress",
                    nameof(lowAddressBytes),
                    e
                );
            }

            IPAddress highAddress;
            try
            {
                highAddress = new IPAddress(highAddressBytes);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(
                    $"could not convert {nameof(highAddressBytes)} to an IPAddress",
                    nameof(highAddressBytes),
                    e
                );
            }

            try
            {
                return new Subnet(lowAddress, highAddress);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(CouldNotInstantiateSubnetMessage, e);
            }
        }

        /// <summary>
        ///     Construct the smallest possible subnet that would contain both IP addresses encoded as bytes typically the address
        ///     specified are the Network and Broadcast addresses (lower and higher bounds) but this is not necessary. Addresses
        ///     *MUST* be the same address family (either Internetwork or InternetworkV6)
        /// </summary>
        /// <param name="lowAddressBytes">the lower address <see cref="byte" /> array</param>
        /// <param name="highAddressBytes">the high address <see cref="byte" /> array</param>
        /// <param name="subnet">the created subnet or <see langword="null" /> on failure</param>
        /// <returns><see langword="true" /> on success</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryFromBytes(byte[] lowAddressBytes, byte[] highAddressBytes, [NotNullWhen(true)] out Subnet subnet)
#else
        public static bool TryFromBytes(byte[] lowAddressBytes, byte[] highAddressBytes, out Subnet subnet)
#endif
        {
            try
            {
                subnet = FromBytes(lowAddressBytes, highAddressBytes);
                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        #region Parse / TryParse

        /// <summary>
        ///     Unsafe parsing of a string into a subnet
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Accepts CIDR notation (<c>a.b.c.d/n</c> or <c>addr::x/n</c>) as defined in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        /// <param name="subnetString">the string to parse a subnet from</param>
        /// <returns>the parsed subnet</returns>
        /// <exception cref="ArgumentException">could not parse input</exception>
        public static Subnet Parse(string subnetString)
        {
            if (subnetString is null)
            {
                throw new ArgumentNullException(nameof(subnetString));
            }

            if (string.IsNullOrWhiteSpace(subnetString))
            {
                throw new ArgumentException("a non empty value is expected", nameof(subnetString));
            }

            var matches = RoughSubnetRegex.Matches(subnetString);

            if (matches.Count != 1)
            {
                throw new FormatException("unexpected format");
            }

            var match = matches[0];

            var addressString = match.Groups[1].Value;
            var routePrefixString = match.Groups[2].Value;

            if (!IPAddress.TryParse(addressString, out var address)) // attempt to parse IP address portion
            {
                throw new FormatException($"cannot parse ip address \"{addressString}\"");
            }

            int routingPrefix;
            if (!string.IsNullOrWhiteSpace(routePrefixString)) // attempt to parse routing prefix in there is a match
            {
                if (!int.TryParse(routePrefixString, out routingPrefix))
                {
                    throw new FormatException($"cannot parse routing prefix \"{routePrefixString}\"");
                }
            }
            else // no routing prefix match, assume it is a single address
            {
                routingPrefix = address.IsIPv4() ? IPAddressUtilities.IPv4BitCount : IPAddressUtilities.IPv6BitCount;
            }

            if (address.IsIPv4() && routingPrefix > IPAddressUtilities.IPv4BitCount)
            {
                throw new ArgumentException("routing prefix must be 32 or less for an IPv4 subnet", nameof(subnetString));
            }

            if (address.IsIPv6() && routingPrefix > IPAddressUtilities.IPv6BitCount)
            {
                throw new ArgumentException("routing prefix must be 128 or less for an IPv6 subnet", nameof(subnetString));
            }

            try
            {
                return new Subnet(address, routingPrefix);
            }
            catch (Exception e)
            {
                throw new FormatException(CouldNotInstantiateSubnetMessage, e);
            }
        }

        /// <summary>
        /// Unsafe parsing of a string address and routing prefix into a subnet
        /// </summary>
        /// <remarks>
        ///
        /// <para>Parses a subnet from an address string and a separate CIDR prefix integer per
        /// <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>. </para>
        /// </remarks>
        /// <param name="addressString">the address string</param>
        /// <param name="routingPrefix">the subnet routing prefix</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="addressString" /> has an invalid <see cref="System.Net.Sockets.AddressFamily" />
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="addressString" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="routingPrefix" /> is less than <c>0</c>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="routingPrefix" /> is out of range of the provided
        /// <see cref="System.Net.Sockets.AddressFamily" />
        /// </exception>
        /// <returns>The parsed <see cref="Subnet"/></returns>
        public static Subnet Parse(string addressString, int routingPrefix)
        {
            if (addressString is null)
            {
                throw new ArgumentNullException(nameof(addressString));
            }

            if (!IPAddress.TryParse(addressString, out var address))
            {
                throw new FormatException($"could not parse {nameof(addressString)} value \"{addressString}\"");
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(address.AddressFamily))
            {
                throw new ArgumentException(
                    $"must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(addressString)
                );
            }

            if (routingPrefix < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routingPrefix));
            }

            if (address.IsIPv4() && routingPrefix > IPAddressUtilities.IPv4BitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(routingPrefix));
            }

            if (address.IsIPv6() && routingPrefix > IPAddressUtilities.IPv6BitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(routingPrefix));
            }

            try
            {
                return new Subnet(address, routingPrefix);
            }
            catch (Exception e)
            {
                throw new FormatException(CouldNotInstantiateSubnetMessage, e);
            }
        }

        /// <summary>
        ///     Unsafe parsing of two string as a new subnet
        /// </summary>
        /// <param name="lowAddressString">the low address string</param>
        /// <param name="highAddressString">the high address string</param>
        /// <returns>The parsed <see cref="Subnet"/></returns>
        public static Subnet Parse(string lowAddressString, string highAddressString)
        {
            if (lowAddressString is null)
            {
                throw new ArgumentNullException(nameof(lowAddressString));
            }

            if (highAddressString is null)
            {
                throw new ArgumentNullException(nameof(highAddressString));
            }

            if (!IPAddress.TryParse(lowAddressString, out var lowAddress))
            {
                throw new FormatException($"could not parse {nameof(lowAddressString)} value \"{lowAddressString}\"");
            }

            if (!IPAddress.TryParse(highAddressString, out var highAddress))
            {
                throw new FormatException($"could not parse {nameof(highAddressString)} value \"{highAddressString}\"");
            }

            if (lowAddress.AddressFamily != highAddress.AddressFamily)
            {
                throw new ArgumentException(
                    $"{nameof(lowAddressString)} and {nameof(highAddressString)} must have matching address families"
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(lowAddress.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(lowAddressString)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(lowAddressString)
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(highAddress.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(highAddressString)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(highAddressString)
                );
            }

            if (!highAddress.IsGreaterThanOrEqualTo(lowAddress))
            {
                throw new InvalidOperationException(
                    $"{nameof(highAddressString)} must be greater or equal to {nameof(lowAddressString)}"
                );
            }

            try
            {
                return new Subnet(lowAddress, highAddress);
            }
            catch (Exception e)
            {
                throw new FormatException(CouldNotInstantiateSubnetMessage, e);
            }
        }

        /// <summary>
        ///     Attempt to parse a string into a subnet
        /// </summary>
        /// <param name="subnetString">the string to parse</param>
        /// <param name="subnet">the created subnet or <see langword="null" /> on failure</param>
        /// <returns><see langword="true" /> on success</returns>
        public static bool TryParse(string subnetString,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out Subnet subnet)
        {
            try
            {
                subnet = Parse(subnetString);
                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        /// <summary>
        ///     Try to parse <paramref name="addressString" /> as an <see cref="IPAddress" /> for a Subnet with the given routing
        ///     prefix
        /// </summary>
        /// <param name="addressString">the address string</param>
        /// <param name="routingPrefix">the subnet routing prefix</param>
        /// <param name="subnet">the created subnet or <see langword="null" /> on failure</param>
        /// <returns><see langword="true" /> on success</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(string addressString, int routingPrefix, [NotNullWhen(true)] out Subnet subnet)
#else
        public static bool TryParse(string addressString, int routingPrefix, out Subnet subnet)
#endif
        {
            try
            {
                subnet = Parse(addressString, routingPrefix);
                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        /// <summary>
        ///     Try parse two strings as addresses
        /// </summary>
        /// <param name="lowAddressString">the low address string</param>
        /// <param name="highAddressString">the high address string</param>
        /// <param name="subnet">the created subnet or <see langword="null" /> on failure</param>
        /// <returns><see langword="true" /> on success</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(string lowAddressString, string highAddressString, [NotNullWhen(true)] out Subnet subnet)
#else
        public static bool TryParse(string lowAddressString, string highAddressString, out Subnet subnet)
#endif
        {
            try
            {
                subnet = Parse(lowAddressString, highAddressString);

                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        #region From Partial

        /// <summary>
        ///     Try to convert a partial IPv4 address into a subnet based on found provided partial octets
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         IPv4 dotted-quad structure (4 octets × 8 bits = 32 bits total) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
        ///         Each supplied octet contributes 8 bits to the routing prefix.
        ///     </para>
        /// </remarks>
        /// <param name="input">the partial IP address to parse</param>
        /// <param name="subnet">the subnet created</param>
        /// <returns>true on success</returns>
        public static bool TryIPv4FromPartial(string input,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out Subnet subnet)
        {
            if (string.IsNullOrWhiteSpace(input) || !IPv4OctetPartialRegex.IsMatch(input))
            {
                subnet = null;
                return false;
            }

            input = input.TrimEnd('.');
            var octetCount = input.Count(c => c == '.') + 1;
            var addressString = input + string.Concat(Enumerable.Repeat(".0", IPAddressUtilities.IPv4OctetCount - octetCount));

            try
            {
                subnet = Parse(addressString, octetCount * 8);
                return true;
            }
            catch
            {
                subnet = null;
                return false;
            }
        }

        /// <summary>
        ///     Given a IPv6 cidr-like or IPv6 like string build a collection of all possible valid subnets that could be intended
        ///     by the input
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         IPv6 colon-hex notation with <c>::</c> zero-group collapse per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.2">RFC 4291 §2.2</see>.
        ///     </para>
        /// </remarks>
        /// <param name="input">the partial ipv6 cidr or address</param>
        /// <param name="subnets">a collection of all possible matching subnets on success, or an empty collection on failure</param>
        /// <returns><see langword="true" /> on success</returns>
        [Obsolete(
            "the needs for this method are very specialized and may not be what the developer is expecting; this is likely to be replaced by a host of other more explicit and useful methods"
        )]
        public static bool TryIPv6FromPartial(string input, out IEnumerable<Subnet> subnets)
        {
            // try to discover possible garbage or incomplete input
            int hextetCount;
            if (
                string.IsNullOrWhiteSpace(input)
                || input.Equals(":", StringComparison.OrdinalIgnoreCase)
                || input.Contains(":::")
                || DoubleColonsAppearsMultipleTimes(input)
                || (hextetCount = input.Split([':'], StringSplitOptions.RemoveEmptyEntries).Length)
                    > IPAddressUtilities.IPv6HextetCount
                || (
                    hextetCount >= IPAddressUtilities.IPv6HextetCount && input.EndsWith(":", StringComparison.OrdinalIgnoreCase)
                )
            ) // too many hextets
            {
                subnets = [];
                return false;
            }

            // parseable as a well defined IPv6 subnet already
            if (input.Contains('/')
#if NETSTANDARD2_0
                && !input.EndsWith("/")
#else
                && !input.EndsWith('/')
#endif

                && TryParse(input, out var subnet) && subnet.IsIPv6)
            {
                subnets = [subnet];
                return true;
            }

            // TODO this could probably be done more cleanly, and provide a more expected result

            // a IPv6 cidr partial is provided
            if (
                (
                    IPAddress.TryParse(input, out var address) // treat as a complete address, may contain a '::' or not
                    || (input.Contains("::") && IPAddress.TryParse(input.TrimEnd(':'), out address))
                    || IPAddress.TryParse(input.TrimEnd(':') + "::", out address)
                ) && address.IsIPv6()
            ) // no collapse, but incomplete address
            {
                var trimmedPartial = input.TrimEnd(':'); // remove trailing ":" or "::" if exists

                // break up entry on hextets, an empty hextet implies a collapse
                var hextets = trimmedPartial
                    .Split([':'], StringSplitOptions.None) // DO NOT remove empty splits
                    .ToList();

                // should contain an empty, pump the first with appropriate values

                // get the index of the collapse
                var collapse = hextets
                    .Select((value, index) => new { value, index })
                    .FirstOrDefault(pair => string.IsNullOrWhiteSpace(pair.value));

                int collapseIndex;
                if (collapse is null && hextets.Count == 8) // no collapse - fully fledged address, all 8 hextets present
                {
                    subnets = [Parse(input, IPAddressUtilities.IPv6BitCount)];
                    return true;
                }

                // introduce a collapse at the end if one does not exist
                if (collapse is null) // not fully fledged, add a collapse to the end
                {
                    hextets.Add(string.Empty);
                    collapseIndex = hextets.Count - 1;
                }
                else // a collapse is present
                {
                    collapseIndex = collapse.index;
                }

                var subnetsList = new List<Subnet>();

                hextetCount = hextets.Count;
                var permutationLimit = (IPAddressUtilities.IPv6HextetCount + 1) - hextetCount; // number of permutations of IP partial, every hextet available removes a permutation
                for (var permutation = 0; permutation <= permutationLimit; permutation++)
                {
                    var hextetsCopy = hextets.ToList();
                    hextetsCopy.RemoveAt(collapseIndex); // collapsed empty item
                    hextetsCopy.InsertRange(collapseIndex, Enumerable.Repeat("0", permutation)); // add zeros as appropriate

                    var addressString = string.Join(":", hextetsCopy); // re join string

                    if (
                        !IPAddress.TryParse(addressString + "::", out var subnetAddress)
                        && !IPAddress.TryParse(addressString, out subnetAddress)
                    )
                    {
                        subnets = [];
                        return false;
                    }

                    var routePrefix = ((permutation + hextetCount) - 1) * 16;
                    subnetsList.Add(new Subnet(subnetAddress, routePrefix));
                }

                subnets = subnetsList;
                return true;
            }

            // fail; could not parse anything useful from the input
            subnets = [];
            return false;

            // checks input for multiple occurrences of discrete "::" substrings

            static bool DoubleColonsAppearsMultipleTimes(string @in)
            {
                const string colons = "::";
                var firstIndex = @in.IndexOf(colons, StringComparison.Ordinal);
                return firstIndex >= 0 && @in.IndexOf(colons, firstIndex + colons.Length, StringComparison.Ordinal) >= 0;
            }
        }

        #endregion // end: From Partial

        #endregion // end: Parse / TryParse

        #endregion // end: Static Factory Methods
    }
}
