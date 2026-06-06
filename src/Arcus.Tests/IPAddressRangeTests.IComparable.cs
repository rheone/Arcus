namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="IPAddressRange"/> tests for <see cref="System.IComparable{T}"/>
    /// </content>
    public partial class IPAddressRangeTests
    {
        #region CompareTo / Operators

        /// <summary>
        ///     Gets test data for <see cref="CompareTo_Test" /> and operator tests.
        ///     Covers equal ranges, null right operands, cross-family ordering, and length-based ordering within a family.
        ///     <para>Parameters: expected comparison result (int), left (IPAddressRange), right (IPAddressRange).</para>
        /// </summary>
        /// <value>
        ///     Test data for <see cref="CompareTo_Test" /> and operator tests.
        ///     Covers equal ranges, null right operands, cross-family ordering, and length-based ordering within a family.
        ///     <para>Parameters: expected comparison result (int), left (IPAddressRange), right (IPAddressRange).</para>
        /// </value>
        public static TheoryData<int, IPAddressRange, IPAddressRange> Comparison_Values
        {
            get
            {
                var ipv4Slash16 = Subnet.Parse("192.168.0.0/16");
                var ipv6Slash64 = Subnet.Parse("ab:cd::/64");
                var ipv4Slash20 = Subnet.Parse("192.168.0.0/20");
                var ipv6Slash96 = Subnet.Parse("ab:cd::/96");
                var ipv4All = Subnet.Parse("0.0.0.0/0");
                var ipv6All = Subnet.Parse("::/0");
                var ipv4Single = Subnet.Parse("0.0.0.0/32");
                var ipv6Single = Subnet.Parse("::/128");

                var data = new TheoryData<int, IPAddressRange, IPAddressRange>
                {
                    {
                        0,
                        new IPAddressRange(ipv4Slash16.Head, ipv4Slash16.Tail),
                        new IPAddressRange(ipv4Slash16.Head, ipv4Slash16.Tail)
                    },
                    {
                        0,
                        new IPAddressRange(ipv6Slash64.Head, ipv6Slash64.Tail),
                        new IPAddressRange(ipv6Slash64.Head, ipv6Slash64.Tail)
                    },
                    { 1, new IPAddressRange(ipv4Slash16.Head, ipv4Slash16.Tail), null },
                    { 1, new IPAddressRange(ipv6Slash64.Head, ipv6Slash64.Tail), null },
                    {
                        1,
                        new IPAddressRange(ipv4Slash16.Head, ipv4Slash16.Tail),
                        new IPAddressRange(ipv4Slash20.Head, ipv4Slash20.Tail)
                    },
                    {
                        -1,
                        new IPAddressRange(ipv4Slash20.Head, ipv4Slash20.Tail),
                        new IPAddressRange(ipv4Slash16.Head, ipv4Slash16.Tail)
                    },
                    {
                        1,
                        new IPAddressRange(ipv6Slash64.Head, ipv6Slash64.Tail),
                        new IPAddressRange(ipv6Slash96.Head, ipv6Slash96.Tail)
                    },
                    {
                        -1,
                        new IPAddressRange(ipv6Slash96.Head, ipv6Slash96.Tail),
                        new IPAddressRange(ipv6Slash64.Head, ipv6Slash64.Tail)
                    },
                    { -1, new IPAddressRange(ipv4All.Head, ipv4All.Tail), new IPAddressRange(ipv6All.Head, ipv6All.Tail) },
                    { 1, new IPAddressRange(ipv6All.Head, ipv6All.Tail), new IPAddressRange(ipv4All.Head, ipv4All.Tail) },
                    {
                        -1,
                        new IPAddressRange(ipv4Single.Head, ipv4Single.Tail),
                        new IPAddressRange(ipv6Single.Head, ipv6Single.Tail)
                    },
                    {
                        1,
                        new IPAddressRange(ipv6Single.Head, ipv6Single.Tail),
                        new IPAddressRange(ipv4Single.Head, ipv4Single.Tail)
                    },
                };

                return data;
            }
        }

        /// <summary>Verifies CompareTo returns the expected result for all ordered range pairs.</summary>
        /// <param name="expected">Expected comparison result (-1, 0, or 1).</param>
        /// <param name="left">The left operand range.</param>
        /// <param name="right">The right operand range.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void CompareTo_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.CompareTo(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: CompareTo / Operators
    }
}
