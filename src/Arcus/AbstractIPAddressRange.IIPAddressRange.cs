using System.Net;
using Arcus.Math;
using Arcus.Utilities;

namespace Arcus
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> implementation of <see cref="IIPAddressRange"/>
    /// </content>
    public abstract partial class AbstractIPAddressRange
    {
        #region Deconstructors

        /// <inheritdoc />
        public void Deconstruct(out IPAddress head, out IPAddress tail)
        {
            head = this.Head;
            tail = this.Tail;
        }

        #endregion // end: Deconstruct

        #region TryGetLength

        /// <inheritdoc />
        public bool TryGetLength(out int length)
        {
            var actualLength = this.Length;

            if (actualLength <= int.MaxValue)
            {
                length = (int)actualLength;
                return true;
            }

            length = -1;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetLength(out long length)
        {
            var actualLength = this.Length;

            if (actualLength <= long.MaxValue)
            {
                length = (long)actualLength;
                return true;
            }

            length = -1;
            return false;
        }

        #endregion // end: TryGetLength

        #region Set Operations

        #region Contains

        /// <inheritdoc />
        public bool Contains(IIPAddressRange addressRange)
        {
            return ReferenceEquals(this, addressRange)
                || Equals(this, addressRange)
                || (addressRange != null && this.Contains(addressRange.Head) && this.Contains(addressRange.Tail));
        }

        /// <inheritdoc />
        public bool Contains(IPAddress address)
        {
            return address != null && address.AddressFamily == this.AddressFamily && address.IsBetween(this.Head, this.Tail);
        }

        #endregion // end: Contains

        #region Overlap and Touches

        /// <inheritdoc />
        public bool HeadOverlappedBy(IIPAddressRange addressRange)
        {
            return ReferenceEquals(this, addressRange)
                || Equals(this, addressRange)
                || (addressRange != null && addressRange.Contains(this.Head));
        }

        /// <inheritdoc />
        public bool TailOverlappedBy(IIPAddressRange addressRange)
        {
            return ReferenceEquals(this, addressRange)
                || Equals(this, addressRange)
                || (addressRange != null && addressRange.Contains(this.Tail));
        }

        /// <inheritdoc />
        public bool Overlaps(IIPAddressRange addressRange)
        {
            return ReferenceEquals(this, addressRange)
                || Equals(this, addressRange)
                || (
                    addressRange != null
                    && (this.Contains(addressRange.Head) || this.Contains(addressRange.Tail) || addressRange.Contains(this))
                );
        }

        /// <inheritdoc />
        public bool Touches(IIPAddressRange addressRange)
        {
            return addressRange != null
                && this.AddressFamily == addressRange.AddressFamily
                && (
                    (
                        this.Tail.IsLessThan(this.Tail.AddressFamily.MaxIPAddress()) // prevent overflow
                        && this.Tail.Increment().Equals(addressRange.Head)
                    ) // this tail appears directly before that head
                    || (
                        addressRange.Tail.IsLessThan(addressRange.Tail.AddressFamily.MaxIPAddress()) // prevent overflow
                        && addressRange.Tail.Increment().Equals(this.Head)
                    )
                ); // that tail appears directly before this head
        }
        #endregion // end: Overlap and Touches

        #endregion // end: Set Operations

        #region Contains Any/All Public/Private Addresses

        /// <inheritdoc/>
        /// <remarks>
        ///     <para>
        ///         Private address blocks are defined in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc1918#section-3">RFC 1918 §3</see>.
        ///         Uses range-overlap detection: returns <see langword="true" /> if the interval
        ///         <c>[Head, Tail]</c> intersects the interval <c>[subnet.Head, subnet.Tail]</c> for
        ///         any entry in <see cref="SubnetUtilities.PrivateIPAddressRangesList" />.
        ///         This correctly handles ranges whose endpoints are both outside a private block but
        ///         whose interior spans it (e.g., <c>11.0.0.0 – 173.0.0.0</c> spans <c>172.16.0.0/12</c>).
        ///     </para>
        /// </remarks>
        public bool ContainsAnyPrivateAddresses()
        {
            return SubnetUtilities.PrivateIPAddressRangesList.Any(subnet =>
                this.Head.IsLessThanOrEqualTo(subnet.Tail) && this.Tail.IsGreaterThanOrEqualTo(subnet.Head)
            );
        }

        /// <inheritdoc/>
        /// <remarks>
        ///     <para>
        ///         Private address blocks are defined in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc1918#section-3">RFC 1918 §3</see>.
        ///         Returns <see langword="true" /> if the entire interval <c>[Head, Tail]</c> is
        ///         contained within a single entry in <see cref="SubnetUtilities.PrivateIPAddressRangesList" />.
        ///         Because private subnets are disjoint, a range that spans two private blocks necessarily
        ///         includes a public gap and therefore returns <see langword="false" />.
        ///     </para>
        /// </remarks>
        public bool ContainsAllPrivateAddresses()
        {
            return SubnetUtilities.PrivateIPAddressRangesList.Any(subnet =>
                subnet.Head.IsLessThanOrEqualTo(this.Head) && subnet.Tail.IsGreaterThanOrEqualTo(this.Tail)
            );
        }

        /// <inheritdoc/>
        /// <remarks>
        ///     <para>
        ///         For example, <c>192.168.0.1 – 192.168.255.255</c> returns <see langword="false" />
        ///         (wholly inside a single private block), while <c>10.0.0.0 – 11.0.0.0</c> returns
        ///         <see langword="true" /> (the range spans two private blocks with a public gap).
        ///     </para>
        ///     <para>
        ///         Public addresses are those not defined as private in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc1918#section-3">RFC 1918 §3</see>.
        ///         Returns <see langword="true" /> if the interval <c>[Head, Tail]</c> is not entirely
        ///         contained within any single entry in <see cref="SubnetUtilities.PrivateIPAddressRangesList" />.
        ///         Because private subnets are disjoint, any range spanning two private blocks includes a
        ///         public gap, so this method returns <see langword="true" /> in that case.
        ///     </para>
        /// </remarks>
        public bool ContainsAnyPublicAddresses()
        {
            return !SubnetUtilities.PrivateIPAddressRangesList.Any(subnet => subnet.Contains(this));
        }

        /// <inheritdoc/>
        /// <remarks>
        ///     <para>
        ///         Public addresses are those not defined as private in
        ///         <see href="https://www.rfc-editor.org/rfc/rfc1918#section-3">RFC 1918 §3</see>.
        ///         Uses range-overlap detection: returns <see langword="true" /> if the interval
        ///         <c>[Head, Tail]</c> does not intersect any entry in
        ///         <see cref="SubnetUtilities.PrivateIPAddressRangesList" />.
        ///         This correctly handles ranges whose endpoints are both public but whose interior spans
        ///         a private block — such a range returns <see langword="false" />.
        ///     </para>
        /// </remarks>
        public bool ContainsAllPublicAddresses()
        {
            return SubnetUtilities.PrivateIPAddressRangesList.All(subnet =>
                !(this.Head.IsLessThanOrEqualTo(subnet.Tail) && this.Tail.IsGreaterThanOrEqualTo(subnet.Head))
            );
        }

        #endregion // end: Contains Any/All Public/Private Addresses
    }
}
