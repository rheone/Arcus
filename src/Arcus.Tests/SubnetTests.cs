using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Arcus;
using Arcus.Tests.XunitSerializers;
using Xunit;
using Xunit.Sdk;
#if NET48   // maintained for .NET 4.8 compatibility
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endif

[assembly: RegisterXunitSerializer(typeof(SubnetXunitSerializer), typeof(Subnet))]
[assembly: RegisterXunitSerializer(typeof(FormatProviderXunitSerializer), typeof(IFormatProvider))]

namespace Arcus.Tests
{
    /// <summary>Unit tests for <see cref="Subnet"/>.</summary>
    public class SubnetTests
    {
        #region Addresses

        /// <summary>Verifies that enumerating a subnet's addresses matches converting it to an array.</summary>
        /// <param name="input">CIDR notation string for the subnet under test.</param>
        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("16.8.14.12/28")]
        [InlineData("16.8.14.12/32")]
        [InlineData("::/128")]
        [InlineData("feed:beef::/120")]
        public void Addresses_Test(string input)
        {
            // Arrange
            var subnet = Subnet.Parse(input);

            // Act
            var hosts = subnet.ToList();

            // Assert
            Assert.Equal(subnet.ToArray(), hosts);
        }

        #endregion // end: Addresses

        #region Class

        /// <summary>Verifies that <see cref="Subnet"/> is assignable to the expected base types and interfaces.</summary>
        /// <param name="assignableFromType">The type that <see cref="Subnet"/> should be assignable to.</param>
        [Theory]
        [InlineData(typeof(AbstractIPAddressRange))]
        [InlineData(typeof(IEquatable<Subnet>))]
        [InlineData(typeof(IComparable<Subnet>))]
        [InlineData(typeof(IComparable))]
#if NET48
        [InlineData(typeof(ISerializable))]
#endif
        public void Assignability_Test(Type assignableFromType)
        {
            // Arrange
            var type = typeof(Subnet);

            // Act
            var isAssignableFrom = assignableFromType.IsAssignableFrom(type);

            // Assert
            Assert.True(isAssignableFrom);
        }

        #endregion // end: Class

        #region CompareTo / Operators

        /// <summary>Gets theory data for comparison operator and <see cref="Subnet.CompareTo(Subnet)"/> tests.</summary>
        /// <returns>Parameters: expected comparison sign (int), left subnet (<see cref="Subnet"/>), right subnet (<see cref="Subnet"/>).</returns>
        public static TheoryData<int, Subnet, Subnet> Comparison_Values()
        {
            return new TheoryData<int, Subnet, Subnet>
            {
                { 0, Subnet.Parse("192.168.0.0/16"), Subnet.Parse("192.168.0.0/16") },
                { 0, Subnet.Parse("ab:cd::/64"), Subnet.Parse("ab:cd::/64") },
                { 1, Subnet.Parse("192.168.0.0/16"), null },
                { 1, Subnet.Parse("ab:cd::/64"), null },
                { 1, Subnet.Parse("192.168.0.0/16"), Subnet.Parse("192.168.0.0/20") },
                { -1, Subnet.Parse("192.168.0.0/20"), Subnet.Parse("192.168.0.0/16") },
                { 1, Subnet.Parse("ab:cd::/64"), Subnet.Parse("ab:cd::/96") },
                { -1, Subnet.Parse("ab:cd::/96"), Subnet.Parse("ab:cd::/64") },
                { -1, Subnet.Parse("0.0.0.0/0"), Subnet.Parse("::/0") },
                { 1, Subnet.Parse("::/0"), Subnet.Parse("0.0.0.0/0") },
                { -1, Subnet.Parse("0.0.0.0/32"), Subnet.Parse("::/128") },
                { 1, Subnet.Parse("::/128"), Subnet.Parse("0.0.0.0/32") },
            };
        }

