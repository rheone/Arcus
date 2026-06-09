using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using Arcus.Math;
using Arcus.Utilities;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

#if NETSTANDARD2_0
using System.Runtime.Serialization;
#endif

namespace Arcus
{
    /// <summary>
    ///     An IPv4 or IPv6 subnetwork representation - the work horse and original intention of the Arcus library
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         IPv4 addresses are 32-bit unsigned integers per
    ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see> (routing prefix bounded [0, 32]).
    ///         IPv6 addresses are 128-bit unsigned integers per
    ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see> (routing prefix bounded [0, 128]).
    ///         Subnet mask semantics are defined in
    ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
    ///         CIDR prefix notation (<c>address/prefix-length</c>) is defined in
    ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
    ///     </para>
    /// </remarks>
    [DebuggerDisplay("{ToString()}")]
    [Serializable]
    public partial class Subnet : AbstractIPAddressRange
    {
        /// <summary>
        ///     Gets the number of usable addresses in the subnet (ignores Broadcast and Network addresses)
        /// </summary>
        /// <value>
        /// the number of usable addresses in the subnet (ignores Broadcast and Network addresses)
        /// </value>
        /// <remarks>
        ///     <para>
        ///         Excludes the network address and broadcast address as defined in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
        ///     </para>
        /// </remarks>
        public BigInteger UsableHostAddressCount => Length >= 2 ? Length - 2 : 0;

        /// <summary>
        ///     Gets the broadcast of the subnet (highest order ip address)
        /// </summary>
        /// <value>
        /// the broadcast of the subnet (highest order ip address)
        /// </value>
        /// <remarks>
        ///     <para>
        ///         The highest address in the subnet (all host bits set) per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
        ///     </para>
        /// </remarks>
        public IPAddress BroadcastAddress => Tail;

        /// <summary>
        ///     Gets the calculated Netmask of the subnet, only valid for IPv4 based subnets will be <see langword="null" /> on IPv6
        ///     subnets
        /// </summary>
        /// <value>
        /// the calculated Netmask of the subnet, only valid for IPv4 based subnets will be <see langword="null" /> on IPv6
        ///     subnets
        /// </value>
        /// <remarks>
        ///     <para>
        ///         A valid subnet mask is a contiguous sequence of leading 1-bits followed by 0-bits per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc950#section-2">RFC 950 §2</see>.
        ///         IPv4 only; <see langword="null" /> for IPv6 subnets.
        ///     </para>
        /// </remarks>
        public IPAddress Netmask { get; }

        /// <summary>
        ///     Gets the network prefix of the subnet (lowest order IP address)
        /// </summary>
        /// <value>
        /// the network prefix of the subnet (lowest order IP address)
        /// </value>
        /// <remarks>
        ///     <para>
        ///         The network address derived by AND-ing the IP address with the subnet mask per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        public IPAddress NetworkPrefixAddress => Head;

        /// <summary>
        ///     Gets the routing prefix used to specify the ip address
        /// </summary>
        /// <value>
        /// the routing prefix used to specify the ip address
        /// </value>
        /// <remarks>
        ///     <para>
        ///         The prefix length in CIDR notation per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///         Valid range is [0, 32] for IPv4 and [0, 128] for IPv6.
        ///     </para>
        /// </remarks>
        public int RoutingPrefix { get; }

        #region Formatting

        /// <inheritdoc />
        /// <remarks>
        ///     <para>
        ///         The <c>g</c>/<c>G</c> (and default) format produces canonical CIDR notation (<c>address/prefix-length</c>)
        ///         per <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        public override string ToString(
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [AllowNull]
#endif
            string format,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [AllowNull]
#endif
            IFormatProvider formatProvider)
        {
            formatProvider ??= CultureInfo.InvariantCulture;

            return format?.Trim() switch
            {
                // unspecified
                null or { Length: 0 } or "g" or "G" => $"{this.NetworkPrefixAddress}/{this.RoutingPrefix}",
                // "friendly" formats
                "f" or "F" => IsSingleIP ? $"{this.NetworkPrefixAddress}" : $"{this.NetworkPrefixAddress}/{this.RoutingPrefix}",
                // range formats
                "r" or "R" => $"{this.NetworkPrefixAddress} - {this.BroadcastAddress}",
                // delegate to base
                _ => base.ToString(format, formatProvider),
            };
        }

        #endregion // end: Formatting

        #region set based operations

        /// <summary>
        ///     check if a given subnet falls within the specified subnet
        /// </summary>
        /// <param name="subnet">the subnet to test</param>
        /// <returns>true if the passed subnet is contained within this</returns>
        public bool Contains(Subnet subnet) =>
            subnet != null && Contains(subnet.NetworkPrefixAddress) && Contains(subnet.BroadcastAddress);

        /// <summary>
        ///     check if the given subnets overlaps this
        /// </summary>
        /// <param name="subnet">the subnet to check the overlap of</param>
        /// <returns>true if there is an overlap</returns>
        public bool Overlaps(Subnet subnet) => subnet != null && (subnet.Contains(this) || this.Contains(subnet));

