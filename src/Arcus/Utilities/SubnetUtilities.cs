using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Arcus.Math;

namespace Arcus.Utilities
{
    /// <summary>
    ///     Static utility class containing miscellaneous operations for <see cref="Subnet" /> objects
    /// </summary>
    public static class SubnetUtilities
    {
        /// <summary>
        ///     A collection of all known private IP Address ranges.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Contains the four RFC-defined private/ULA address blocks:
        ///         <c>10.0.0.0/8</c>, <c>172.16.0.0/12</c>, and <c>192.168.0.0/16</c> per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc1918#section-3">RFC 1918 §3</see>;
        ///         and <c>fd00::/8</c> (IPv6 Unique Local Addresses) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4193#section-8">RFC 4193 §8</see>.
        ///     </para>
        ///     <para>
        ///         <b>Breaking change (readonly):</b> This field is now <see langword="readonly" />.
        ///         Prior to this version the field reference could be reassigned by external code.
        ///         Code that assigned to <c>SubnetUtilities.PrivateIPAddressRangesList = ...</c>
        ///         will no longer compile. The <see cref="IReadOnlyList{T}" /> contract already
        ///         prevented mutation of the list contents; this change extends that guarantee to
        ///         the field reference itself.
        ///     </para>
        /// </remarks>
        public static readonly IReadOnlyList<Subnet> PrivateIPAddressRangesList = new[]
        {
            // IPv4 RFC 1918
            Subnet.Parse("10.0.0.0", 8),
            Subnet.Parse("172.16.0.0", 12),
            Subnet.Parse("192.168.0.0", 16),
            // IPv6 RFC 4193
            Subnet.Parse("fd00::", 8),
        }.ToList().AsReadOnly();

        /// <summary>
        ///     A collection of all known Link Local IP Address ranges.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Contains <c>169.254.0.0/16</c> (IPv4 link-local) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc3927#section-2.1">RFC 3927 §2.1</see>
        ///         and <c>fe80::/10</c> (IPv6 link-local) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.4">RFC 4291 §2.4</see>.
        ///     </para>
        ///     <para>
        ///         <b>Breaking change (readonly):</b> This field is now <see langword="readonly" />.
        ///         Prior to this version the field reference could be reassigned by external code.
        ///         Code that assigned to <c>SubnetUtilities.LinkLocalIPAddressRangesList = ...</c>
        ///         will no longer compile.
        ///     </para>
        /// </remarks>
        public static readonly IReadOnlyList<Subnet> LinkLocalIPAddressRangesList = new[]
        {
            // RFC 3927
            Subnet.Parse("169.254.0.0", 16),
            // RFC 4291
            Subnet.Parse("fe80::", 10),
        }.ToList().AsReadOnly();

