using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Arcus.Math;
using Arcus.Utilities;

namespace Arcus
{
    /// <summary>
    ///     An <see langword="abstract" /> implementation of <see cref="IIPAddressRange" /> built to work with IPv4 and IPv6
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         IPv4 addresses are represented as 32-bit unsigned integers per
    ///         <see href="https://www.rfc-editor.org/rfc/rfc791#section-2.3">RFC 791 §2.3</see>.
    ///         IPv6 addresses are represented as 128-bit unsigned integers per
    ///         <see href="https://www.rfc-editor.org/info/rfc4291/#section-2">RFC 4291 §2</see>;
    ///         <see cref="System.Numerics.BigInteger"/> is used throughout because IPv6 ranges can exceed <see cref="long.MaxValue"/>.
    ///     </para>
    /// </remarks>
    public abstract partial class AbstractIPAddressRange : IIPAddressRange
    {
        /// <summary>
        ///     <see langword="true" /> Gets a value indicating whether if the subnet describes a single ip address
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the subnet describes a single ip address
        /// </value>
        public bool IsSingleIP => this.Length == 1;

        /// <inheritdoc />
        public AddressFamily AddressFamily => this.Head.AddressFamily;

        /// <inheritdoc />
        public bool IsIPv4 => this.Head.IsIPv4();

        /// <inheritdoc />
        public bool IsIPv6 => this.Head.IsIPv6();

        /// <inheritdoc />
        public IPAddress Head { get; }

        /// <inheritdoc />
        public IPAddress Tail { get; }

        /// <inheritdoc />
        public BigInteger Length { get; }

        #region AddressTuple

        /// <summary>
        ///     AddressTuple for moving around a pair of <see cref="IPAddress" /> objects as a unit
        /// </summary>
        protected readonly struct AddressTuple : IEquatable<AddressTuple>
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="AddressTuple" /> struct.
            /// </summary>
            /// <param name="head">the head address</param>
            /// <param name="tail">the tail address</param>
            public AddressTuple(IPAddress head, IPAddress tail)
            {
                this.Head = head ?? throw new ArgumentNullException(nameof(head));
                this.Tail = tail ?? throw new ArgumentNullException(nameof(tail));
            }

            /// <summary>
            ///     Gets head
            /// </summary>
            /// <value>
            /// Head
            /// </value>
            public IPAddress Head { get; }

            /// <summary>
            ///     Gets tail
            /// </summary>
            /// <value>
            /// Tail
            /// </value>
            public IPAddress Tail { get; }

            /// <inheritdoc />
            public bool Equals(AddressTuple other)
            {
                return this.Head.Equals(other.Head) && this.Tail.Equals(other.Tail);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is AddressTuple other && this.Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode() => HashCode.Combine(Head, Tail);

            /// <summary>
            ///     Equals operation
            /// </summary>
            /// <param name="left">left operand</param>
            /// <param name="right">right operand</param>
            /// <returns><see langword="true" /> if <paramref name="left"></paramref> and <paramref name="right" />are equal</returns>
            public static bool operator ==(AddressTuple left, AddressTuple right)
            {
                return left.Equals(right);
            }

            /// <summary>
            ///     Not Equals operation
            /// </summary>
            /// <param name="left">left operand</param>
            /// <param name="right">right operand</param>
            /// <returns><see langword="true" /> if <paramref name="left"></paramref> and <paramref name="right" />are equal</returns>
            public static bool operator !=(AddressTuple left, AddressTuple right)
            {
                return !(left == right);
            }
        }

        #endregion // end: AddressTuple

        #region Ctor

        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractIPAddressRange"/> class.
        /// </summary>
        /// <param name="head">the range head (lowest valued <see cref="IPAddress" />)</param>
        /// <param name="tail">the range tail (highest valued <see cref="IPAddress" />)</param>
        protected AbstractIPAddressRange(IPAddress head, IPAddress tail)
        {
            #region defense

            if (head == null)
            {
                throw new ArgumentNullException(nameof(head));
            }

            if (tail == null)
            {
                throw new ArgumentNullException(nameof(tail));
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(head.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(head)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(head)
                );
            }

            if (!IPAddressUtilities.ValidAddressFamilies.Contains(tail.AddressFamily))
            {
                throw new ArgumentException(
                    $"{nameof(tail)} must have an address family equal to {string.Join(", ", IPAddressUtilities.ValidAddressFamilies)}",
                    nameof(tail)
                );
            }

            if (head.AddressFamily != tail.AddressFamily)
            {
                throw new InvalidOperationException($"{nameof(head)} and {nameof(tail)} must have matching address families");
            }

            if (!tail.IsGreaterThanOrEqualTo(head))
            {
                throw new InvalidOperationException($"{nameof(tail)} must be greater or equal to {nameof(head)}");
            }

            #endregion // end: defense

            this.Head = head;
            this.Tail = tail;
            this.Length = CalculateLength();

            BigInteger CalculateLength()
            {
                var tailWrap = BigEndianBitWrapper.FromBytes(tail.GetAddressBytes());
                var headWrap = BigEndianBitWrapper.FromBytes(head.GetAddressBytes());
                return tailWrap.Subtract(headWrap).ToBigInteger() + 1;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractIPAddressRange"/> class.
        /// </summary>
        /// <param name="addressTuple">an <see cref="AddressTuple"/> representing two addresses</param>
        protected AbstractIPAddressRange(AddressTuple addressTuple)
            : this(addressTuple.Head, addressTuple.Tail)
        {
            // nothing additional to do
        }

        #endregion // end: Ctor
    }
}