        #endregion // end: set based operations

        #region Ctor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subnet" /> class.
        ///     Construct the smallest possible subnet that would contain both IP addresses
        ///     typically the address specified are the Network and Broadcast addresses
        ///     (lower and higher bounds) but this is not necessary.
        ///     Addresses *MUST* be the same address family (either Internetwork or InternetworkV6)
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Constructs the smallest subnet (largest routing prefix) that contains both addresses per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///     </para>
        /// </remarks>
        /// <param name="lowAddress">a address to be contained within the subnet</param>
        /// <param name="highAddress">another address to be contained within the subnet</param>
        public Subnet(IPAddress lowAddress, IPAddress highAddress)
            : this(CtorFactory(lowAddress, highAddress)) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subnet" /> class.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Constructs a subnet from an address and CIDR prefix length per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4632#section-2">RFC 4632 §2</see>.
        ///         The network prefix address is derived by AND-ing <paramref name="address"/> with the prefix mask.
        ///     </para>
        /// </remarks>
        /// <param name="address">the ip address</param>
        /// <param name="routingPrefix">the routing prefix</param>
        /// <exception cref="ArgumentException">IP Address must be IPv4 or IPv6</exception>
        /// <exception cref="ArgumentException">Routing prefix is out of range</exception>
        public Subnet(IPAddress address, int routingPrefix)
            : this(CtorFactory(address, routingPrefix)) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subnet" /> class.
        ///     Private constructor that receives pre-computed Head/Tail, netmask, and routing prefix.
        ///     Avoids redundant calls to <see cref="NormalizeAndCreateNetMask(IPAddress, IPAddress)" />.
        /// </summary>
        private Subnet(CtorFactoryResult result)
            : base(result.Tuple)
        {
            this.RoutingPrefix = result.RoutingPrefix;
            this.Netmask = IsIPv4 ? result.Netmask : null;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subnet" /> class.
        ///     contains only a single ip address
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Creates a host route: /32 for IPv4 per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>,
        ///         or /128 for IPv6 per
        ///         <see href="https://www.rfc-editor.org/rfc/rfc4291#section-2.1">RFC 4291 §2.1</see>.
        ///     </para>
        /// </remarks>
        /// <param name="address">the ip address</param>
        public Subnet(IPAddress address)
            : base(address, address)
        {
            this.RoutingPrefix = IsIPv4 ? IPAddressUtilities.IPv4BitCount : IPAddressUtilities.IPv6BitCount;

            if (IsIPv6)
            {
                return;
            }

            var netmaskBytes = Enumerable.Repeat((byte)0xff, IPAddressUtilities.IPv4ByteCount).ToArray();

            this.Netmask = new IPAddress(netmaskBytes);
        }

#if NETSTANDARD2_0
        /// <summary>Initializes a new instance of the <see cref="Subnet"/> class.</summary>
        /// <param name="info">serialization info</param>
        /// <param name="context">serialization context</param>
        /// <exception cref="ArgumentNullException"><paramref name="info"/> is <see langword="null"/></exception>
        protected Subnet(SerializationInfo info, StreamingContext context)
            : this(
                new IPAddress(
                    (byte[])
                        (info ?? throw new ArgumentNullException(nameof(info))).GetValue(
                            nameof(BroadcastAddress),
                            typeof(byte[])
                        )
                ),
                (int)info.GetValue(nameof(RoutingPrefix), typeof(int))
            ) { }
#endif

        private static CtorFactoryResult CtorFactory(IPAddress lowAddress, IPAddress highAddress)
        {
            #region Defense

            if (lowAddress is null)
            {
                throw new ArgumentNullException(nameof(lowAddress));
            }

            if (highAddress is null)
            {
                throw new ArgumentNullException(nameof(highAddress));
            }

            if (lowAddress.AddressFamily != highAddress.AddressFamily)
            {
                throw new ArgumentException(
                    $"{nameof(lowAddress)} and {nameof(highAddress)} must have matching address families"
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(lowAddress.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(lowAddress)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(lowAddress)
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(highAddress.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(highAddress)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(highAddress)
                );
            }

            if (!highAddress.IsGreaterThanOrEqualTo(lowAddress))
            {
                throw new InvalidOperationException($"{nameof(highAddress)} must be greater or equal to {nameof(lowAddress)}");
            }

            #endregion // end: Defense

            var result = NormalizeAndCreateNetMask(lowAddress, highAddress);
            return new CtorFactoryResult(new AddressTuple(result.Head, result.Tail), result.Mask, result.Prefix);
        }

        private static CtorFactoryResult CtorFactory(IPAddress address, int routingPrefix)
        {
            #region Defense

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(address.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(address)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(address)
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

            #endregion // end: Defense

            var result = NormalizeAndCreateNetMask(address, routingPrefix);
            return new CtorFactoryResult(new AddressTuple(result.Head, result.Tail), result.Mask, routingPrefix);
        }

        private readonly struct CtorFactoryResult(AddressTuple tuple, IPAddress netmask, int routingPrefix)
        {
            public AddressTuple Tuple { get; } = tuple;
            public IPAddress Netmask { get; } = netmask;
            public int RoutingPrefix { get; } = routingPrefix;
        }

        #endregion // end: Ctor

        #region Static metods, may be appropriate for extracting

        private readonly struct AddressMaskAndPrefixTuple
        {
            public AddressMaskAndPrefixTuple(IPAddress head, IPAddress tail, IPAddress mask, int prefix)
            {
                this.Head = head ?? throw new ArgumentNullException(nameof(head));
                this.Tail = tail ?? throw new ArgumentNullException(nameof(tail));
                this.Mask = mask ?? throw new ArgumentNullException(nameof(mask));

                if (prefix < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(prefix));
                }

                this.Prefix = prefix;
            }

            /// <summary>
            ///     Gets head
            /// </summary>
            public IPAddress Head { get; }

            /// <summary>
            ///     Gets tail
            /// </summary>
            public IPAddress Tail { get; }

            /// <summary>
            ///     Gets mask
            /// </summary>
            public IPAddress Mask { get; }

            /// <summary>
            ///     Gets prefix
            /// </summary>
            public int Prefix { get; }
        }

        private readonly struct AddressAndMaskTuple(IPAddress head, IPAddress tail, IPAddress mask)
        {
            /// <summary>
            ///     Gets head
            /// </summary>
            public IPAddress Head { get; } = head ?? throw new ArgumentNullException(nameof(head));

            /// <summary>
            ///     Gets tail
            /// </summary>
            public IPAddress Tail { get; } = tail ?? throw new ArgumentNullException(nameof(tail));

            /// <summary>
            ///     Gets mask
            /// </summary>
            public IPAddress Mask { get; } = mask ?? throw new ArgumentNullException(nameof(mask));
        }

        private static AddressMaskAndPrefixTuple NormalizeAndCreateNetMask(IPAddress head, IPAddress tail)
        {
            var headBytes = head.GetAddressBytes();
            var tailBytes = tail.GetAddressBytes();

            var routingPrefix = CalculateRoutingPrefix(headBytes, tailBytes);
            var result = NormalizeAndCreateNetMask(head, routingPrefix);
            return new AddressMaskAndPrefixTuple(result.Head, result.Tail, result.Mask, routingPrefix);

            static int CalculateRoutingPrefix(byte[] hb, byte[] tb)
            {
                var bitCount = hb.Length * 8; // 8 bits per byte

                // iterate in order to find the count of common bits starting at the 0th element of each byte array
                for (var i = 0; i < bitCount; i++)
                {
                    var byteIndex = i / 8;
                    var bitmask = (byte)(0x80 >> (i % 8));
                    if ((hb[byteIndex] & bitmask) != (tb[byteIndex] & bitmask)) // if bits aren't equal break
                    {
                        return i; // return index when matching stops
                    }
                }

                return bitCount; // all match, return length
            }
        }

        private static AddressAndMaskTuple NormalizeAndCreateNetMask(IPAddress head, int routingPrefix)
        {
            var headWrap = BigEndianBitWrapper.FromBytes(head.GetAddressBytes());
            var maskWrap = BigEndianBitWrapper.CreateMask(headWrap.ByteWidth, routingPrefix);
            return new AddressAndMaskTuple(
                new IPAddress((headWrap & maskWrap).ToBytes()),
                new IPAddress((headWrap | ~maskWrap).ToBytes()),
                new IPAddress(maskWrap.ToBytes())
            );
        }

        #endregion // end: Static metods, may be appropriate for extracting

        #region Deconstructors

        /// <summary>
        ///     Deconstruct to Network Prefix Address, Broadcast Address, Netmask, and Routing Prefix
        /// </summary>
        /// <param name="networkPrefixAddress">the subnet <see cref="NetworkPrefixAddress" /></param>
        /// <param name="broadcastAddress">the subnet <see cref="BroadcastAddress" /></param>
        /// <param name="netmask">the subnet <see cref="Netmask" />, will be <see langword="null" /> for non IPv6 subnets</param>
        /// <param name="routingPrefix">the subnet <see cref="RoutingPrefix" /></param>
        public void Deconstruct(
            out IPAddress networkPrefixAddress,
            out IPAddress broadcastAddress,
            out IPAddress netmask,
            out int routingPrefix
        )
        {
            networkPrefixAddress = this.NetworkPrefixAddress;
            broadcastAddress = this.BroadcastAddress;
            netmask = this.Netmask;
            routingPrefix = this.RoutingPrefix;
        }

        /// <summary>
        ///     Deconstruct to Network Prefix Address and Routing Prefix
        /// </summary>
        /// <param name="networkPrefixAddress">the subnet <see cref="NetworkPrefixAddress" /></param>
        /// <param name="routingPrefix">the subnet <see cref="RoutingPrefix" /></param>
        public void Deconstruct(out IPAddress networkPrefixAddress, out int routingPrefix)
        {
            networkPrefixAddress = this.NetworkPrefixAddress;
            routingPrefix = this.RoutingPrefix;
        }

        #endregion // end: Deconstruct
    }
}