        /// <summary>
        ///     Get The fewest consecutive subnets that would fill the range between the given addresses (inclusive)
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Uses CIDR block sizing (each increment of prefix length halves the block: 2<sup>max−n</sup> addresses) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        /// <param name="left">lowest order IP Address</param>
        /// <param name="right">highest order IP Address</param>
        /// <returns>an enumerable of Subnet</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Address families must match</exception>
        /// <exception cref="InvalidOperationException">Address families must be InterNetwork or InternetworkV6</exception>
        public static IEnumerable<Subnet> FewestConsecutiveSubnetsFor(IPAddress left, IPAddress right)
        {
            #region defense

            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(left.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(left)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(left)
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(right.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(right)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(right)
                );
            }

            if (left.AddressFamily != right.AddressFamily)
            {
                throw new InvalidOperationException($"{nameof(left)} and {nameof(right)} must have matching address families");
            }

            #endregion // end: defense

            var minHead = IPAddressMath.Min(left, right);
            var maxTail = IPAddressMath.Max(left, right);

            return FilledSubnets(minHead, maxTail, new Subnet(minHead, maxTail));

            // recursive function call
            // Works by verifying that passed subnet isn't bounded by head, tail IP Addresses
            // if not breaks subnet in half and recursively tests, building in essence a binary tree of testable subnet paths

            static IEnumerable<Subnet> FilledSubnets(IPAddress head, IPAddress tail, Subnet subnet)
            {
                var networkPrefixAddress = subnet.NetworkPrefixAddress;
                var broadcastAddress = subnet.BroadcastAddress;

                // the given subnet is the perfect size for the head/tail (not papa bear, not mama bear, but just right with baby bear)
                if (networkPrefixAddress.IsGreaterThanOrEqualTo(head) && broadcastAddress.IsLessThanOrEqualTo(tail))
                {
                    return [subnet];
                }

                // increasing the route prefix by 1 creates a subnet of half the initial size (due 2^(max-n) route prefix sizing)
                var nextSmallestRoutePrefix = subnet.RoutingPrefix + 1;

                // over-iterated route prefix, no valid subnet beyond this point; end search on this branch
                if (
                    (subnet.IsIPv6 && nextSmallestRoutePrefix > IPAddressUtilities.IPv6BitCount)
                    || (subnet.IsIPv4 && nextSmallestRoutePrefix > IPAddressUtilities.IPv4BitCount)
                )
                {
                    return []; // no subnets to be found here, stop investigating branch of tree
                }

                // build head subnet
                var headSubnet = new Subnet(networkPrefixAddress, nextSmallestRoutePrefix);

                // use the next address after the end of the head subnet as the first address for the tail subnet
                if (!IPAddressMath.TryIncrement(headSubnet.BroadcastAddress, out var tailStartingAddress))
                {
                    throw new InvalidOperationException($"unable to increment {headSubnet.BroadcastAddress}");
                }

                var tailSubnet = new Subnet(tailStartingAddress, nextSmallestRoutePrefix);

                // break into binary search tree, searching both head subnet and tail subnet for ownership of head and tail ip
                return FilledSubnets(head, tail, headSubnet).Concat(FilledSubnets(head, tail, tailSubnet));
            }
        }

        /// <summary>
        ///     Return the largest subnet (smallest route prefix value)
        ///     if more than one "largest" return is not predictable beyond that one will be returned
        ///     Consider usage of DefaultSubnetComparer
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         A smaller routing prefix means a larger address block per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>
        ///         (block size = 2<sup>max−prefix</sup>).
        ///     </para>
        /// </remarks>
        /// <param name="subnets">the subnets to search</param>
        /// <returns>
        ///     The first largest subnet by routing prefix, or <see langword="null" /> if no <paramref name="subnets" /> to
        ///     choose from
        /// </returns>
        public static Subnet LargestSubnet(IEnumerable<Subnet> subnets)
        {
            var enumerable = (subnets ?? []).ToList();

            return !enumerable.Any()
                ? null
                : enumerable.Where(s => s != null).Aggregate((s1, s2) => s1.RoutingPrefix < s2.RoutingPrefix ? s1 : s2);
        }

        /// <summary>
        ///     Return the smallest subnet (largest route prefix value)
        ///     if more than one "smallest" return is not predictable beyond that one will be returned
        ///     Consider usage of DefaultSubnetComparer
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         A larger routing prefix means a smaller address block per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>
        ///         (block size = 2<sup>max−prefix</sup>).
        ///     </para>
        /// </remarks>
        /// <param name="subnets">the list of subnets</param>
        /// <returns>The first smallest subnet by routing prefix, or null if no subnets to choose from</returns>
        public static Subnet SmallestSubnet(IEnumerable<Subnet> subnets)
        {
            var enumerable = (subnets ?? []).ToList();

            return !enumerable.Any()
                ? null
                : enumerable.Where(s => s != null).Aggregate((s1, s2) => s1.RoutingPrefix > s2.RoutingPrefix ? s1 : s2);
        }
    }
}
