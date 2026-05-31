using System;
using System.Net;
using Arcus.Math;
using Xunit;

namespace Arcus.Tests.Math
{
    public class IPAddressMathTests
    {
        #region IsEqualTo

        public static TheoryData<bool, IPAddress, IPAddress> IsEqualTo_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress>();

                // reference equal — same instance, verifies the ReferenceEquals fast path
                var ipv4SameAddress = IPAddress.Parse("192.168.1.1");
                data.Add(true, ipv4SameAddress, ipv4SameAddress);

                var ipv6SameAddress = IPAddress.Parse("abc::123");
                data.Add(true, ipv6SameAddress, ipv6SameAddress);

                // value equal — distinct instances with equal values, verifies Equals fallback
                data.Add(true, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.1"));
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"));

                // both null
                data.Add(true, null, null);

                // not equal
                data.Add(false, IPAddress.Parse("192.168.1.25"), IPAddress.Parse("192.168.1.1"));
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.25"));
                data.Add(false, IPAddress.Parse("abc::fff"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("abc::fff"));

                // null vs non-null
                data.Add(false, null, IPAddress.Parse("192.168.1.1"));
                data.Add(false, null, IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("192.168.1.1"), null);
                data.Add(false, IPAddress.Parse("abc::123"), null);

                // differing address families
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("192.168.1.1"));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsEqualTo_Test_Data))]
        public void IsEqualTo_TwoAddresses_ReturnsExpectedEquality_Test(bool expected, IPAddress left, IPAddress right)
        {
            // Arrange
            // Act
            var result = left.IsEqualTo(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsEqualTo

        #region IsGreaterThan

        public static TheoryData<bool, IPAddress, IPAddress> IsGreaterThan_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress>();

                // reference equal — not greater than (verifies ReferenceEquals fast path)
                var ipv4SameAddress = IPAddress.Parse("192.168.1.1");
                data.Add(false, ipv4SameAddress, ipv4SameAddress);

                var ipv6SameAddress = IPAddress.Parse("abc::123");
                data.Add(false, ipv6SameAddress, ipv6SameAddress);

                // value equal — not greater than (verifies Equals fallback using distinct instances)
                data.Add(false, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.1"));
                data.Add(false, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"));

                // null comparisons
                data.Add(false, null, null);
                data.Add(false, null, IPAddress.Parse("192.168.1.1"));
                data.Add(false, null, IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("192.168.1.1"), null);
                data.Add(false, IPAddress.Parse("abc::123"), null);

                // differing address families
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("192.168.1.1"));

                // greater than
                data.Add(true, IPAddress.Parse("192.168.1.25"), IPAddress.Parse("192.168.1.1"));
                data.Add(true, IPAddress.Parse("abc::fff"), IPAddress.Parse("abc::123"));

                // less than
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.25"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("abc::fff"));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsGreaterThan_Test_Data))]
        public void IsGreaterThan_TwoAddresses_ReturnsExpectedOrdering_Test(bool expected, IPAddress left, IPAddress right)
        {
            // Arrange
            // Act
            var result = left.IsGreaterThan(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsGreaterThan

        #region IsGreaterThanOrEqualTo

        public static TheoryData<bool, IPAddress, IPAddress> IsGreaterThanOrEqualTo_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress>();

                // reference equal — is >= (verifies ReferenceEquals fast path)
                var ipv4SameAddress = IPAddress.Parse("192.168.1.1");
                data.Add(true, ipv4SameAddress, ipv4SameAddress);

                var ipv6SameAddress = IPAddress.Parse("abc::123");
                data.Add(true, ipv6SameAddress, ipv6SameAddress);

                // value equal — is >= (verifies Equals fallback using distinct instances)
                data.Add(true, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.1"));
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"));

                // null comparisons
                data.Add(false, null, IPAddress.Parse("192.168.1.1"));
                data.Add(false, null, IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("192.168.1.1"), null);
                data.Add(false, IPAddress.Parse("abc::123"), null);
                data.Add(true, null, null);

                // differing address families
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("192.168.1.1"));

                // greater than
                data.Add(true, IPAddress.Parse("192.168.1.25"), IPAddress.Parse("192.168.1.1"));
                data.Add(true, IPAddress.Parse("abc::fff"), IPAddress.Parse("abc::123"));

                // less than
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.25"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("abc::fff"));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsGreaterThanOrEqualTo_Test_Data))]
        public void IsGreaterThanOrEqualTo_TwoAddresses_ReturnsExpectedOrdering_Test(
            bool expected,
            IPAddress left,
            IPAddress right
        )
        {
            // Arrange
            // Act
            var result = left.IsGreaterThanOrEqualTo(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsGreaterThanOrEqualTo

        #region IsLessThan

        public static TheoryData<bool, IPAddress, IPAddress> IsLessThan_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress>();

                // reference equal — not less than (verifies ReferenceEquals fast path)
                var ipv4SameAddress = IPAddress.Parse("192.168.1.1");
                data.Add(false, ipv4SameAddress, ipv4SameAddress);

                var ipv6SameAddress = IPAddress.Parse("abc::123");
                data.Add(false, ipv6SameAddress, ipv6SameAddress);

                // value equal — not less than (verifies Equals fallback using distinct instances)
                data.Add(false, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.1"));
                data.Add(false, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"));

                // null comparisons
                data.Add(false, null, null);
                data.Add(false, null, IPAddress.Parse("192.168.1.1"));
                data.Add(false, null, IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("192.168.1.1"), null);
                data.Add(false, IPAddress.Parse("abc::123"), null);

                // differing address families
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("192.168.1.1"));

                // greater than — not less than
                data.Add(false, IPAddress.Parse("192.168.1.25"), IPAddress.Parse("192.168.1.1"));
                data.Add(false, IPAddress.Parse("abc::fff"), IPAddress.Parse("abc::123"));

                // less than
                data.Add(true, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.25"));
                data.Add(true, IPAddress.Parse("abc::123"), IPAddress.Parse("abc::fff"));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsLessThan_Test_Data))]
        public void IsLessThan_TwoAddresses_ReturnsExpectedOrdering_Test(bool expected, IPAddress left, IPAddress right)
        {
            // Arrange
            // Act
            var result = left.IsLessThan(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsLessThan

        #region IsLessThanOrEqualTo

        public static TheoryData<bool, IPAddress, IPAddress> IsLessThanOrEqualTo_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress>();

                // reference equal — is <= (verifies ReferenceEquals fast path)
                var ipv4SameAddress = IPAddress.Parse("192.168.1.1");
                data.Add(true, ipv4SameAddress, ipv4SameAddress);

                var ipv6SameAddress = IPAddress.Parse("abc::123");
                data.Add(true, ipv6SameAddress, ipv6SameAddress);

                // value equal — is <= (verifies Equals fallback using distinct instances)
                data.Add(true, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.1"));
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"));

                // null comparisons
                data.Add(true, null, null);
                data.Add(false, IPAddress.Parse("192.168.1.1"), null);
                data.Add(false, IPAddress.Parse("abc::123"), null);
                data.Add(false, null, IPAddress.Parse("192.168.1.1"));
                data.Add(false, null, IPAddress.Parse("abc::123"));

                // differing address families
                data.Add(false, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("abc::123"));
                data.Add(false, IPAddress.Parse("abc::123"), IPAddress.Parse("192.168.1.1"));

                // greater than — not <=
                data.Add(false, IPAddress.Parse("192.168.1.25"), IPAddress.Parse("192.168.1.1"));
                data.Add(false, IPAddress.Parse("abc::fff"), IPAddress.Parse("abc::123"));

                // less than — is <=
                data.Add(true, IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.25"));
                data.Add(true, IPAddress.Parse("abc::123"), IPAddress.Parse("abc::fff"));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsLessThanOrEqualTo_Test_Data))]
        public void IsLessThanOrEqualTo_TwoAddresses_ReturnsExpectedOrdering_Test(
            bool expected,
            IPAddress left,
            IPAddress right
        )
        {
            // Arrange
            // Act
            var result = left.IsLessThanOrEqualTo(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsLessThanOrEqualTo

        #region IsBetween

        public static TheoryData<bool, IPAddress, IPAddress, IPAddress, bool> IsBetween_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, IPAddress, IPAddress, IPAddress, bool>();

                // reference equals low
                var ipv4SameAddress = IPAddress.Parse("192.168.0.1");
                var ipv6SameAddress = IPAddress.Parse("abc::123");

                data.Add(true, ipv4SameAddress, ipv4SameAddress, IPAddress.Parse("192.168.1.10"), true);
                data.Add(false, ipv4SameAddress, ipv4SameAddress, IPAddress.Parse("192.168.1.10"), false);
                data.Add(true, ipv6SameAddress, ipv6SameAddress, IPAddress.Parse("abc::f123"), true);
                data.Add(false, ipv6SameAddress, ipv6SameAddress, IPAddress.Parse("abc::f123"), false);

                // reference equals high
                data.Add(true, ipv4SameAddress, IPAddress.Parse("192.168.0.0"), ipv4SameAddress, true);
                data.Add(false, ipv4SameAddress, IPAddress.Parse("192.168.0.0"), ipv4SameAddress, false);
                data.Add(true, ipv6SameAddress, IPAddress.Parse("abc::"), ipv6SameAddress, true);
                data.Add(false, ipv6SameAddress, IPAddress.Parse("abc::"), ipv6SameAddress, false);

                // reference equals both low and high
                data.Add(true, ipv4SameAddress, ipv4SameAddress, ipv4SameAddress, true);
                data.Add(false, ipv4SameAddress, ipv4SameAddress, ipv4SameAddress, false);
                data.Add(true, ipv6SameAddress, ipv6SameAddress, ipv6SameAddress, true);
                data.Add(false, ipv6SameAddress, ipv6SameAddress, ipv6SameAddress, false);

                // value equals low — distinct instances, verifies Equals fallback
                data.Add(
                    true,
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.10"),
                    true
                );
                data.Add(
                    false,
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.10"),
                    false
                );
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), IPAddress.Parse("def::f456"), true);
                data.Add(false, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), IPAddress.Parse("def::f456"), false);

                // value equals high — distinct instances, verifies Equals fallback
                data.Add(
                    true,
                    IPAddress.Parse("10.20.30.10"),
                    IPAddress.Parse("10.20.30.0"),
                    IPAddress.Parse("10.20.30.10"),
                    true
                );
                data.Add(
                    false,
                    IPAddress.Parse("10.20.30.10"),
                    IPAddress.Parse("10.20.30.0"),
                    IPAddress.Parse("10.20.30.10"),
                    false
                );
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::"), IPAddress.Parse("def::456"), true);
                data.Add(false, IPAddress.Parse("def::456"), IPAddress.Parse("def::"), IPAddress.Parse("def::456"), false);

                // value equals low and high — distinct instances, verifies Equals fallback
                data.Add(
                    true,
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    true
                );
                data.Add(
                    false,
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    IPAddress.Parse("10.20.30.1"),
                    false
                );
                data.Add(true, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), true);
                data.Add(false, IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), IPAddress.Parse("def::456"), false);

                // before low — both inclusive and exclusive
                data.Add(
                    false,
                    IPAddress.Parse("192.168.1.0"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    true
                );
                data.Add(
                    false,
                    IPAddress.Parse("192.168.1.0"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    false
                );
                data.Add(false, IPAddress.Parse("abc::"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), true);
                data.Add(false, IPAddress.Parse("abc::"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), false);

                // after high — both inclusive and exclusive
                data.Add(
                    false,
                    IPAddress.Parse("192.168.20.0"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    true
                );
                data.Add(
                    false,
                    IPAddress.Parse("192.168.20.0"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    false
                );
                data.Add(false, IPAddress.Parse("abcd::"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), true);
                data.Add(false, IPAddress.Parse("abcd::"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), false);

                // inside range — both inclusive and exclusive
                data.Add(
                    true,
                    IPAddress.Parse("192.168.10.128"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    true
                );
                data.Add(
                    true,
                    IPAddress.Parse("192.168.10.128"),
                    IPAddress.Parse("192.168.10.0"),
                    IPAddress.Parse("192.168.10.255"),
                    false
                );
                data.Add(true, IPAddress.Parse("abc::fff0"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), true);
                data.Add(true, IPAddress.Parse("abc::fff0"), IPAddress.Parse("abc::ff"), IPAddress.Parse("abc::ffff"), false);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsBetween_Test_Data))]
        public void IsBetween_AddressAndRange_ReturnsExpectedMembership_Test(
            bool expected,
            IPAddress input,
            IPAddress low,
            IPAddress high,
            bool inclusive
        )
        {
            // Arrange
            // Act
            var result = input.IsBetween(low, high, inclusive);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsBetween_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => ((IPAddress)null).IsBetween(IPAddress.Any, IPAddress.Any));
        }

        [Fact]
        public void IsBetween_NullLow_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddress.Any.IsBetween(null, IPAddress.Any));
        }

        [Fact]
        public void IsBetween_NullHigh_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddress.Any.IsBetween(IPAddress.Any, null));
        }

        [Fact]
        public void IsBetween_LowGreaterThanHigh_Throws_InvalidOperationException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() =>
                IPAddress.Any.IsBetween(IPAddress.Parse("100.1.1.1"), IPAddress.Parse("10.1.1.1"))
            );
        }

        public static TheoryData<IPAddress, IPAddress, IPAddress> IsBetween_UnmatchedAddressFamilies_Test_Data
        {
            get
            {
                var data = new TheoryData<IPAddress, IPAddress, IPAddress>();

                var ipv4 = IPAddress.Any;
                var ipv6 = IPAddress.IPv6Any;

                data.Add(ipv4, ipv4, ipv6);
                data.Add(ipv4, ipv6, ipv4);
                data.Add(ipv6, ipv4, ipv4);
                data.Add(ipv6, ipv6, ipv4);
                data.Add(ipv6, ipv4, ipv6);
                data.Add(ipv4, ipv6, ipv6);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsBetween_UnmatchedAddressFamilies_Test_Data))]
        public void IsBetween_MismatchedAddressFamilies_Throws_InvalidOperationException_Test(
            IPAddress input,
            IPAddress low,
            IPAddress high
        )
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => input.IsBetween(low, high));
        }

        #endregion // end: IsBetween

        #region IsAtMax

        [Theory]
        [InlineData(false, "::")]
        [InlineData(false, "0.0.0.0")]
        [InlineData(false, "128.128.128.128")]
        [InlineData(false, "7777:7777:7777:7777:7777:7777:7777:7777")]
        [InlineData(true, "255.255.255.255")]
        [InlineData(true, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        public void IsAtMax_KnownAddress_ReturnsExpectedResult_Test(bool expected, string input)
        {
            // Arrange
            var address = IPAddress.Parse(input);

            // Act
            var result = address.IsAtMax();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAtMax_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => ((IPAddress)null).IsAtMax());
        }

        #endregion // end: IsAtMax

        #region IsAtMin

        [Theory]
        [InlineData(false, "255.255.255.255")]
        [InlineData(false, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        [InlineData(false, "128.128.128.128")]
        [InlineData(false, "7777:7777:7777:7777:7777:7777:7777:7777")]
        [InlineData(true, "::")]
        [InlineData(true, "0.0.0.0")]
        public void IsAtMin_KnownAddress_ReturnsExpectedResult_Test(bool expected, string input)
        {
            // Arrange
            var address = IPAddress.Parse(input);

            // Act
            var result = address.IsAtMin();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAtMin_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => ((IPAddress)null).IsAtMin());
        }

        #endregion // end: IsAtMin

        #region Max

        public static TheoryData<IPAddress, IPAddress, IPAddress> Max_Test_Data
        {
            get
            {
                var data = new TheoryData<IPAddress, IPAddress, IPAddress>();

                var minIpv4 = IPAddress.Parse("192.168.1.1");
                var maxIpv4 = IPAddress.Parse("192.168.100.1");

                data.Add(maxIpv4, maxIpv4, maxIpv4);
                data.Add(maxIpv4, minIpv4, maxIpv4);
                data.Add(maxIpv4, maxIpv4, minIpv4);

                var minIpv6 = IPAddress.Parse("abc::01");
                var maxIpv6 = IPAddress.Parse("ffff::f123");

                data.Add(maxIpv6, maxIpv6, maxIpv6);
                data.Add(maxIpv6, minIpv6, maxIpv6);
                data.Add(maxIpv6, maxIpv6, minIpv6);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Max_Test_Data))]
        public void Max_TwoAddresses_ReturnsLargerAddress_Test(IPAddress expected, IPAddress left, IPAddress right)
        {
            // Arrange
            // Act
            var result = IPAddressMath.Max(left, right);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Max_NullLeftInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Max(null, IPAddress.Any));
        }

        [Fact]
        public void Max_NullRightInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Max(IPAddress.Any, null));
        }

        [Fact]
        public void Max_BothInputsNull_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Max(null, null));
        }

        [Fact]
        public void Max_MismatchedAddressFamilies_LeftIPv4RightIPv6_Throws_InvalidOperationException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => IPAddressMath.Max(IPAddress.Any, IPAddress.IPv6Any));
        }

        [Fact]
        public void Max_MismatchedAddressFamilies_LeftIPv6RightIPv4_Throws_InvalidOperationException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => IPAddressMath.Max(IPAddress.IPv6Any, IPAddress.Any));
        }

        #endregion // end: Max

        #region Min

        public static TheoryData<IPAddress, IPAddress, IPAddress> Min_Test_Data
        {
            get
            {
                var data = new TheoryData<IPAddress, IPAddress, IPAddress>();

                var minIpv4 = IPAddress.Parse("192.168.1.1");
                var maxIpv4 = IPAddress.Parse("192.168.100.1");

                data.Add(minIpv4, minIpv4, minIpv4);
                data.Add(minIpv4, minIpv4, maxIpv4);
                data.Add(minIpv4, maxIpv4, minIpv4);

                var minIpv6 = IPAddress.Parse("abc::01");
                var maxIpv6 = IPAddress.Parse("ffff::f123");

                data.Add(minIpv6, minIpv6, minIpv6);
                data.Add(minIpv6, minIpv6, maxIpv6);
                data.Add(minIpv6, maxIpv6, minIpv6);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Min_Test_Data))]
        public void Min_TwoAddresses_ReturnsSmallerAddress_Test(IPAddress expected, IPAddress left, IPAddress right)
        {
            // Arrange
            // Act
            var result = IPAddressMath.Min(left, right);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Min_NullLeftInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Min(null, IPAddress.Any));
        }

        [Fact]
        public void Min_NullRightInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Min(IPAddress.Any, null));
        }

        [Fact]
        public void Min_BothInputsNull_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressMath.Min(null, null));
        }

        [Fact]
        public void Min_MismatchedAddressFamilies_LeftIPv4RightIPv6_Throws_InvalidOperationException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => IPAddressMath.Min(IPAddress.Any, IPAddress.IPv6Any));
        }

        [Fact]
        public void Min_MismatchedAddressFamilies_LeftIPv6RightIPv4_Throws_InvalidOperationException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => IPAddressMath.Min(IPAddress.IPv6Any, IPAddress.Any));
        }

        #endregion // end: Min

        #region Increment

        [Theory]
        [InlineData("::", "::", 0)]
        [InlineData("::1", "::", 1)]
        [InlineData("abcd::53b0", "abcd::7ac0", -10000)]
        [InlineData("abcd::72c0", "abcd::7ac0", -2048)]
        [InlineData("abcd::82c0", "abcd::7ac0", 2048)]
        [InlineData("abcd::a1d0", "abcd::7ac0", 10000)]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", -1)]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe", 1)]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 0)]
        [InlineData("192.167.253.0", "192.168.1.0", -1024)]
        [InlineData("192.168.0.255", "192.168.1.0", -1)]
        [InlineData("192.168.1.0", "192.168.1.0", 0)]
        [InlineData("192.168.1.0", "192.168.1.255", -255)]
        [InlineData("192.168.1.1", "192.168.1.0", 1)]
        [InlineData("192.168.1.2", "192.168.1.0", 2)]
        [InlineData("192.168.1.255", "192.168.1.0", 255)]
        [InlineData("192.168.5.0", "192.168.1.0", 1024)]
        [InlineData("255.255.255.254", "255.255.255.255", -1)]
        [InlineData("255.255.255.255", "255.255.255.254", 1)]
        [InlineData("255.255.255.255", "255.255.255.255", 0)]
        public void Increment_ValidDelta_ReturnsExpectedAddress_Test(string expected, string input, long delta)
        {
            // Arrange
            var address = IPAddress.Parse(input);

            // Act
            var result = address.Increment(delta);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [InlineData("::", -1)] // IPv6 underflow
        [InlineData("::FF", -1024)] // IPv6 underflow by large negative delta
        [InlineData("0.0.0.0", -1)] // IPv4 underflow
        [InlineData("0.0.0.255", -1024)] // IPv4 underflow by large negative delta
        public void Increment_NegativeDeltaCausesUnderflow_Throws_InvalidOperationException_Test(string input, long delta)
        {
            // Arrange
            var address = IPAddress.Parse(input);

            // Act
            Action act = () => address.Increment(delta);

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Theory]
        [InlineData("255.255.255.0", 1024)] // IPv4 overflow by large positive delta
        [InlineData("255.255.255.255", 1)] // IPv4 overflow at max
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ff", 65535)] // IPv6 overflow by large positive delta
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 1)] // IPv6 overflow at max
        public void Increment_PositiveDeltaCausesOverflow_Throws_InvalidOperationException_Test(string input, long delta)
        {
            // Arrange
            var address = IPAddress.Parse(input);

            // Act
            Action act = () => address.Increment(delta);

            // Assert — overflow message is distinct from underflow
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("overflow", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Increment_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => ((IPAddress)null).Increment());
        }

        #region TryIncrement

        [Theory]
        [InlineData(false, null, null, 0)]
        [InlineData(false, null, "255.255.255.0", 1024)]
        [InlineData(false, null, "255.255.255.255", 1)]
        [InlineData(false, null, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ff", 65535)]
        [InlineData(false, null, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 1)]
        [InlineData(false, null, "::", -1)]
        [InlineData(false, null, "::FF", -1024)]
        [InlineData(false, null, "0.0.0.0", -1)]
        [InlineData(false, null, "0.0.0.255", -1024)]
        [InlineData(true, "::", "::", 0)]
        [InlineData(true, "::1", "::", 1)]
        [InlineData(true, "abcd::53b0", "abcd::7ac0", -10000)]
        [InlineData(true, "abcd::72c0", "abcd::7ac0", -2048)]
        [InlineData(true, "abcd::82c0", "abcd::7ac0", 2048)]
        [InlineData(true, "abcd::a1d0", "abcd::7ac0", 10000)]
        [InlineData(true, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", -1)]
        [InlineData(true, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe", 1)]
        [InlineData(true, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", 0)]
        [InlineData(true, "192.167.253.0", "192.168.1.0", -1024)]
        [InlineData(true, "192.168.0.255", "192.168.1.0", -1)]
        [InlineData(true, "192.168.1.0", "192.168.1.0", 0)]
        [InlineData(true, "192.168.1.0", "192.168.1.255", -255)]
        [InlineData(true, "192.168.1.1", "192.168.1.0", 1)]
        [InlineData(true, "192.168.1.2", "192.168.1.0", 2)]
        [InlineData(true, "192.168.1.255", "192.168.1.0", 255)]
        [InlineData(true, "192.168.5.0", "192.168.1.0", 1024)]
        [InlineData(true, "255.255.255.254", "255.255.255.255", -1)]
        [InlineData(true, "255.255.255.255", "255.255.255.254", 1)]
        [InlineData(true, "255.255.255.255", "255.255.255.255", 0)]
        public void TryIncrement_VariousInputs_ReturnsExpectedSuccessAndAddress_Test(
            bool expectedSuccess,
            string expectedResultString,
            string inputString,
            long delta
        )
        {
            // Arrange
            _ = IPAddress.TryParse(inputString, out var input);

            // Act
            var successResult = IPAddressMath.TryIncrement(input, out var result, delta);

            // Assert
            Assert.Equal(expectedSuccess, successResult);
            _ = IPAddress.TryParse(expectedResultString, out var expectedResultAddress);
            Assert.Equal(expectedResultAddress, result);
        }

        #endregion // end: TryIncrement

        #endregion // end: Increment
    }
}