        /// <summary>Verifies that <see cref="Subnet.CompareTo(Subnet)"/> returns a value whose sign matches the expected ordering.</summary>
        /// <param name="expected">Expected sign of the comparison result.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void CompareTo_Subnet_ReturnsExpectedSign_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left.CompareTo(right);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.CompareTo(object)"/> throws <see cref="ArgumentException"/> when the argument is not a <see cref="Subnet"/>.</summary>
        [Fact]
        public void CompareTo_Object_NonSubnet_Throws_ArgumentException_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act / Assert
            Assert.Throws<ArgumentException>(() => subnet.CompareTo("not a subnet"));
        }

        /// <summary>Verifies that <see cref="Subnet.CompareTo(object)"/> returns a positive value when compared to null.</summary>
        [Fact]
        public void CompareTo_Object_Null_ReturnsPositive_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.CompareTo((object)null);

            // Assert
            Assert.Equal(1, result);
        }

        /// <summary>Verifies that <see cref="Subnet.CompareTo(object)"/> returns zero when compared to a boxed equivalent subnet.</summary>
        [Fact]
        public void CompareTo_Object_SubnetBox_ReturnsZero_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");
            object boxed = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.CompareTo(boxed);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>Verifies that the <c>==</c> operator returns the expected equality result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; zero means the subnets are equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_Equals_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left == right;

            // Assert
            Assert.Equal(expected == 0, result);
        }

        /// <summary>Verifies that the <c>!=</c> operator returns the expected inequality result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; non-zero means the subnets are not equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_NotEquals_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left != right;

            // Assert
            Assert.Equal(expected != 0, result);
        }

        /// <summary>Verifies that the <c>&gt;</c> operator returns the expected result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; positive means left is greater.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_GreaterThan_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left > right;

            // Assert
            Assert.Equal(expected > 0, result);
        }

        /// <summary>Verifies that the <c>&gt;=</c> operator returns the expected result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; non-negative means left is greater than or equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_GreaterThanOrEqual_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left >= right;

            // Assert
            Assert.Equal(expected >= 0, result);
        }

        /// <summary>Verifies that the <c>&lt;</c> operator returns the expected result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; negative means left is less.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_LessThan_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left < right;

            // Assert
            Assert.Equal(expected < 0, result);
        }

        /// <summary>Verifies that the <c>&lt;=</c> operator returns the expected result based on comparison sign.</summary>
        /// <param name="expected">Expected comparison sign; non-positive means left is less than or equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_LessThanOrEqual_Test(int expected, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var result = left <= right;

            // Assert
            Assert.Equal(expected <= 0, result);
        }

        /// <summary>Verifies that the <c>&lt;</c> operator returns <see langword="true"/> when the left operand is null and the right is non-null.</summary>
        [Fact]
        public void Operator_LessThan_NullLeft_NonNullRight_ReturnsTrue_Test()
        {
            // Arrange
            Subnet left = null;
            var right = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = left < right;

            // Assert
            Assert.True(result);
        }

        /// <summary>Verifies that the <c>&lt;</c> operator returns <see langword="false"/> when both operands are null.</summary>
        [Fact]
        public void Operator_LessThan_BothNull_ReturnsFalse_Test()
        {
            // Arrange
            Subnet left = null;
            Subnet right = null;

            // Act
            var result = left < right;

            // Assert
            Assert.False(result);
        }

        /// <summary>Verifies that the <c>&gt;</c> operator returns <see langword="false"/> when the left operand is null.</summary>
        [Fact]
        public void Operator_GreaterThan_NullLeft_ReturnsFalse_Test()
        {
            // Arrange
            Subnet left = null;
            var right = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = left > right;

            // Assert
            Assert.False(result);
        }

        #endregion // end: CompareTo / Operators

        #region Netmask

        /// <summary>Verifies that <see cref="Subnet.Netmask"/> is null for an IPv6 subnet.</summary>
        [Fact]
        public void Netmask_Null_ForIPv6_Test()
        {
            // Arrange
            var subnet = new Subnet(IPAddress.IPv6Any, 32);

            // Act
            var netmask = subnet.Netmask;

            // Assert
            Assert.Null(netmask);
        }

        /// <summary>Verifies that <see cref="Subnet.Netmask"/> is non-null and correct for an IPv4 subnet.</summary>
        [Fact]
        public void Netmask_NotNull_ForIPv4_Test()
        {
            // Arrange
            var subnet = new Subnet(IPAddress.Parse("192.168.1.0"), 24);

            // Act
            var netmask = subnet.Netmask;

            // Assert
            Assert.NotNull(netmask);
            Assert.Equal(IPAddress.Parse("255.255.255.0"), netmask);
        }

        #endregion // end: Netmask

        #region NetworkPrefixAddress / BroadcastAddress

        /// <summary>Verifies that <see cref="Subnet.NetworkPrefixAddress"/> equals the subnet's <see cref="AbstractIPAddressRange.Head"/>.</summary>
        [Fact]
        public void NetworkPrefixAddress_EqualsHead_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.1.0/24");

            // Act
            var networkPrefix = subnet.NetworkPrefixAddress;

            // Assert
            Assert.Equal(subnet.Head, networkPrefix);
            Assert.Equal(IPAddress.Parse("192.168.1.0"), networkPrefix);
        }

        /// <summary>Verifies that <see cref="Subnet.BroadcastAddress"/> equals the subnet's <see cref="AbstractIPAddressRange.Tail"/>.</summary>
        [Fact]
        public void BroadcastAddress_EqualsTail_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.1.0/24");

            // Act
            var broadcast = subnet.BroadcastAddress;

            // Assert
            Assert.Equal(subnet.Tail, broadcast);
            Assert.Equal(IPAddress.Parse("192.168.1.255"), broadcast);
        }

        #endregion // end: NetworkPrefixAddress / BroadcastAddress

        #region Overlaps

        /// <summary>Verifies that <see cref="Subnet.Overlaps(Subnet)"/> returns the expected result for various subnet pairs.</summary>
        /// <param name="expected">Expected overlap result.</param>
        /// <param name="subnetAString">CIDR string for the first subnet.</param>
        /// <param name="subnetBString">CIDR string for the second subnet, or null.</param>
        [Theory]
        [InlineData(true, "0.0.0.0/0", "0.0.0.0/0")]
        [InlineData(true, "::/0", "::/0")]
        [InlineData(true, "0.0.0.0/0", "255.255.0.0/16")]
        [InlineData(true, "255.255.0.0/16", "0.0.0.0/0")]
        [InlineData(true, "::/0", "abcd:ef01::/64")]
        [InlineData(true, "abcd:ef01::/64", "::/0")]
        [InlineData(false, "0.0.0.0/0", null)]
        [InlineData(false, "::/0", null)]
        [InlineData(false, "0.0.0.0/0", "::/0")]
        [InlineData(false, "::/0", "0.0.0.0/0")]
        public void Overlaps_Test(bool expected, string subnetAString, string subnetBString)
        {
            // Arrange
            var subnetA = Subnet.Parse(subnetAString);
            _ = Subnet.TryParse(subnetBString, out var subnetB);

            // Act
            var result = subnetA.Overlaps(subnetB);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.Overlaps(IIPAddressRange)"/> is symmetric when a range is wholly contained within a subnet.</summary>
        [Fact]
        public void Overlaps_WhollyContainedIIPAddressRange_IsSymmetric_Test()
        {
            // When a range is wholly contained inside a subnet, both directions must return true.
            // This exercises AbstractIPAddressRange.Overlaps(IIPAddressRange) — the Subnet-typed
            // overload already handled this correctly; the base-class path had an asymmetry bug.
            var outer = Subnet.Parse("192.168.0.0/16");
            var inner = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255"));

            Assert.True(outer.Overlaps((IIPAddressRange)inner), "outer.Overlaps(inner) should be true");
            Assert.True(inner.Overlaps((IIPAddressRange)outer), "inner.Overlaps(outer) should be true — symmetry");
        }

        /// <summary>Verifies that the subnet-typed <see cref="Subnet.Overlaps(Subnet)"/> overload is symmetric when one subnet is wholly inside the other.</summary>
        [Fact]
        public void Overlaps_WhollyContainedSubnet_IsSymmetric_Test()
        {
            // Subnet-typed overload: both directions must return true when one subnet is inside the other.
            var outer = Subnet.Parse("10.0.0.0/8");
            var inner = Subnet.Parse("10.10.0.0/16");

            Assert.True(outer.Overlaps(inner), "outer.Overlaps(inner) should be true");
            Assert.True(inner.Overlaps(outer), "inner.Overlaps(outer) should be true — symmetry");
        }

        #endregion // end: Overlaps

        #region ToString

        /// <summary>Verifies that <see cref="Subnet.ToString(string, System.IFormatProvider)"/> returns the canonical CIDR representation.</summary>
        /// <param name="expected">Expected canonical CIDR string.</param>
        /// <param name="input">Input CIDR string to parse.</param>
        [Theory]
        [InlineData("192.168.1.1/32", "192.168.1.1/32")]
        [InlineData("192.168.0.0/16", "192.168.1.1/16")]
        [InlineData("0.0.0.0/0", "192.168.1.1/0")]
        [InlineData("::/128", "::/128")]
        [InlineData("::/64", "::/64")]
        [InlineData("::/0", "::/0")]
        public void ToString_Test(string expected, string input)
        {
            // Arrange
            // Act
            var result = Subnet.Parse(input).ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: ToString

        #region UsableHostAddressCount

        /// <summary>Gets theory data for <see cref="UsableHostAddressCount_Test"/>.</summary>
        /// <returns>Parameters: expected usable host address count (<see cref="BigInteger"/>), subnet under test (<see cref="Subnet"/>).</returns>
        public static TheoryData<BigInteger, Subnet> UsableHostAddressCount_Test_Values()
        {
            var data = new TheoryData<BigInteger, Subnet>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var routePrefix = 32 - i;
                    var count = routePrefix < 2 ? BigInteger.Zero : BigInteger.Subtract(BigInteger.Pow(2, routePrefix), 2);

                    var subnet = new Subnet(ipAddress, i);
                    data.Add(count, subnet);
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var routePrefix = 128 - i;
                    var count = routePrefix < 2 ? BigInteger.Zero : BigInteger.Subtract(BigInteger.Pow(2, routePrefix), 2);

                    var subnet = new Subnet(ipAddress, i);
                    data.Add(count, subnet);
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.UsableHostAddressCount"/> returns the expected count for a given subnet.</summary>
        /// <param name="expected">Expected usable host address count.</param>
        /// <param name="subnet">The subnet under test.</param>
        [Theory]
        [MemberData(nameof(UsableHostAddressCount_Test_Values))]
        public void UsableHostAddressCount_Test(BigInteger expected, Subnet subnet)
        {
            // Arrange

            // Act
            var result = subnet.UsableHostAddressCount;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.UsableHostAddressCount"/> returns zero for a single-host /32 subnet.</summary>
        [Fact]
        public void UsableHostAddressCount_SingleHost_ReturnsZero_Test()
        {
            // Arrange
            var subnet = new Subnet(IPAddress.Parse("10.0.0.1"), 32);

            // Act
            var result = subnet.UsableHostAddressCount;

            // Assert
            Assert.Equal(BigInteger.Zero, result);
        }

        /// <summary>Verifies that <see cref="Subnet.UsableHostAddressCount"/> returns the total length minus two for a /0 subnet.</summary>
        [Fact]
        public void UsableHostAddressCount_SlashZero_ReturnsLengthMinusTwo_Test()
        {
            // Arrange
            var subnet = new Subnet(IPAddress.Any, 0);

            // Act
            var result = subnet.UsableHostAddressCount;

            // Assert
            Assert.Equal(BigInteger.Pow(2, 32) - 2, result);
        }

        #endregion // end: UsableHostAddressCount

        #region Contains

        #region Contains(IPAddress)

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.Contains(System.Net.IPAddress)"/> returns the expected result for various address inputs.</summary>
        /// <param name="expected">Expected result.</param>
        /// <param name="subnetString">CIDR string for the subnet under test.</param>
        /// <param name="containsIPAddressString">The IP address string to test containment, or null.</param>
        [Theory]
        [InlineData(true, "192.168.0.0/16", "192.168.0.0")]
        [InlineData(true, "192.168.0.0/16", "192.168.0.16")]
        [InlineData(false, "192.168.0.0/16", "192.255.0.0")]
        [InlineData(false, "0.0.0.0/0", null)]
        [InlineData(false, "0.0.0.0/0", "::")]
        [InlineData(true, "::/0", "::")]
        [InlineData(true, "2001:0db8:85a3:0042::/64", "2001:0db8:85a3:0042:1000:8a2e:0370:7334")]
        [InlineData(false, "2001:0db8:85a3:0042::/64", "2007:0db8:85a3::abc")]
        [InlineData(false, "::/0", null)]
        [InlineData(false, "::/0", "192.168.1.1")]
        public void Contains_IPAddress_Test(bool expected, string subnetString, string containsIPAddressString)
        {
            // Arrange
            var subnet = Subnet.Parse(subnetString);
            var containsIPAddress = containsIPAddressString != null ? IPAddress.Parse(containsIPAddressString) : null;

            // Act
            var result = subnet.Contains(containsIPAddress);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Contains(IPAddress)

        #region Contains(Subnet)

        /// <summary>Verifies that <see cref="Subnet.Contains(Subnet)"/> returns the expected result for various subnet inputs.</summary>
        /// <param name="expected">Expected result.</param>
        /// <param name="subnetString">CIDR string for the outer subnet.</param>
        /// <param name="containsSubnetString">CIDR string for the inner subnet to test, or null.</param>
        [Theory]
        [InlineData(true, "192.168.0.0/16", "192.168.0.0/16")]
        [InlineData(true, "192.168.0.0/16", "192.168.0.0/32")]
        [InlineData(false, "192.168.0.0/16", "192.168.0.0/8")]
        [InlineData(false, "192.168.0.0/16", null)]
        public void Contains_Subnet_Test(bool expected, string subnetString, string containsSubnetString)
        {
            // Arrange
            var subnet = Subnet.Parse(subnetString);
            var containsSubnet = containsSubnetString != null ? Subnet.Parse(containsSubnetString) : null;

            // Act
            var result = subnet.Contains(containsSubnet);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that an IPv4 subnet does not contain an IPv6 address.</summary>
        [Fact]
        public void Contains_IPAddress_IPv4SubnetDoesNotContainIPv6_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("0.0.0.0/0");

            // Act
            var result = subnet.Contains(IPAddress.Parse("::"));

            // Assert
            Assert.False(result);
        }

        #endregion // end: Contains(Subnet)

        #endregion // end: Contains

        #region TryIPv4FromPartial

        /// <summary>Gets theory data for <see cref="TryIPv4FromPartial_Test"/>.</summary>
        /// <returns>Parameters: expected subnet result (<see cref="Subnet"/> or null), partial IPv4 input string.</returns>
        public static TheoryData<Subnet, string> TryIPv4FromPartial_Test_Values()
        {
            return new TheoryData<Subnet, string>
            {
                { null, null },
                { null, string.Empty },
                { null, "potato" },
                { Subnet.Parse("192.0.0.0/8"), "192" },
                { Subnet.Parse("192.0.0.0/8"), "192." },
                { Subnet.Parse("192.168.0.0/16"), "192.168" },
                { Subnet.Parse("192.168.0.0/16"), "192.168." },
                { Subnet.Parse("192.168.1.0/24"), "192.168.1" },
                { Subnet.Parse("192.168.1.0/24"), "192.168.1." },
                { Subnet.Parse("192.168.1.1/32"), "192.168.1.1" },
                { null, "192.168.1.1." },
                { null, "192.168.0.1.5" },
                // Addresses #59 - TryIPv4FromPartial throws FormatException for 0-prefixed octet not valid as octal
                { null, "10.209.005.029" },
            };
        }

        /// <summary>Verifies that <see cref="Subnet.TryIPv4FromPartial"/> returns the expected subnet for a given partial IPv4 string.</summary>
        /// <param name="expected">Expected subnet, or null when the parse should fail.</param>
        /// <param name="input">Partial IPv4 address string to parse.</param>
        [Theory]
        [MemberData(nameof(TryIPv4FromPartial_Test_Values))]
        public void TryIPv4FromPartial_Test(Subnet expected, string input)
        {
            // Arrange

            // Act
            var success = Subnet.TryIPv4FromPartial(input, out var subnet);

            // Assert
            Assert.Equal(expected != null, success);
            Assert.Equal(expected, subnet);
        }

        #endregion // end: TryIPv4FromPartial

        #region Ipv4OctetPartialPattern

        /// <summary>Gets theory data for <see cref="Ipv4OctetPartialPattern_IsMatch_ReturnsExpected_Test"/>.</summary>
        /// <value>Parameters: expected match result (bool), input string to test against the pattern.</value>
        public static TheoryData<bool, string> Ipv4OctetPartialPattern_Test_Data =>
            new()
            {
                // single octet — valid range
                { true, "0" },
                { true, "1" },
                { true, "192" },
                { true, "255" },
                // two octets
                { true, "192.168" },
                { true, "10.0" },
                // three octets
                { true, "192.168.1" },
                { true, "10.0.0" },
                // four octets — full addresses
                { true, "192.168.1.1" },
                { true, "0.0.0.0" },
                { true, "255.255.255.255" },
                // trailing dot (partial entry)
                { true, "192." },
                { true, "192.168." },
                { true, "192.168.1." },
                // non-matching — empty
                { false, string.Empty },
                // non-matching — octet out of range
                { false, "256" },
                { false, "999" },
                // non-matching — five groups
                { false, "192.168.0.1.5" },
                // non-matching — non-IPv4
                { false, "::" },
                { false, "potato" },
            };

        /// <summary>Verifies that <see cref="Subnet.Ipv4OctetPartialPattern"/> matches the expected inputs.</summary>
        /// <param name="expected">Whether the pattern should match the input.</param>
        /// <param name="input">The string to test against the pattern.</param>
        [Theory]
        [MemberData(nameof(Ipv4OctetPartialPattern_Test_Data))]
        public void Ipv4OctetPartialPattern_IsMatch_ReturnsExpected_Test(bool expected, string input)
        {
            // Arrange
            var regex = new Regex(Subnet.Ipv4OctetPartialPattern, RegexOptions.CultureInvariant);

            // Act
            var result = regex.IsMatch(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Ipv4OctetPartialPattern

        #region TryIPv6FromPartial

        /// <summary>Gets theory data for <see cref="TryIPv6FromPartial_Test"/>.</summary>
        /// <returns>Parameters: expected subnets (<see cref="IEnumerable{Subnet}"/>), partial IPv6 input string.</returns>
        public static TheoryData<IEnumerable<Subnet>, string> TryIPv6FromPartial_Test_Values()
        {
            var data = new TheoryData<IEnumerable<Subnet>, string>();

            // bad input formats
            data.Add(Enumerable.Empty<Subnet>(), null);
            data.Add(Enumerable.Empty<Subnet>(), string.Empty);
            data.Add(Enumerable.Empty<Subnet>(), "potato");

            // invalid input
            data.Add(Enumerable.Empty<Subnet>(), ":");
            data.Add(Enumerable.Empty<Subnet>(), ":::");
            data.Add(Enumerable.Empty<Subnet>(), "0:0:0:0:0:0:0:0:0"); // too many hextets
            data.Add(Enumerable.Empty<Subnet>(), "0:0:0:0:0:0:0:0::");

            // invalid input, multiple "::"
            data.Add(Enumerable.Empty<Subnet>(), "::0::");
            data.Add(Enumerable.Empty<Subnet>(), "0::0::0");
            data.Add(Enumerable.Empty<Subnet>(), "::0:0:0::");

            // explicit valid subnets
            data.Add(new[] { Subnet.Parse("::/128") }, "::/128");
            data.Add(new[] { Subnet.Parse("2001:db8:85a3:42::/64") }, "2001:db8:85a3:42::/64");
            data.Add(
                new[] { Subnet.Parse("2001:0db8:85a3:0042:1000:0001:0370:7334/128") },
                "2001:0db8:85a3:0042:1000:0001:0370:7334/128"
            );

            for (var hextetCount = 0; hextetCount <= 8; hextetCount++)
            {
                var sb = new StringBuilder();

                sb.Append(string.Join(":", Enumerable.Repeat("0", hextetCount)));

                if (hextetCount < 8)
                {
                    sb.Append("::");
                }

                var subnets = new List<Subnet>();
                for (var i = 0; i <= 8 - hextetCount; i++)
                {
                    subnets.Add(Subnet.Parse($"::/{128 - (16 * i)}"));
                }

                data.Add(subnets, sb.ToString());
            }

            var hextets = "2001:0db8:85a3:0042:1000:0001:0370:7334".Split(':');

            for (var hextetCount = 1; hextetCount <= 8; hextetCount++)
            {
                var subnets = new List<Subnet>();
                for (var i = 8 - hextetCount; i >= 0; i--)
                {
                    var enumerable = hextets.Take(hextetCount).Select(s => new string([.. s.SkipWhile(c => c == '0')]));
                    var trimmedLeadingZero = string.Join(":", enumerable);
                    var subnet =
                        hextetCount < 8
                            ? Subnet.Parse($"{trimmedLeadingZero}::/{128 - (16 * i)}")
                            : Subnet.Parse($"{trimmedLeadingZero}");

                    subnets.Add(subnet);
                }

                var inputString = string.Join(":", hextets.Take(hextetCount));

                if (hextetCount < 8)
                {
                    data.Add(subnets, $"{inputString}::");
                    data.Add(subnets, $"{inputString}:");
                }

                // A bare hextet without any ':' is not treated as a valid partial IPv6 address
                if (!string.IsNullOrEmpty(inputString) && inputString != hextets[0])
                {
                    data.Add(subnets, inputString);
                }
            }

            return data;
        }

        /// <summary>Verifies that <see cref="Subnet.TryIPv6FromPartial"/> returns the expected subnets for a given partial IPv6 string.</summary>
        /// <param name="expected">Expected subnets, or an empty enumerable when the parse should fail.</param>
        /// <param name="input">Partial IPv6 address string to parse.</param>
        [Theory]
        [MemberData(nameof(TryIPv6FromPartial_Test_Values))]
        public void TryIPv6FromPartial_Test(IEnumerable<Subnet> expected, string input)
        {
            // Arrange
            // Act
#pragma warning disable CS0618 // testing a known obsolete method; ignoring the fact that it is obsolete
            var success = Subnet.TryIPv6FromPartial(input, out var subnets);
#pragma warning restore CS0618

            // Assert
            var expectedList = expected.ToList();
            var subnetList = subnets.ToList();

            Assert.Equal(expectedList.Any(), success);
            Assert.Equal(expectedList.Count, subnetList.Count);
            Assert.All(subnetList, subnet => Assert.Contains(subnet, expectedList));
        }

        #endregion // end: TryIPv6FromPartial

        #region Ctor

        #region Ctor(IPAddress)

        /// <summary>Gets theory data for <see cref="Ctor_IPAddress_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), address to construct with (<see cref="IPAddress"/>).</returns>
        public static TheoryData<Subnet, IPAddress> Ctor_IPAddress_Test_Values()
        {
            var data = new TheoryData<Subnet, IPAddress>();

            foreach (var ipAddress in IPv4Addresses())
            {
                var subnet = new Subnet(ipAddress, 32);
                data.Add(subnet, ipAddress);
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                var subnet = new Subnet(ipAddress, 128);
                data.Add(subnet, ipAddress);
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> from a single <see cref="IPAddress"/> produces the expected host subnet.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="address">The IP address to construct from.</param>
        [Theory]
        [MemberData(nameof(Ctor_IPAddress_Test_Values))]
        public void Ctor_IPAddress_Test(Subnet expected, IPAddress address)
        {
            // Arrange
            // Act
            var subnet = new Subnet(address);

            // Assert
            Assert.Equal(expected, subnet);
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with a null <see cref="IPAddress"/> throws <see cref="ArgumentNullException"/>.</summary>
        [Fact]
        public void Ctor_IPAddress_NullIPAddress_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => new Subnet((IPAddress)null));
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> from an IPv4 address sets routing prefix to 32.</summary>
        [Fact]
        public void Ctor_IPAddress_SetsRoutingPrefix32_ForIPv4_Test()
        {
            // Arrange
            var address = IPAddress.Parse("10.0.0.1");

            // Act
            var subnet = new Subnet(address);

            // Assert
            Assert.Equal(32, subnet.RoutingPrefix);
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> from an IPv6 address sets routing prefix to 128.</summary>
        [Fact]
        public void Ctor_IPAddress_SetsRoutingPrefix128_ForIPv6_Test()
        {
            // Arrange
            var address = IPAddress.IPv6Loopback;

            // Act
            var subnet = new Subnet(address);

            // Assert
            Assert.Equal(128, subnet.RoutingPrefix);
        }

        #endregion // end: Ctor(IPAddress)

        #region Ctor(IPAddress, int)

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with a null address and routing prefix throws <see cref="ArgumentNullException"/>.</summary>
        [Fact]
        public void Ctor_IPAddress_Int_NullIPAddress_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => new Subnet(null, 42));
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with an out-of-range routing prefix throws <see cref="ArgumentOutOfRangeException"/>.</summary>
        /// <param name="address">IP address string.</param>
        /// <param name="routingPrefix">The out-of-range routing prefix.</param>
        [Theory]
        [InlineData("192.168.1.1", -1)]
        [InlineData("192.168.1.1", 33)]
        [InlineData("::", -1)]
        [InlineData("::", 129)]
        public void Ctor_IPAddress_Int_IntOutOfRange_Throws_ArgumentOutOfRangeException_Test(string address, int routingPrefix)
        {
            // Arrange
            var ipAddress = IPAddress.Parse(address);

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new Subnet(ipAddress, routingPrefix));
        }

        #endregion // end: Ctor(IPAddress, int)

        #region Ctor(IPAddress, IPAddress)

        /// <summary>Gets theory data for <see cref="Ctor_IPAddress_IPAddress_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), primary address (<see cref="IPAddress"/>), secondary address (<see cref="IPAddress"/>).</returns>
        public static TheoryData<Subnet, IPAddress, IPAddress> Ctor_IPAddress_IPAddress_Test_Values()
        {
            var data = new TheoryData<Subnet, IPAddress, IPAddress>();
            var seen = new HashSet<string>();

            foreach (var address in IPv4Addresses())
            {
                for (var routePrefix = 0; routePrefix <= 32; routePrefix++)
                {
                    var subnet = new Subnet(address, routePrefix);
                    var key = $"{subnet.ToString("f", null)}|{subnet.Head}|{subnet.Tail}";
                    if (seen.Add(key))
                    {
                        data.Add(subnet, subnet.Head, subnet.Tail);
                    }
                }
            }

            foreach (var address in IPv6Addresses())
            {
                for (var routePrefix = 0; routePrefix <= 128; routePrefix++)
                {
                    var subnet = new Subnet(address, routePrefix);
                    var key = $"{subnet.ToString("f", null)}|{subnet.Head}|{subnet.Tail}";
                    if (seen.Add(key))
                    {
                        data.Add(subnet, subnet.Head, subnet.Tail);
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> from two <see cref="IPAddress"/> bounds produces the expected subnet.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="primary">The low address bound.</param>
        /// <param name="secondary">The high address bound.</param>
        [Theory]
        [MemberData(nameof(Ctor_IPAddress_IPAddress_Test_Values))]
        public void Ctor_IPAddress_IPAddress_Test(Subnet expected, IPAddress primary, IPAddress secondary)
        {
            // Arrange
            // Act
            var result = new Subnet(primary, secondary);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with a null address argument throws <see cref="ArgumentNullException"/>.</summary>
        /// <param name="primary">Primary address string, or null.</param>
        /// <param name="secondary">Secondary address string, or null.</param>
        [Theory]
        [InlineData("192.168.3.25", null)]
        [InlineData(null, "192.168.3.25")]
        [InlineData("::", null)]
        [InlineData(null, "::")]
        [InlineData(null, null)]
        public void Ctor_IPAddress_IPAddress_Null_Input_Throws_ArgumentNullException_Test(string primary, string secondary)
        {
            // Arrange
            _ = IPAddress.TryParse(primary, out var primaryAddress);
            _ = IPAddress.TryParse(secondary, out var secondaryAddress);

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => new Subnet(primaryAddress, secondaryAddress));
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with an invalid address ordering throws <see cref="InvalidOperationException"/>.</summary>
        /// <param name="primary">The higher (invalid low) address string.</param>
        /// <param name="secondary">The lower (invalid high) address string.</param>
        [Theory]
        [InlineData("192.168.3.25", "192.168.3.0")]
        [InlineData("ff::dc", "::")]
        public void Ctor_IPAddress_IPAddress_Input_Invalid_Ordering_Throws_InvalidOperationException_Test(
            string primary,
            string secondary
        )
        {
            // Arrange
            var primaryAddress = IPAddress.Parse(primary);
            var secondaryAddress = IPAddress.Parse(secondary);

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => new Subnet(primaryAddress, secondaryAddress));
        }

        /// <summary>Verifies that constructing a <see cref="Subnet"/> with mismatched address families throws <see cref="ArgumentException"/>.</summary>
        /// <param name="primary">First address string.</param>
        /// <param name="secondary">Second address string of a different family.</param>
        [Theory]
        [InlineData("192.168.3.25", "2001:0db8:85a3:0042:1000:8a2e:0370:7334")]
        [InlineData("2001:0db8:85a3:0042:1000:8a2e:0370:7334", "192.168.3.25")]
        public void Ctor_IPAddress_IPAddress_MismatchAddressFamily_Throws_ArgumentException_Test(
            string primary,
            string secondary
        )
        {
            // Arrange
            var primaryAddress = IPAddress.Parse(primary);
            var secondaryAddress = IPAddress.Parse(secondary);

            // Act / Assert
            Assert.Throws<ArgumentException>(() => new Subnet(primaryAddress, secondaryAddress));
        }

        #endregion // end: Ctor(IPAddress, IPAddress)

        #endregion // end: Ctor

        #region ISerializable
#if NET48   // maintained for .NET 4.8 compatibility
        /// <summary>Gets theory data for <see cref="CanSerializable_Test"/>.</summary>
        /// <returns>Parameters: subnet to serialize (<see cref="Subnet"/>).</returns>
        public static TheoryData<Subnet> CanSerializable_Test_Values()
        {
            return new TheoryData<Subnet>
            {
                { new Subnet(IPAddress.Parse("192.168.1.0")) },
                { new Subnet(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")) },
                { new Subnet(IPAddress.Parse("::"), IPAddress.Parse("::FFFF")) },
            };
        }

        /// <summary>Verifies that a <see cref="Subnet"/> can be round-trip serialized and deserialized using <see cref="BinaryFormatter"/>.</summary>
        /// <param name="subnet">The subnet to serialize.</param>
        [Theory]
        [MemberData(nameof(CanSerializable_Test_Values))]
        public void CanSerializable_Test(Subnet subnet)
        {
            // Arrange
            var formatter = new BinaryFormatter();

            // Act
            using (var writeStream = new MemoryStream())
            {
                formatter.Serialize(writeStream, subnet);
                writeStream.Seek(0, SeekOrigin.Begin);

                // Deserialize the object from the stream
                var result = formatter.Deserialize(writeStream);

                // Assert
                var actual = Assert.IsType<Subnet>(result);

                // using explicit EqualityComparer to avoid comparing elements of enumerable
                Assert.Equal(subnet, actual, SubnetEqualityComparer.Instance);
            }
        }
#endif
        #endregion // end: ISerializable

        #region Static Factory Methods

        #region FromBytes

        /// <summary>Gets theory data for <see cref="FromBytes_Bytes_Bytes_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), low address bytes (<see cref="T:byte[]"/>), high address bytes (<see cref="T:byte[]"/>).</returns>
        public static TheoryData<Subnet, byte[], byte[]> FromBytes_Bytes_Bytes_Test_Values()
        {
            var data = new TheoryData<Subnet, byte[], byte[]>();
            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var key =
                        $"{BitConverter.ToString(subnet.Head.GetAddressBytes())}|{BitConverter.ToString(subnet.Tail.GetAddressBytes())}";
                    if (seen.Add(key))
                    {
                        data.Add(subnet, subnet.Head.GetAddressBytes(), subnet.Tail.GetAddressBytes());
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var key =
                        $"{BitConverter.ToString(subnet.Head.GetAddressBytes())}|{BitConverter.ToString(subnet.Tail.GetAddressBytes())}";
                    if (seen.Add(key))
                    {
                        data.Add(subnet, subnet.Head.GetAddressBytes(), subnet.Tail.GetAddressBytes());
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.FromBytes(byte[], byte[])"/> produces the expected subnet from address byte arrays.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="lowAddressBytes">Byte array for the low address.</param>
        /// <param name="highAddressBytes">Byte array for the high address.</param>
        [Theory]
        [MemberData(nameof(FromBytes_Bytes_Bytes_Test_Values))]
        public void FromBytes_Bytes_Bytes_Test(Subnet expected, byte[] lowAddressBytes, byte[] highAddressBytes)
        {
            // Arrange
            // Act
            var subnet = Subnet.FromBytes(lowAddressBytes, highAddressBytes);

            // Assert
            Assert.Equal(expected, subnet);
        }

        /// <summary>Verifies that <see cref="Subnet.FromBytes(byte[], byte[])"/> throws <see cref="ArgumentNullException"/> for null or empty byte arrays.</summary>
        /// <param name="lowAddressBytes">Low address byte array, or null.</param>
        /// <param name="highAddressBytes">High address byte array, or null.</param>
        [Theory]
        [InlineData(null, new byte[] { 0x01, 0x01, 0xA8, 0xC0 })]
        [InlineData(new byte[] { 0x01, 0x01, 0xA8, 0xC0 }, null)]
        [InlineData(null, new byte[] { })]
        public void FromBytes_Null_Input_Throws_ArgumentNullException_Test(byte[] lowAddressBytes, byte[] highAddressBytes)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.FromBytes(lowAddressBytes, highAddressBytes));
        }

        /// <summary>Verifies that <see cref="Subnet.FromBytes(byte[], byte[])"/> throws <see cref="ArgumentException"/> for invalid byte array lengths.</summary>
        /// <param name="lowAddressBytes">Low address byte array with invalid length.</param>
        /// <param name="highAddressBytes">High address byte array with invalid length.</param>
        [Theory]
        [InlineData(new byte[] { 0x01, 0x01, 0xA8, 0xC0, 0xFF }, new byte[] { 0x01, 0x01, 0xA8, 0xC0 })]
        [InlineData(new byte[] { 0x01, 0x01, 0xA8, 0xC0 }, new byte[] { 0x01, 0x01, 0xA8, 0xC0, 0xFF })]
        [InlineData(new byte[] { }, new byte[] { 0x01, 0x01, 0xA8, 0xC0, 0xFF })]
        [InlineData(new byte[] { 0x01, 0x01, 0xA8, 0xC0, 0xFF }, new byte[] { })]
        public void FromBytes_Invalid_Input_Throws_ArgumentException_Test(byte[] lowAddressBytes, byte[] highAddressBytes)
        {
            // Arrange
            // Act
            var exception = Assert.Throws<ArgumentException>(() => Subnet.FromBytes(lowAddressBytes, highAddressBytes));

            // Assert
            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        /// <summary>Verifies that <see cref="Subnet.FromBytes(byte[], byte[])"/> throws <see cref="InvalidOperationException"/> when the high address is lower than the low address.</summary>
        [Fact]
        public void FromBytes_HighAddressLowerThanLowAddress_Throws_InvalidOperationException_Test()
        {
            // Arrange
            var lowBytes = IPAddress.Parse("192.168.2.0").GetAddressBytes();
            var highBytes = IPAddress.Parse("192.168.1.0").GetAddressBytes();

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => Subnet.FromBytes(lowBytes, highBytes));
        }

        #endregion // end: FromBytes

        #region TryFromBytes

        /// <summary>Gets theory data for <see cref="TryFromBytes_Bytes_Bytes_Test"/>.</summary>
        /// <returns>Parameters: expected success (bool), expected subnet (<see cref="Subnet"/> or null), low address bytes (<see cref="T:byte[]"/>), high address bytes (<see cref="T:byte[]"/>).</returns>
        public static TheoryData<bool, Subnet, byte[], byte[]> TryFromBytes_Bytes_Bytes_Test_Values()
        {
            var data = new TheoryData<bool, Subnet, byte[], byte[]>();

            data.Add(false, null, null, null);
            data.Add(false, null, IPAddress.Any.GetAddressBytes(), IPAddress.IPv6Any.GetAddressBytes());
            data.Add(false, null, Array.Empty<byte>(), IPAddress.IPv6Any.GetAddressBytes());
            data.Add(false, null, Array.Empty<byte>(), IPAddress.Any.GetAddressBytes());
            data.Add(false, null, IPAddress.IPv6Any.GetAddressBytes(), Array.Empty<byte>());
            data.Add(false, null, IPAddress.Any.GetAddressBytes(), Array.Empty<byte>());

            data.Add(false, null, new byte[] { 0x00 }, IPAddress.IPv6Any.GetAddressBytes());
            data.Add(false, null, new byte[] { 0x00 }, IPAddress.Any.GetAddressBytes());
            data.Add(false, null, IPAddress.IPv6Any.GetAddressBytes(), new byte[] { 0x00 });
            data.Add(false, null, IPAddress.Any.GetAddressBytes(), new byte[] { 0x00 });

            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var key =
                        $"{BitConverter.ToString(subnet.Head.GetAddressBytes())}|{BitConverter.ToString(subnet.Tail.GetAddressBytes())}";
                    if (seen.Add(key))
                    {
                        data.Add(true, subnet, subnet.Head.GetAddressBytes(), subnet.Tail.GetAddressBytes());
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var key =
                        $"{BitConverter.ToString(subnet.Head.GetAddressBytes())}|{BitConverter.ToString(subnet.Tail.GetAddressBytes())}";
                    if (seen.Add(key))
                    {
                        data.Add(true, subnet, subnet.Head.GetAddressBytes(), subnet.Tail.GetAddressBytes());
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.TryFromBytes(byte[], byte[], out Subnet)"/> returns the expected success flag and subnet.</summary>
        /// <param name="expectedSuccess">Expected return value of the try method.</param>
        /// <param name="expectedSubnet">Expected subnet output, or null on failure.</param>
        /// <param name="lowAddressBytes">Byte array for the low address.</param>
        /// <param name="highAddressBytes">Byte array for the high address.</param>
        [Theory]
        [MemberData(nameof(TryFromBytes_Bytes_Bytes_Test_Values))]
        public void TryFromBytes_Bytes_Bytes_Test(
            bool expectedSuccess,
            Subnet expectedSubnet,
            byte[] lowAddressBytes,
            byte[] highAddressBytes
        )
        {
            // Arrange
            // Act
            var success = Subnet.TryFromBytes(lowAddressBytes, highAddressBytes, out var subnet);

            // Assert
            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedSubnet, subnet);
        }

        #endregion // end: TryFromBytes

        #region Parse(string)

        /// <summary>Gets theory data for <see cref="Parse_String_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), CIDR input string.</returns>
        public static TheoryData<Subnet, string> Parse_String_Test_Values()
        {
            var data = new TheoryData<Subnet, string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, $"{ipAddress}/{i}");
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, $"{ipAddress}/{i}");
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> returns the expected subnet for a CIDR string.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="input">CIDR string to parse.</param>
        [Theory]
        [MemberData(nameof(Parse_String_Test_Values))]
        public void Parse_String_Test(Subnet expected, string input)
        {
            // Arrange

            // Act
            var subnet = Subnet.Parse(input);

            // Assert
            Assert.Equal(expected, subnet);
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> throws <see cref="ArgumentNullException"/> for a null input.</summary>
        [Fact]
        public void Parse_String_Null_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.Parse((string)null));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> throws <see cref="ArgumentException"/> for whitespace-only input.</summary>
        /// <param name="input">Whitespace input string.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_String_WhiteSpace_Throws_ArgumentException_Test(string input)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.Parse(input));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> throws <see cref="FormatException"/> for a badly formatted string.</summary>
        [Fact]
        public void Parse_String_BadFormat_Throws_FormatException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<FormatException>(() => Subnet.Parse("potato"));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> throws <see cref="ArgumentException"/> when the IPv4 routing prefix exceeds 32.</summary>
        [Fact]
        public void Parse_String_IPv4RoutingPrefixTooLarge_Throws_ArgumentException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.Parse("192.168.1.0/33"));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string)"/> throws <see cref="ArgumentException"/> when the IPv6 routing prefix exceeds 128.</summary>
        [Fact]
        public void Parse_String_IPv6RoutingPrefixTooLarge_Throws_ArgumentException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.Parse("::/129"));
        }

        #endregion // end: Parse(string)

        #region RoughSubnetStringPattern

        /// <summary>Gets theory data for <see cref="RoughSubnetStringPattern_Matches_ReturnsExpected_Test"/>.</summary>
        /// <value>Parameters: expected match result (bool), input string to test against the pattern.</value>
        public static TheoryData<bool, string> RoughSubnetStringPattern_Test_Data =>
            new()
            {
                // IPv4 CIDR notation
                { true, "192.168.1.0/24" },
                { true, "0.0.0.0/0" },
                { true, "255.255.255.255/32" },
                // IPv6 CIDR notation
                { true, "::/0" },
                { true, "2001:db8::/32" },
                // case-insensitive (IgnoreCase)
                { true, "FEED:BEEF::/48" },
                { true, "feed:beef::/48" },
                // address only — no prefix
                { true, "192.168.1.1" },
                { true, "::" },
                { true, "2001:db8::1" },
                // non-matching — leading slash (no address part)
                { false, "/32" },
                // non-matching — empty string
                { false, string.Empty },
                // non-matching — internal space
                { false, "192.168.1.0 /24" },
            };

        /// <summary>Verifies that <see cref="Subnet.RoughSubnetStringPattern"/> matches the expected inputs.</summary>
        /// <param name="expected">Whether the pattern should produce exactly one match.</param>
        /// <param name="input">The string to test against the pattern.</param>
        [Theory]
        [MemberData(nameof(RoughSubnetStringPattern_Test_Data))]
        public void RoughSubnetStringPattern_Matches_ReturnsExpected_Test(bool expected, string input)
        {
            // Arrange
            var regex = new Regex(Subnet.RoughSubnetStringPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            // Act
            var result = regex.Matches(input).Count == 1;

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: RoughSubnetStringPattern

        #region TryParse(string)

        /// <summary>Gets theory data for <see cref="TryParse_String_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/> or null), CIDR input string.</returns>
        public static TheoryData<Subnet, string> TryParse_String_Test_Values()
        {
            var data = new TheoryData<Subnet, string>();

            data.Add(null, null);
            data.Add(null, string.Empty);
            data.Add(null, "potato");
            data.Add(null, "2001:0db8:85a3:0042:1000:8a2e:0370:7334/129");
            data.Add(null, "0.0.0.0/33");
            data.Add(null, "0.0.0.0/potato");
            data.Add(null, "potato/16");
            data.Add(null, "0.0.0.0/");
            data.Add(null, "/32");

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, $"{ipAddress}/{i}");
                }

                data.Add(new Subnet(ipAddress, ipAddress), ipAddress.ToString());
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, $"{ipAddress}/{i}");
                }

                data.Add(new Subnet(ipAddress, ipAddress), ipAddress.ToString());
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.TryParse(string, out Subnet)"/> returns the expected success flag and subnet.</summary>
        /// <param name="expected">Expected subnet output, or null on failure.</param>
        /// <param name="input">CIDR string to parse.</param>
        [Theory]
        [MemberData(nameof(TryParse_String_Test_Values))]
        public void TryParse_String_Test(Subnet expected, string input)
        {
            // Arrange

            // Act
            var success = Subnet.TryParse(input, out var subnet);

            // Assert
            Assert.Equal(expected != null, success);
            Assert.Equal(expected, subnet);
        }

        #endregion // end: TryParse(string)

        #region Parse(string, int)

        /// <summary>Gets theory data for <see cref="Parse_String_Int_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), address string, routing prefix (int).</returns>
        public static TheoryData<Subnet, string, int> Parse_String_Int_Test_Values()
        {
            var data = new TheoryData<Subnet, string, int>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, ipAddress.ToString(), i);
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(subnet, ipAddress.ToString(), i);
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, int)"/> returns the expected subnet for an address string and routing prefix.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="addressString">IP address string.</param>
        /// <param name="routePrefix">Routing prefix length.</param>
        [Theory]
        [MemberData(nameof(Parse_String_Int_Test_Values))]
        public void Parse_String_Int_Test(Subnet expected, string addressString, int routePrefix)
        {
            // Arrange
            // Act
            var subnet = Subnet.Parse(addressString, routePrefix);

            // Assert
            Assert.Equal(expected, subnet);
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, int)"/> throws <see cref="ArgumentNullException"/> for a null address string.</summary>
        [Fact]
        public void Parse_String_Int_NullAddressString_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.Parse((string)null, 24));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, int)"/> throws <see cref="FormatException"/> for a badly formatted address string.</summary>
        [Fact]
        public void Parse_String_Int_BadAddressFormat_Throws_FormatException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<FormatException>(() => Subnet.Parse("potato", 24));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, int)"/> throws <see cref="ArgumentOutOfRangeException"/> when the routing prefix is out of range.</summary>
        /// <param name="addressString">IP address string.</param>
        /// <param name="routingPrefix">The out-of-range routing prefix.</param>
        [Theory]
        [InlineData("192.168.1.0", -1)]
        [InlineData("192.168.1.0", 33)]
        [InlineData("::", -1)]
        [InlineData("::", 129)]
        public void Parse_String_Int_RoutingPrefixOutOfRange_Throws_ArgumentOutOfRangeException_Test(
            string addressString,
            int routingPrefix
        )
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => Subnet.Parse(addressString, routingPrefix));
        }

        #endregion // end: Parse(string, int)

        #region TryParse(string, int)

        /// <summary>Gets theory data for <see cref="TryParse_String_Int_Test"/>.</summary>
        /// <returns>Parameters: expected success (bool), expected subnet (<see cref="Subnet"/> or null), address string, routing prefix (int).</returns>
        public static TheoryData<bool, Subnet, string, int> TryParse_String_Int_Test_Values()
        {
            var data = new TheoryData<bool, Subnet, string, int>();

            data.Add(false, null, null, 0);
            data.Add(false, null, "potato", 0);
            data.Add(false, null, IPAddress.Any.ToString(), -5);
            data.Add(false, null, IPAddress.IPv6Any.ToString(), -5);
            data.Add(false, null, IPAddress.Any.ToString(), 33);
            data.Add(false, null, IPAddress.IPv6Any.ToString(), 129);

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(true, subnet, ipAddress.ToString(), i);
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    data.Add(true, subnet, ipAddress.ToString(), i);
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.TryParse(string, int, out Subnet)"/> returns the expected success flag and subnet.</summary>
        /// <param name="expectedSuccess">Expected return value of the try method.</param>
        /// <param name="expectedSubnet">Expected subnet output, or null on failure.</param>
        /// <param name="addressString">IP address string.</param>
        /// <param name="routePrefix">Routing prefix length.</param>
        [Theory]
        [MemberData(nameof(TryParse_String_Int_Test_Values))]
        public void TryParse_String_Int_Test(bool expectedSuccess, Subnet expectedSubnet, string addressString, int routePrefix)
        {
            // Arrange
            // Act
            var success = Subnet.TryParse(addressString, routePrefix, out var subnet);

            // Assert
            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedSubnet, subnet);
        }

        #endregion // end: TryParse(string, int)

        #region Parse(string, string)

        /// <summary>Gets theory data for <see cref="Parse_String_String_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), low address string, high address string.</returns>
        public static TheoryData<Subnet, string, string> Parse_String_String_Test_Values()
        {
            var data = new TheoryData<Subnet, string, string>();
            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var low = subnet.Head.ToString();
                    var high = subnet.Tail.ToString();
                    if (seen.Add($"{low}|{high}"))
                    {
                        data.Add(subnet, low, high);
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var low = subnet.Head.ToString();
                    var high = subnet.Tail.ToString();
                    if (seen.Add($"{low}|{high}"))
                    {
                        data.Add(subnet, low, high);
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, string)"/> returns the expected subnet for low and high address strings.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="low">Low address string.</param>
        /// <param name="high">High address string.</param>
        [Theory]
        [MemberData(nameof(Parse_String_String_Test_Values))]
        public void Parse_String_String_Test(Subnet expected, string low, string high)
        {
            // Arrange
            // Act
            var subnet = Subnet.Parse(low, high);

            // Assert
            Assert.Equal(expected, subnet);
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, string)"/> throws <see cref="ArgumentNullException"/> when either address string is null.</summary>
        /// <param name="low">Low address string, or null.</param>
        /// <param name="high">High address string, or null.</param>
        [Theory]
        [InlineData("::", null)]
        [InlineData(null, "::")]
        [InlineData("192.168.1.1", null)]
        [InlineData(null, "192.168.1.1")]
        [InlineData(null, null)]
        public void Parse_String_String_NullAddressString_Throws_ArgumentNullException_Test(string low, string high)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.Parse(low, high));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, string)"/> throws <see cref="FormatException"/> for badly formatted address strings.</summary>
        /// <param name="low">Low address string.</param>
        /// <param name="high">High address string.</param>
        [Theory]
        [InlineData("::", "potato")]
        [InlineData("potato", "::")]
        [InlineData("192.168.1.1", "potato")]
        [InlineData("potato", "192.168.1.1")]
        public void Parse_String_String_BadAddressFormat_Throws_FormatException_Test(string low, string high)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<FormatException>(() => Subnet.Parse(low, high));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, string)"/> throws <see cref="ArgumentException"/> when the address strings are from different address families.</summary>
        /// <param name="low">Low address string.</param>
        /// <param name="high">High address string of a different family.</param>
        [Theory]
        [InlineData("192.168.1.1", "::")]
        [InlineData("::", "192.168.1.1")]
        public void Parse_String_String_MisMatchAddressFamily_Throws_ArgumentException_Test(string low, string high)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.Parse(low, high));
        }

        /// <summary>Verifies that <see cref="Subnet.Parse(string, string)"/> throws <see cref="InvalidOperationException"/> when the low address is greater than the high address.</summary>
        /// <param name="low">Low address string that is actually higher.</param>
        /// <param name="high">High address string that is actually lower.</param>
        [Theory]
        [InlineData("192.168.1.32", "192.168.1.0")]
        [InlineData("::20", "::")]
        public void Parse_String_String_InvalidRange_Throws_InvalidOperationException_Test(string low, string high)
        {
            // Arrange
            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => Subnet.Parse(low, high));
        }

        #endregion // end: Parse(string, string)

        #region TryParse(string, string)

        /// <summary>Gets theory data for <see cref="TryParse_String_String_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/> or null), low address string, high address string.</returns>
        public static TheoryData<Subnet, string, string> TryParse_String_String_Test_Values()
        {
            var data = new TheoryData<Subnet, string, string>();

            foreach (var s in new[] { null, string.Empty, "\t", "potato" })
            {
                data.Add(null, "192.168.1.1", s);
                data.Add(null, s, "192.168.1.1");
                data.Add(null, "2001:0db8:85a3:0042:1000:8a2e:0370:7334", s);
                data.Add(null, s, "2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }

            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var low = subnet.Head.ToString();
                    var high = subnet.Tail.ToString();
                    if (seen.Add($"{low}|{high}"))
                    {
                        data.Add(subnet, low, high);
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    var low = subnet.Head.ToString();
                    var high = subnet.Tail.ToString();
                    if (seen.Add($"{low}|{high}"))
                    {
                        data.Add(subnet, low, high);
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.TryParse(string, string, out Subnet)"/> returns the expected success flag and subnet.</summary>
        /// <param name="expected">Expected subnet output, or null on failure.</param>
        /// <param name="low">Low address string.</param>
        /// <param name="high">High address string.</param>
        [Theory]
        [MemberData(nameof(TryParse_String_String_Test_Values))]
        public void TryParse_String_String_Test(Subnet expected, string low, string high)
        {
            // Arrange

            // Act
            var success = Subnet.TryParse(low, high, out var subnet);

            // Assert
            Assert.Equal(expected != null, success);
            Assert.Equal(expected, subnet);
        }

        #endregion // end: TryParse(string, string)

        #endregion // end: Static Factory Methods

        #region Length / TryGetLength

        /// <summary>Gets theory data for <see cref="Length_Test"/>, <see cref="TryGetLength_Integer_Test"/>, and <see cref="TryGetLength_Long_Test"/>.</summary>
        /// <returns>Parameters: expected length (<see cref="BigInteger"/>), subnet under test (<see cref="Subnet"/>).</returns>
        public static TheoryData<BigInteger, Subnet> Length_Test_Values()
        {
            var data = new TheoryData<BigInteger, Subnet>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var routePrefix = 32 - i;
                    var length = BigInteger.Pow(2, routePrefix);

                    var subnet = new Subnet(ipAddress, i);
                    data.Add(length, subnet);
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var routePrefix = 128 - i;
                    var length = BigInteger.Pow(2, routePrefix);

                    var subnet = new Subnet(ipAddress, i);
                    data.Add(length, subnet);
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.Length"/> returns the expected address count for a subnet.</summary>
        /// <param name="expected">Expected length.</param>
        /// <param name="subnet">The subnet under test.</param>
        [Theory]
        [MemberData(nameof(Length_Test_Values))]
        public static void Length_Test(BigInteger expected, Subnet subnet)
        {
            // Arrange
            // Act

            var result = subnet.Length;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.TryGetLength(out int)"/> returns the expected success flag and integer length.</summary>
        /// <param name="expected">Expected length as a <see cref="BigInteger"/>.</param>
        /// <param name="subnet">The subnet under test.</param>
        [Theory]
        [MemberData(nameof(Length_Test_Values))]
        public static void TryGetLength_Integer_Test(BigInteger expected, Subnet subnet)
        {
            // Arrange
            // Act
            var success = subnet.TryGetLength(out int length);

            // Assert
            Assert.Equal(expected <= int.MaxValue, success);
            Assert.Equal(expected <= int.MaxValue ? (int)expected : -1, length);
        }

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.TryGetLength(out long)"/> returns the expected success flag and long length.</summary>
        /// <param name="expected">Expected length as a <see cref="BigInteger"/>.</param>
        /// <param name="subnet">The subnet under test.</param>
        [Theory]
        [MemberData(nameof(Length_Test_Values))]
        public static void TryGetLength_Long_Test(BigInteger expected, Subnet subnet)
        {
            // Arrange
            // Act
            var success = subnet.TryGetLength(out long length);

            // Assert
            Assert.Equal(expected <= long.MaxValue, success);
            Assert.Equal(expected <= long.MaxValue ? (long)expected : -1, length);
        }

        #endregion // end: Length / TryGetLength

        #region Equals

        #region Equals(Subnet)

        /// <summary>Gets theory data for <see cref="Equals_Subnet_Test"/> and <see cref="Equals_Object_BoxedSubnet_Test"/>.</summary>
        /// <returns>Parameters: expected equality result (bool), first subnet (<see cref="Subnet"/>), second subnet (<see cref="Subnet"/>).</returns>
        public static TheoryData<bool, Subnet, Subnet> Equals_Subnet_Test_Values()
        {
            var data = new TheoryData<bool, Subnet, Subnet>();
            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var sA = new Subnet(ipAddress, i);
                    var sB = new Subnet(ipAddress, (i + 2) % 32);
                    var sv6A = new Subnet(IPAddress.IPv6Any, i);
                    var sv6B = new Subnet(IPAddress.IPv6Loopback, i);

                    if (seen.Add($"T|{sA.ToString("f", null)}|{sA.ToString("f", null)}"))
                    {
                        data.Add(true, sA, sA); // equivalent
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sB.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sB); // differing prefix
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sv6A.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sv6A); // different family
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sv6B.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sv6B); // different family
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var sA = new Subnet(ipAddress, i);
                    var sB = new Subnet(ipAddress, (i + 2) % 128);

                    if (seen.Add($"T|{sA.ToString("f", null)}|{sA.ToString("f", null)}"))
                    {
                        data.Add(true, sA, sA); // equivalent
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sB.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sB); // differing prefix
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(Subnet)"/> returns the expected equality result.</summary>
        /// <param name="expected">Expected equality result.</param>
        /// <param name="subnetA">The subject subnet.</param>
        /// <param name="subnetB">The subnet to compare against.</param>
        [Theory]
        [MemberData(nameof(Equals_Subnet_Test_Values))]
        public void Equals_Subnet_Test(bool expected, Subnet subnetA, Subnet subnetB)
        {
            // Arrange

            // Act
            var result = subnetA.Equals(subnetB);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(Subnet)"/> returns <see langword="false"/> when the argument is null.</summary>
        [Fact]
        public void Equals_Subnet_NullOther_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals((Subnet)null);

            // Assert
            Assert.False(result);
        }

        #endregion // end: Equals(Subnet)

        #region Equals(object)

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns the expected equality result when the argument is a boxed subnet.</summary>
        /// <param name="expected">Expected equality result.</param>
        /// <param name="subnetA">The subject subnet.</param>
        /// <param name="subnetB">The boxed subnet to compare against.</param>
        [Theory]
        [MemberData(nameof(Equals_Subnet_Test_Values))]
        public void Equals_Object_BoxedSubnet_Test(bool expected, Subnet subnetA, object subnetB)
        {
            // Arrange

            // Act
            var result = subnetA.Equals(subnetB);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns <see langword="false"/> when the argument is not a <see cref="Subnet"/>.</summary>
        [Fact]
        public void Equals_Object_NonSubnetType_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals("not a subnet");

            // Assert
            Assert.False(result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns <see langword="false"/> when the argument is null.</summary>
        [Fact]
        public void Equals_Object_Null_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals((object)null);

            // Assert
            Assert.False(result);
        }

        #endregion // end: Equals(object)

        #endregion // end: Equals

        #region FromNetMask

        /// <summary>Gets theory data for <see cref="FromNetMask_Test"/>.</summary>
        /// <returns>Parameters: expected subnet (<see cref="Subnet"/>), network prefix address (<see cref="IPAddress"/>), netmask address (<see cref="IPAddress"/>).</returns>
        public static TheoryData<Subnet, IPAddress, IPAddress> FromNetMask_Test_Values()
        {
            var data = new TheoryData<Subnet, IPAddress, IPAddress>();
            var networkPrefix = IPAddress.Parse("192.168.1.1");

            for (var i = 0; i <= 32; i++)
            {
                var netmaskBytes = BigEndianBitWrapper.CreateMask(4, i).ToBytes();

                var netmask = new IPAddress(netmaskBytes);

                var expected = new Subnet(networkPrefix, i);

                data.Add(expected, networkPrefix, netmask);
            }

            return data;
        }

        /// <summary>Verifies that <see cref="Subnet.FromNetMask(IPAddress, IPAddress)"/> returns the expected subnet for a network prefix and netmask.</summary>
        /// <param name="expected">Expected subnet.</param>
        /// <param name="networkPrefix">Network prefix address.</param>
        /// <param name="netmask">Netmask address.</param>
        [Theory]
        [MemberData(nameof(FromNetMask_Test_Values))]
        public void FromNetMask_Test(Subnet expected, IPAddress networkPrefix, IPAddress netmask)
        {
            // Arrange
            // Act
            var result = Subnet.FromNetMask(networkPrefix, netmask);

            // Assert
            Assert.Equal(expected, result);
            Assert.Equal(netmask, result.Netmask);
        }

        /// <summary>Verifies that <see cref="Subnet.FromNetMask(IPAddress, IPAddress)"/> throws <see cref="ArgumentNullException"/> when the address is null.</summary>
        [Fact]
        public void FromNetMask_NullAddress_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.FromNetMask(null, IPAddress.Any));
        }

        /// <summary>Verifies that <see cref="Subnet.FromNetMask(IPAddress, IPAddress)"/> throws <see cref="ArgumentNullException"/> when the netmask is null.</summary>
        [Fact]
        public void FromNetMask_NullNetMask_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Subnet.FromNetMask(IPAddress.Any, null));
        }

        /// <summary>Verifies that <see cref="Subnet.FromNetMask(IPAddress, IPAddress)"/> throws <see cref="ArgumentException"/> when the netmask is invalid.</summary>
        [Fact]
        public void FromNetMask_InvalidNetMask_Throws_ArgumentException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.FromNetMask(IPAddress.Any, IPAddress.IPv6Any));
        }

        /// <summary>Verifies that <see cref="Subnet.FromNetMask(IPAddress, IPAddress)"/> throws <see cref="ArgumentException"/> when the address is IPv6.</summary>
        [Fact]
        public void FromNetMask_IPv6Address_Throws_ArgumentException_Test()
        {
            // Arrange
            // Act / Assert
            Assert.Throws<ArgumentException>(() => Subnet.FromNetMask(IPAddress.IPv6Any, IPAddress.Any));
        }

        /// <summary>Gets theory data for <see cref="TryFromNetMask_Test"/>.</summary>
        /// <returns>Parameters: expected success (bool), expected subnet (<see cref="Subnet"/> or null), network prefix address (<see cref="IPAddress"/>), netmask address (<see cref="IPAddress"/>).</returns>
        public static TheoryData<bool, Subnet, IPAddress, IPAddress> TryFromNetMask_Test_Values()
        {
            var data = new TheoryData<bool, Subnet, IPAddress, IPAddress>();
            var networkPrefix = IPAddress.Parse("192.168.1.1");

            for (var i = 0; i <= 32; i++)
            {
                var netmaskBytes = BigEndianBitWrapper.CreateMask(4, i).ToBytes();

                var netmask = new IPAddress(netmaskBytes);

                var expected = new Subnet(networkPrefix, i);

                data.Add(true, expected, networkPrefix, netmask);
            }

            data.Add(false, null, null, IPAddress.Parse("192.168.1.1"));
            data.Add(false, null, IPAddress.Parse("192.168.1.1"), null);
            data.Add(false, null, null, IPAddress.Parse("::"));
            data.Add(false, null, IPAddress.Parse("::"), null);
            data.Add(false, null, null, null);

            return data;
        }

        /// <summary>Verifies that <see cref="Subnet.TryFromNetMask(IPAddress, IPAddress, out Subnet)"/> returns the expected success flag and subnet.</summary>
        /// <param name="expectedSuccess">Expected return value of the try method.</param>
        /// <param name="expectedSubnet">Expected subnet output, or null on failure.</param>
        /// <param name="networkPrefix">Network prefix address.</param>
        /// <param name="netmask">Netmask address.</param>
        [Theory]
        [MemberData(nameof(TryFromNetMask_Test_Values))]
        public void TryFromNetMask_Test(bool expectedSuccess, Subnet expectedSubnet, IPAddress networkPrefix, IPAddress netmask)
        {
            // Arrange
            // Act
            var success = Subnet.TryFromNetMask(networkPrefix, netmask, out var subnet);

            // Assert
            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedSubnet, subnet);
        }

        #endregion // end: FromNetMask

        #region Formatting

        #region ToString

        /// <summary>Verifies that <see cref="AbstractIPAddressRange.ToString()"/> matches the general format string output.</summary>
        /// <param name="headString">Head address string of the range.</param>
        /// <param name="tailString">Tail address string of the range.</param>
        [Theory]
        [InlineData("192.168.1.1", "192.168.1.42")]
        [InlineData("::beef", "0123::dead")]
        public void ToString_MatchesGeneralFormat_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = new IPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.ToString();

            // Assert
            Assert.Equal($"{iPAddressRange:G}", result);
        }

        #endregion // end: ToString

        #region ToString(string, IFormatProvider)

        /// <summary>Gets theory data for <see cref="ToString_Format_Test"/>.</summary>
        /// <returns>Parameters: expected string result, format specifier string, format provider (<see cref="IFormatProvider"/>), subnet under test (<see cref="Subnet"/>).</returns>
        public static TheoryData<string, string, IFormatProvider, Subnet> ToString_Format_Test_Values()
        {
            var data = new TheoryData<string, string, IFormatProvider, Subnet>();

            // general formats
            foreach (var format in new[] { null, string.Empty, "g", "G" })
            {
                foreach (var subnet in Ipv4AddressSubnets().Concat(Ipv6AddressSubnets()))
                {
                    data.Add(
                        $"{subnet.NetworkPrefixAddress}/{subnet.RoutingPrefix}",
                        format,
                        CultureInfo.CurrentCulture,
                        subnet
                    );
                }
            }

            // "friendly" formats
            foreach (var format in new[] { "f", "F" })
            {
                foreach (var subnet in Ipv4AddressSubnets().Concat(Ipv6AddressSubnets()))
                {
                    data.Add(
                        subnet.IsSingleIP
                            ? $"{subnet.NetworkPrefixAddress}"
                            : $"{subnet.NetworkPrefixAddress}/{subnet.RoutingPrefix}",
                        format,
                        CultureInfo.CurrentCulture,
                        subnet
                    );
                }
            }

            // range formats
            foreach (var format in new[] { "r", "R" })
            {
                foreach (var subnet in Ipv4AddressSubnets().Concat(Ipv6AddressSubnets()))
                {
                    data.Add($"{subnet.Head} - {subnet.Tail}", format, CultureInfo.CurrentCulture, subnet);
                }
            }

            return data;

            IEnumerable<Subnet> Ipv4AddressSubnets()
            {
                yield return new Subnet(IPAddress.Parse("192.168.1.1"), 16);
                yield return new Subnet(IPAddress.Parse("192.168.1.1"), 32);
            }

            IEnumerable<Subnet> Ipv6AddressSubnets()
            {
                yield return new Subnet(IPAddress.Parse("abc:123::beef"), 16);
                yield return new Subnet(IPAddress.Parse("abc:123::beef"), 128);
            }
        }

        /// <summary>Verifies that <see cref="Subnet.ToString(string, IFormatProvider)"/> returns the expected formatted string.</summary>
        /// <param name="expected">Expected formatted string.</param>
        /// <param name="format">Format specifier.</param>
        /// <param name="formatProvider">Format provider.</param>
        /// <param name="subnet">The subnet to format.</param>
        [Theory]
        [MemberData(nameof(ToString_Format_Test_Values))]
        public void ToString_Format_Test(string expected, string format, IFormatProvider formatProvider, Subnet subnet)
        {
            // Arrange
            // Act
            var result = subnet.ToString(format, formatProvider);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.ToString(string, IFormatProvider)"/> uses invariant culture when the format provider is null.</summary>
        [Fact]
        public void ToString_NullFormatProvider_UsesInvariantCulture_Test()
        {
            // Arrange
            var subnet = new Subnet(IPAddress.Parse("192.168.1.0"), 24);

            // Act
            var result = subnet.ToString("G", null);

            // Assert
            Assert.Equal("192.168.1.0/24", result);
        }

        /// <summary>Verifies that <see cref="Subnet.ToString(string, IFormatProvider)"/> throws <see cref="FormatException"/> for an unknown format specifier.</summary>
        [Fact]
        public void ToString_UnknownFormat_Throws_FormatException_Test()
        {
            // Arrange
            var range = new Subnet(IPAddress.Parse("192.168.1.1"), 16);

            // Act / Assert
            Assert.Throws<FormatException>(() => range.ToString("potato", CultureInfo.CurrentCulture));
        }

        #endregion // end: ToString(string, IFormatProvider)

        #endregion // end: Formatting

        #region GetHashCode

        /// <summary>Gets theory data for <see cref="GetHashCode_Test"/>.</summary>
        /// <returns>Parameters: expected hash equality (bool), left subnet (<see cref="Subnet"/>), right subnet (<see cref="Subnet"/>).</returns>
        public static TheoryData<bool, Subnet, Subnet> GetHashCode_Test_Values()
        {
            return new TheoryData<bool, Subnet, Subnet>
            {
                // value-equal IPv4
                { true, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.Any, 16) },
                // value-equal IPv6
                { true, new Subnet(IPAddress.IPv6Any, 16), new Subnet(IPAddress.IPv6Any, 16) },
                // expected different
                { false, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.IPv6Any, 16) },
                { false, new Subnet(IPAddress.Any, 8), new Subnet(IPAddress.Any, 16) },
                { false, new Subnet(IPAddress.IPv6Any, 8), new Subnet(IPAddress.IPv6Any, 16) },
                { false, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.Broadcast, 16) },
                { false, new Subnet(IPAddress.Parse("::")), new Subnet(IPAddress.Parse("ab::"), 16) },
            };
        }

        /// <summary>Verifies that <see cref="Subnet.GetHashCode"/> returns equal hashes for value-equal subnets and distinct hashes for different subnets.</summary>
        /// <param name="expectedEqual">Whether the hash codes are expected to be equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(GetHashCode_Test_Values))]
        public void GetHashCode_Test(bool expectedEqual, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var leftHash = left.GetHashCode();
            var rightHash = right.GetHashCode();

            // Assert
            Assert.Equal(expectedEqual, leftHash == rightHash);
        }

        #endregion // end: GetHashCode

        #region Deconstruct

        /// <summary>Gets theory data for deconstruct tests.</summary>
        /// <returns>Parameters: subnet under test (<see cref="Subnet"/>).</returns>
        public static TheoryData<Subnet> Deconstruct_Values()
        {
            var data = new TheoryData<Subnet>();
            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    if (seen.Add(subnet.ToString("f", null)))
                    {
                        data.Add(subnet);
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var subnet = new Subnet(ipAddress, i);
                    if (seen.Add(subnet.ToString("f", null)))
                    {
                        data.Add(subnet);
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Parse("192.168.1.1");
                yield return IPAddress.Parse("255.255.0.0");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
                yield return IPAddress.Parse("dead:beef::");
            }
        }

        #region Deconstruct(IPAddress, IPAddress, IPAddress, int)

        /// <summary>Verifies that the four-element deconstruct of a <see cref="Subnet"/> yields the expected components.</summary>
        /// <param name="subnet">The subnet to deconstruct.</param>
        [Theory]
        [MemberData(nameof(Deconstruct_Values))]
        public void Deconstruct_IPAddress_IPAddress_IPAddress_Int_Test(Subnet subnet)
        {
            // Arrange
            // Act
            var (networkPrefixAddress, broadcastAddress, netmask, routingPrefix) = subnet;

            // Assert
            Assert.Equal(subnet.NetworkPrefixAddress, networkPrefixAddress);
            Assert.Equal(subnet.BroadcastAddress, broadcastAddress);
            Assert.Equal(subnet.Netmask, netmask);
            Assert.Equal(subnet.RoutingPrefix, routingPrefix);
        }

        #endregion // end: Deconstruct(IPAddress, IPAddress, IPAddress, int)

        #region Deconstruct(IPAddress, int)

        /// <summary>Verifies that the two-element deconstruct of a <see cref="Subnet"/> yields the network prefix address and routing prefix.</summary>
        /// <param name="subnet">The subnet to deconstruct.</param>
        [Theory]
        [MemberData(nameof(Deconstruct_Values))]
        public void Deconstruct_IPAddress_Int_Test(Subnet subnet)
        {
            // Arrange
            // Act
            var (networkPrefixAddress, routingPrefix) = subnet;

            // Assert
            Assert.Equal(subnet.NetworkPrefixAddress, networkPrefixAddress);
            Assert.Equal(subnet.RoutingPrefix, routingPrefix);
        }

        #endregion // end: Deconstruct(IPAddress, int)

        #endregion // end: Deconstruct

        #region Ctor refactoring verification

        /// <summary>Verifies that the Subnet(IPAddress, IPAddress) ctor computes correct prefix and netmask.</summary>
        /// <param name="subnetString">CIDR notation expected after construction from low/high addresses.</param>
        /// <param name="lowString">Low address string.</param>
        /// <param name="highString">High address string.</param>
        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0", "192.168.1.255")]
        [InlineData("192.168.1.8/29", "192.168.1.8", "192.168.1.15")]
        [InlineData("10.0.0.0/8", "10.0.0.0", "10.255.255.255")]
        [InlineData("::/112", "::", "::ffff")]
        [InlineData("feed:beef::/32", "feed:beef::", "feed:beef:ffff:ffff:ffff:ffff:ffff:ffff")]
        public void Ctor_FromLowHigh_ComputesPrefixAndNetmask_Test(string subnetString, string lowString, string highString)
        {
            // Arrange
            var expected = Subnet.Parse(subnetString);
            var low = IPAddress.Parse(lowString);
            var high = IPAddress.Parse(highString);

            // Act
            var subnet = new Subnet(low, high);

            // Assert
            Assert.Equal(expected.RoutingPrefix, subnet.RoutingPrefix);
            Assert.Equal(expected.NetworkPrefixAddress, subnet.NetworkPrefixAddress);
            Assert.Equal(expected.BroadcastAddress, subnet.BroadcastAddress);
            Assert.Equal(expected.Netmask, subnet.Netmask);
        }

        #endregion // end: Ctor refactoring verification

        internal class SubnetEqualityComparer : IEqualityComparer<Subnet>
        {
            public static readonly SubnetEqualityComparer Instance = new();

            public bool Equals(Subnet x, Subnet y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null && y is null)
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.Equals(y);
            }

            public int GetHashCode(Subnet obj)
            {
                return obj is null ? -1 : obj.GetHashCode();
            }
        }
    }
}
