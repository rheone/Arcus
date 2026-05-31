using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Arcus.Math;
using Arcus.Utilities;
using NSubstitute;
using Xunit;

namespace Arcus.Tests
{
    public class AbstractIPAddressRangeTests
    {
        #region Setup / Teardown

        public AbstractIPAddressRangeTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        private readonly ITestOutputHelper _testOutputHelper;

        #endregion // end: Setup / Teardown

        #region Deconstructors

        [Fact]
        public void Deconstruct_Head_Tail_Test()
        {
            // Arrange
            const string headString = "192.168.1.1";
            const string tailString = "192.168.1.3";
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            var (resultHead, resultTail) = CreateSubstituteIPAddressRange(head, tail);

            // Assert
            Assert.Equal(resultHead, head);
            Assert.Equal(resultTail, tail);
        }

        #endregion // end: Deconstructors

        #region other members

        private static AbstractIPAddressRange CreateSubstituteIPAddressRange(IPAddress head, IPAddress tail)
        {
            return Substitute.For<AbstractIPAddressRange>(head, tail);
        }

        #endregion // end: other members

        #region IEnumerable

        [Theory]
        [InlineData("::", "::")]
        [InlineData("::", "::FF")]
        [InlineData("0.0.0.0", "0.0.0.0")]
        [InlineData("192.168.1.1", "192.168.1.1")]
        [InlineData("192.168.1.1", "192.168.1.5")]
        [InlineData("255.255.255.128", "255.255.255.255")]
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ff00", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        public void GetEnumerator_Test(string headAddressString, string tailAddressString)
        {
            // Arrange
            var head = IPAddress.Parse(headAddressString);
            var tail = IPAddress.Parse(tailAddressString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var ipAddressArray = iPAddressRange.ToArray();

            // Assert
            Assert.Equal(iPAddressRange.Count(), ipAddressArray.Length);
            Assert.Equal(head, ipAddressArray[0]);
            Assert.Equal(tail, ipAddressArray[ipAddressArray.Length - 1]);
        }

        [Theory]
        [InlineData("::", "::")]
        [InlineData("::", "::FF")]
        [InlineData("0.0.0.0", "0.0.0.0")]
        [InlineData("192.168.1.1", "192.168.1.1")]
        [InlineData("192.168.1.1", "192.168.1.5")]
        [InlineData("255.255.255.128", "255.255.255.255")]
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ff00", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        public void GetIEnumerableEnumerator_Test(string headAddressString, string tailAddressString)
        {
            // Arrange
            var head = IPAddress.Parse(headAddressString);
            var tail = IPAddress.Parse(tailAddressString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = new List<IPAddress>();
            var enumerator = ((IEnumerable)iPAddressRange).GetEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current as IPAddress);
            }

            // Assert
            Assert.Equal(iPAddressRange.Count(), result.Count);
            Assert.Equal(head, result[0]);
            Assert.Equal(tail, result[result.Count - 1]);
        }

        #endregion // end: IEnumerable

        #region IsSingleIP

        /// <summary>
        ///     Parameters: expected (bool), ipAddressRange (AbstractIPAddressRange)
        /// </summary>
        public static TheoryData<bool, AbstractIPAddressRange> IsSingleIP_Test_Data
        {
            get
            {
                var data = new TheoryData<bool, AbstractIPAddressRange>
                {
                    { true, CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Any) },
                    { true, CreateSubstituteIPAddressRange(IPAddress.IPv6Any, IPAddress.IPv6Any) },
                    { false, CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast) },
                    { false, CreateSubstituteIPAddressRange(IPAddress.IPv6Any, IPAddress.IPv6Loopback) },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsSingleIP_Test_Data))]
        public void IsSingleIP_Test(bool expected, AbstractIPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var isSingleIP = ipAddressRange.IsSingleIP;

            // Assert
            Assert.Equal(expected, isSingleIP);
        }

        #endregion // end: IsSingleIP

        #region Length / TryGetLength

        /// <summary>
        ///     Parameters: expected (BigInteger), ipAddressRange (AbstractIPAddressRange)
        /// </summary>
        public static TheoryData<BigInteger, AbstractIPAddressRange> Length_Test_Data
        {
            get
            {
                var data = new TheoryData<BigInteger, AbstractIPAddressRange>
                {
                    // single address
                    { new BigInteger(1), CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Any) },
                    { new BigInteger(1), CreateSubstituteIPAddressRange(IPAddress.IPv6Any, IPAddress.IPv6Any) },
                    // maximum length ipv4
                    {
                        BigInteger.Pow(2, 32),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("0.0.0.0"), IPAddress.Parse("255.255.255.255"))
                    },
                    // maximum length ipv6
                    {
                        BigInteger.Pow(2, 128),
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // ipv6 length at int.MaxValue
                    {
                        new BigInteger(int.MaxValue),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(int.MaxValue - 1))
                    },
                    // ipv6 length at long.MaxValue
                    {
                        new BigInteger(long.MaxValue),
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("::").Increment(long.MaxValue - 1)
                        )
                    },
                    // ipv6 length at int.MaxValue + 1
                    {
                        new BigInteger(int.MaxValue) + 1,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(int.MaxValue))
                    },
                    // ipv6 length at long.MaxValue + 1
                    {
                        new BigInteger(long.MaxValue) + 1,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(long.MaxValue))
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Length_Test_Data))]
        public static void Length_Test(BigInteger expected, AbstractIPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var result = ipAddressRange.Length;

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Length_Test_Data))]
        public static void TryGetLength_Integer_Test(BigInteger expected, AbstractIPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var success = ipAddressRange.TryGetLength(out int length);

            // Assert
            Assert.Equal(expected <= int.MaxValue, success);
            Assert.Equal(expected <= int.MaxValue ? (int)expected : -1, length);
        }

        [Theory]
        [MemberData(nameof(Length_Test_Data))]
        public static void TryGetLength_Long_Test(BigInteger expected, AbstractIPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var success = ipAddressRange.TryGetLength(out long length);

            // Assert
            Assert.Equal(expected <= long.MaxValue, success);
            Assert.Equal(expected <= long.MaxValue ? (long)expected : -1, length);
        }

        #endregion // end: Length / TryGetLength

        #region Contains

        #region Contains(IPAddress)

        [Theory]
        [InlineData(true, "::", "::ABCD", "::1234")] // IPv6 range contains IPv6
        [InlineData(false, "::", "::ABCD", "::FFFF")] // IPv6 range  doesn't contains IPv6
        [InlineData(true, "::", "::ABCD", "::")] // IPv6 range contains IPv6 head
        [InlineData(true, "::", "::ABCD", "::ABCD")] // IPv6 range contains IPv6 tail
        [InlineData(false, "::", "::ABCD", "192.168.1.1")] // IPv6 range doesn't contains IPv4
        [InlineData(false, "::", "::ABCD", null)] // IPv6 range doesn't contains null
        [InlineData(true, "192.168.0.1", "192.168.0.254", "192.168.0.128")] // IPv4 range contains IPv4
        [InlineData(false, "192.168.0.1", "192.168.0.254", "10.1.1.1")] // IPv4 range doesn't contains IPv4
        [InlineData(true, "192.168.0.1", "192.168.0.254", "192.168.0.1")] // IPv4 range contains IPv4 head
        [InlineData(true, "192.168.0.1", "192.168.0.254", "192.168.0.254")] // IPv4 range contains IPv4 tail
        [InlineData(false, "192.168.0.1", "192.168.0.254", "::")] // IPv4 range doesn't contains IPv6
        [InlineData(false, "192.168.0.1", "192.168.0.254", null)] // IPv4 range doesn't contains null
        public void ContainsIPAddress_Test(bool expected, string headString, string tailString, string addressString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);
            _ = IPAddress.TryParse(addressString, out var containsAddress);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.Contains(containsAddress);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Contains(IPAddress)

        #region Contains(IIPAddressRange)

        [Theory]
        [InlineData(true, "::", "::ABCD", "::", "::ABCD")] // IPv6 range contains self
        [InlineData(true, "::", "::ABCD", "::1", "::1234")] // IPv6 range contains internal IPv6 range
        [InlineData(false, "::", "::ABCD", "AA::FFFF", "AB::FFFF")] // IPv6 range doesn't contains external IPv6 range
        [InlineData(false, "A::", "A::ABCD", "::", "A::")] // IPv6 range  doesn't contains IPv6 overhang head
        [InlineData(false, "::", "::ABCD", "::", "AA::FFFF")] // IPv6 range  doesn't contains IPv6 overhang tail
        [InlineData(false, "::", "::ABCD", "192.168.1.1", "192.168.1.10")] // IPv6 range doesn't contains IPv4 range
        [InlineData(false, "::", "A::ABCD", null, null)] // IPv6 range  doesn't contain null range
        [InlineData(true, "128.0.0.1", "128.0.0.254", "128.0.0.1", "128.0.0.254")] // IPv4 range contains self
        [InlineData(true, "128.0.0.1", "128.0.0.254", "128.0.0.5", "128.0.0.100")] // IPv4 range contains internal IPv4 range
        [InlineData(false, "128.0.0.1", "128.0.0.254", "140.0.0.0", "145.0.0.0")] // IPv4 range doesn't contains external IPv4 range
        [InlineData(false, "128.0.0.1", "128.0.0.254", "", "128.0.0.100")] // IPv4 range doesn't contains null head IPv4 range
        [InlineData(false, "128.0.0.1", "128.0.0.254", "128.0.0.5", "")] // IPv4 range doesn't contains null tail IPv4 range
        [InlineData(false, "128.0.0.1", "128.0.0.254", "0.0.0.0", "128.0.0.100")] // IPv4 range  doesn't contains IPv4 overhang head
        [InlineData(false, "128.0.0.1", "128.0.0.254", "128.0.0.5", "145.0.0.0")] // IPv4 range  doesn't contains IPv4 overhang tail
        [InlineData(false, "128.0.0.1", "128.0.0.254", "::ABCD", "42::ABCD")] // IPv4 range doesn't contains IPv6 range
        [InlineData(false, "128.0.0.1", "128.0.0.254", null, null)] // IPv4 range  doesn't contain null range
        public void ContainsIPAddressRange_Test(
            bool expected,
            string headString,
            string tailString,
            string containsHeadString,
            string containsTailString
        )
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var substituteIPAddressSubRange =
                IPAddress.TryParse(containsHeadString, out var subhead)
                && IPAddress.TryParse(containsTailString, out var subtail)
                    ? Substitute.For<AbstractIPAddressRange>(subhead, subtail)
                    : null;

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            var containedIPAddressRange = substituteIPAddressSubRange;

            // Act
            var result = iPAddressRange.Contains(containedIPAddressRange);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Contains(IIPAddressRange)

        #endregion // end: Contains

        #region Class

        [Fact]
        public void Implementation_Test()
        {
            // Arrange
            var type = typeof(AbstractIPAddressRange);

            // Act
            // Assert
            Assert.True(typeof(IIPAddressRange).IsAssignableFrom(type));
        }

        [Fact]
        public void AbstractClass_Test()
        {
            // Arrange
            var type = typeof(AbstractIPAddressRange);

            // Act
            var isAbstract = type.IsAbstract;

            // Assert
            Assert.True(isAbstract);
        }

        #endregion // end: Class

        #region IEnumerable / IEnumerable<IPAddress>

        [Fact] //Test that the expected addresses appear in the given IPv6 range
        public void Enumerable_IPv6_ContainsExpected_Test()
        {
            // Arrange
            const string headString = "::a";
            const string tailString = "::c";
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { head, IPAddress.Parse("::b"), tail }, result);
        }

        [Fact]
        public void Enumerable_IPv6_TakePastEnd_Test()
        {
            // Arrange
            var head = IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:fff0");
            var tail = IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.Take(100).ToList();

            // Assert
            Assert.Equal(16, result.Count);
            Assert.Equal(tail, result[result.Count - 1]);
        }

        [Fact]
        public void Enumerable_IPv4_TakePastEnd_Test()
        {
            // Arrange
            var head = IPAddress.Parse("255.255.255.240");
            var tail = IPAddress.Parse("255.255.255.255");

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.Take(100).ToList();

            // Assert
            Assert.Equal(16, result.Count);
            Assert.Equal(tail, result[result.Count - 1]);
        }

        [Fact] // Test that the expected addresses appear in the given IPv4 range
        public void Enumerable_IPv4_ContainsExpected_Test()
        {
            // Arrange
            const string headString = "192.168.1.1";
            const string tailString = "192.168.1.3";
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { head, IPAddress.Parse("192.168.1.2"), tail }, result);
        }

        [Fact] // Test that a range of one returns a single address when Addresses is called
        public void Enumerable_SameHead_And_SameTail_ReturnsSingle_Test()
        {
            // Arrange
            var ipAddress = IPAddress.Parse("192.168.1.1");

            var iPAddressRange = CreateSubstituteIPAddressRange(ipAddress, ipAddress);

            // Act
            var addresses = iPAddressRange.ToArray();

            // Assert
            Assert.Single(addresses);
            Assert.Equal(ipAddress, addresses.Single());
        }

        [Fact]
        public void Enumerable_ReasonableIteration_Test()
        {
            // Arrange
            var iPAddressRange = CreateSubstituteIPAddressRange(
                IPAddress.Parse("::"),
                IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
            );

            // Act
            var addresses = iPAddressRange.Skip(10).Take(10).ToArray();

            // Assert
            Assert.NotNull(addresses); // if this test completed it likely means that we didn't iterate through 2^128 ip addresses to skip 10 and take 10
        }

        #endregion // end: IEnumerable / IEnumerable<IPAddress>

        #region Ctor

        [Theory]
        [InlineData("192.168.1.1", "192.168.1.5")]
        [InlineData("::beef", "::dead")]
        public void Ctor_HappyPath_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Assert
            Assert.Equal(head, iPAddressRange.Head);
            Assert.Equal(tail, iPAddressRange.Tail);
        }

        [Theory]
        [InlineData("192.168.1.1", null)]
        [InlineData(null, "192.168.1.5")]
        [InlineData(null, null)]
        public void Ctor_Null_Input_Throws_ArgumentNullException_Test(string headString, string tailString)
        {
            // Arrange
            var head = headString != null ? IPAddress.Parse(headString) : null;

            var tail = tailString != null ? IPAddress.Parse(tailString) : null;

            // Act
            // Assert
            var exception = Assert.ThrowsAny<Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            Assert.IsAssignableFrom<ArgumentNullException>(exception.InnerException);
        }

        [Theory]
        [InlineData("192.168.1.1", "::beef")]
        [InlineData("::beef", "192.168.1.5")]
        public void Ctor_MismatchAddressFamilies_Throws_InvalidOperationException_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            // Assert
            var exception = Assert.ThrowsAny<Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            var inner = Assert.IsAssignableFrom<InvalidOperationException>(exception.InnerException);
            Assert.Contains("matching address families", inner.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("192.168.1.5", "192.168.1.1")]
        [InlineData("::dead", "::beef")]
        public void Ctor_TailBeforeHead_Throws_InvalidOperationException_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            // Assert
            var exception = Assert.ThrowsAny<Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            var inner = Assert.IsAssignableFrom<InvalidOperationException>(exception.InnerException);
            Assert.Contains("greater or equal", inner.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion // end: Ctor

        #region AddressFamily

        [Fact]
        public void AddressFamily_IPv4_Test()
        {
            // Arrange
            const string headString = "192.168.1.1";
            const string tailString = "192.168.1.5";

            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            // Assert
            Assert.Equal(AddressFamily.InterNetwork, iPAddressRange.AddressFamily);
            Assert.True(iPAddressRange.IsIPv4);
            Assert.False(iPAddressRange.IsIPv6);
        }

        [Fact]
        public void AddressFamily_IPv6_Test()
        {
            // Arrange
            const string headString = "::beef";
            const string tailString = "::dead";

            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            // Assert
            Assert.Equal(AddressFamily.InterNetworkV6, iPAddressRange.AddressFamily);
            Assert.False(iPAddressRange.IsIPv4);
            Assert.True(iPAddressRange.IsIPv6);
        }

        #endregion // end: AddressFamily

        #region Formatting

        [Theory]
        [InlineData("192.168.1.1", "192.168.1.5", null, "192.168.1.1 - 192.168.1.5")]
        [InlineData("192.168.1.1", "192.168.1.5", "", "192.168.1.1 - 192.168.1.5")]
        [InlineData("192.168.1.1", "192.168.1.5", "g", "192.168.1.1 - 192.168.1.5")]
        [InlineData("192.168.1.1", "192.168.1.5", "G", "192.168.1.1 - 192.168.1.5")]
        [InlineData("::beef", "::dead", null, "::beef - ::dead")]
        [InlineData("::beef", "::dead", "", "::beef - ::dead")]
        [InlineData("::beef", "::dead", "g", "::beef - ::dead")]
        [InlineData("::beef", "::dead", "G", "::beef - ::dead")]
        public void ToString_Format_ReturnsHeadDashTail_Test(
            string headString,
            string tailString,
            string format,
            string expected
        )
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);
            var iPAddressRange = new IPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.ToString(format, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToString_NoArgs_ReturnsHeadDashTail_Test()
        {
            // Arrange
            var head = IPAddress.Parse("10.0.0.1");
            var tail = IPAddress.Parse("10.0.0.255");
            var iPAddressRange = new IPAddressRange(head, tail);

            // Act
            var result = iPAddressRange.ToString();

            // Assert
            Assert.Equal("10.0.0.1 - 10.0.0.255", result);
        }

        [Theory]
        [InlineData("192.168.1.1", "192.168.1.5", "X")]
        [InlineData("192.168.1.1", "192.168.1.5", "R")]
        [InlineData("::beef", "::dead", "Z")]
        public void ToString_UnknownFormat_Throws_FormatException_Test(string headString, string tailString, string format)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);
            var iPAddressRange = new IPAddressRange(head, tail);

            // Act
            // Assert
            Assert.Throws<FormatException>(() => iPAddressRange.ToString(format, CultureInfo.InvariantCulture));
        }

        #endregion // end: Formatting

        #region Set Operations

        #region Contains

        #region Contains IIPAddressRange

        /// <summary>
        ///     Parameters: expected (bool), left (IIPAddressRange), right (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IIPAddressRange> Contains_IIPAddressRange_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IIPAddressRange>
                {
                    // null overlap checking
                    { false, ipv4Range, null },
                    { false, ipv6Range, null },
                    // same overlap checking
                    { true, ipv4Range, ipv4Range },
                    { true, ipv6Range, ipv6Range },
                    // equal overlap checking
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast),
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast)
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // differing address families
                    { false, ipv4Range, ipv6Range },
                    { false, ipv6Range, ipv4Range },
                    // head only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.128")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff"))
                    },
                    // full head and tail overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("ff::ff00"))
                    },
                    // tail only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.192"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff"))
                    },
                    // not touching
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.128")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.0"), IPAddress.Parse("10.1.1.100"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ab::"), IPAddress.Parse("ab::f")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ef::"), IPAddress.Parse("ef::f"))
                    },
                    // disparate ranges
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.2.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Contains_IIPAddressRange_Test_Data))]
        public void Contains_IIPAddressRange_Test(bool expected, IIPAddressRange left, IIPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.Contains(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Contains IIPAddressRange

        #region Contains IPAddress

        /// <summary>
        ///     Parameters: expected (bool), range (IIPAddressRange), address (IPAddress)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IPAddress> Contains_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IPAddress>
                {
                    // does not contain null
                    { false, ipv4Range, null },
                    { false, ipv6Range, null },
                    // differing address families
                    { false, ipv4Range, IPAddress.IPv6Any },
                    { false, ipv6Range, IPAddress.Any },
                    // contains head
                    { true, ipv4Range, ipv4Range.Head },
                    { true, ipv6Range, ipv6Range.Head },
                    // contains tail
                    { true, ipv4Range, ipv4Range.Tail },
                    { true, ipv6Range, ipv6Range.Tail },
                    // does not contain outside before
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.200")),
                        IPAddress.Parse("192.168.1.0")
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff01"), IPAddress.Parse("::ff08")),
                        IPAddress.Parse("::ff00")
                    },
                    // does not contain outside after
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.200")),
                        IPAddress.Parse("192.168.1.201")
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff01"), IPAddress.Parse("::ff08")),
                        IPAddress.Parse("::ff09")
                    },
                };

                // contains all inside IPv4
                var ipv4InsideRange = CreateSubstituteIPAddressRange(
                    IPAddress.Parse("192.168.1.0"),
                    IPAddress.Parse("192.168.1.5")
                );
                foreach (var ip in ipv4InsideRange)
                {
                    data.Add(true, ipv4InsideRange, ip);
                }

                // contains all inside IPv6
                var ipv6InsideRange = CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("::ff0f"));
                foreach (var ip in ipv6InsideRange)
                {
                    data.Add(true, ipv6InsideRange, ip);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Contains_Test_Data))]
        public void Contains_Test(bool expected, IIPAddressRange range, IPAddress address)
        {
            // Arrange
            // Act
            var result = range.Contains(address);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Contains IPAddress

        #endregion // end: Contains

        #region Overlap and Touches

        #region HeadOverlappedBy

        /// <summary>
        ///     Parameters: expected (bool), left (IIPAddressRange), right (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IIPAddressRange> HeadOverlappedBy_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IIPAddressRange>
                {
                    // null overlap checking
                    { false, ipv4Range, null },
                    { false, ipv6Range, null },
                    // same overlap checking
                    { true, ipv4Range, ipv4Range },
                    { true, ipv6Range, ipv6Range },
                    // equal overlap checking
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast),
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast)
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // differing address families
                    { false, ipv4Range, ipv6Range },
                    { false, ipv6Range, ipv4Range },
                    // head only overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.128"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff"))
                    },
                    // full head and tail overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("ff::ff00")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff"))
                    },
                    // tail only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.192"), IPAddress.Parse("192.168.1.255"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff"))
                    },
                    // not touching
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.0"), IPAddress.Parse("10.1.1.100")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.128"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ef::"), IPAddress.Parse("ef::f")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ab::"), IPAddress.Parse("ab::f"))
                    },
                    // disparate ranges
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.2.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(HeadOverlappedBy_Test_Data))]
        public void HeadOverlappedBy_Test(bool expected, IIPAddressRange left, IIPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.HeadOverlappedBy(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: HeadOverlappedBy

        #region TailOverlappedBy

        /// <summary>
        ///     Parameters: expected (bool), left (IIPAddressRange), right (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IIPAddressRange> TailOverlappedBy_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IIPAddressRange>
                {
                    // null overlap checking
                    { false, ipv4Range, null },
                    { false, ipv6Range, null },
                    // same overlap checking
                    { true, ipv4Range, ipv4Range },
                    { true, ipv6Range, ipv6Range },
                    // equal overlap checking
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast),
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast)
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // differing address families
                    { false, ipv4Range, ipv6Range },
                    { false, ipv6Range, ipv4Range },
                    // head only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.128"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff"))
                    },
                    // full head and tail overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("ff::ff00")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff"))
                    },
                    // tail only overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.192"), IPAddress.Parse("192.168.1.255"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff"))
                    },
                    // not touching
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.0"), IPAddress.Parse("10.1.1.100")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.128"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ef::"), IPAddress.Parse("ef::f")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ab::"), IPAddress.Parse("ab::f"))
                    },
                    // disparate ranges
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.2.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(TailOverlappedBy_Test_Data))]
        public void TailOverlappedBy_Test(bool expected, IIPAddressRange left, IIPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.TailOverlappedBy(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: TailOverlappedBy

        #region Overlaps

        /// <summary>
        ///     Parameters: expected (bool), left (IIPAddressRange), right (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IIPAddressRange> Overlaps_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IIPAddressRange>
                {
                    // null overlap checking
                    { false, ipv4Range, null },
                    { false, ipv6Range, null },
                    // same overlap checking
                    { true, ipv4Range, ipv4Range },
                    { true, ipv6Range, ipv6Range },
                    // equal overlap checking
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast),
                        CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast)
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // differing address families
                    { false, ipv4Range, ipv6Range },
                    { false, ipv6Range, ipv4Range },
                    // head only overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.128")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff"))
                    },
                    // full head and tail overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("ff::ff00"))
                    },
                    // tail only overlapped
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.192"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff"))
                    },
                    // not touching
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.128")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.0"), IPAddress.Parse("10.1.1.100"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ab::"), IPAddress.Parse("ab::f")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("ef::"), IPAddress.Parse("ef::f"))
                    },
                    // disparate ranges
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.2.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                    // wholly contained — both directions must return true (symmetry)
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.64"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.64"), IPAddress.Parse("192.168.1.192")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("1::"), IPAddress.Parse("fffe::ffff"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("1::"), IPAddress.Parse("fffe::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff"))
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Overlaps_Test_Data))]
        public void Overlaps_Test(bool expected, IIPAddressRange left, IIPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.Overlaps(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Overlaps

        #region Touches

        /// <summary>
        ///     Parameters: expected (bool), left (IIPAddressRange), right (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, IIPAddressRange, IIPAddressRange> Touches_Test_Data
        {
            get
            {
                var ipv4Range = CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast);
                var ipv6Range = CreateSubstituteIPAddressRange(
                    IPAddress.IPv6Any,
                    IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                );

                var data = new TheoryData<bool, IIPAddressRange, IIPAddressRange>
                {
                    // null overlap checking
                    { false, CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Broadcast), null },
                    {
                        false,
                        CreateSubstituteIPAddressRange(
                            IPAddress.IPv6Any,
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        null
                    },
                    // differing address families
                    { false, ipv4Range, ipv6Range },
                    { false, ipv6Range, ipv4Range },
                    // left tail touches right head
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.100")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.101"), IPAddress.Parse("192.168.1.200"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::abcd")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::abce"), IPAddress.Parse("::ffff"))
                    },
                    // left head touches right tail
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.101"), IPAddress.Parse("192.168.1.200")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.100"))
                    },
                    {
                        true,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::abce"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::abcd"))
                    },
                    // head only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.128")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff"))
                    },
                    // full head and tail overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.128"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ff00"), IPAddress.Parse("ff::ff00"))
                    },
                    // tail only overlapped
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.192"), IPAddress.Parse("192.168.1.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.192"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::ffff"), IPAddress.Parse("1::ffff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ffff"))
                    },
                    // disparate ranges
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.2.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::ff")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                    // this tail at max
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("255.255.255.255")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        ),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::"))
                    },
                    // that tail at max
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("10.1.1.1"), IPAddress.Parse("10.1.5.0")),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("255.255.255.255"))
                    },
                    {
                        false,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("f::"), IPAddress.Parse("f:1::")),
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                };
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Touches_Test_Data))]
        public void Touches_Test(bool expected, IIPAddressRange left, IIPAddressRange right)
        {
            // Arrange
            // Act
            var result = left.Touches(right);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Touches

        #endregion // end: Overlap and Touches

        #endregion // end: Set Operations

        #region Contains Any/All Public/Private Addresses

        /// <summary>
        ///     Parameters: expectedHasPublic (bool), expectedHasPrivate (bool), range (IIPAddressRange)
        /// </summary>
        public static TheoryData<bool, bool, IIPAddressRange> ContainsPublicPrivate_Data
        {
            get
            {
                var data = new TheoryData<bool, bool, IIPAddressRange>();

                // known private ranges
                foreach (var subnet in SubnetUtilities.PrivateIPAddressRangesList)
                {
                    // has private
                    data.Add(false, true, CreateSubstituteIPAddressRange(subnet.Head, subnet.Tail)); // on the border of private
                    data.Add(false, true, CreateSubstituteIPAddressRange(subnet.Head.Increment(2), subnet.Tail.Increment(-2))); // wholly inside of private
                    data.Add(false, true, CreateSubstituteIPAddressRange(subnet.Head.Increment(2), subnet.Tail)); // partially within of private
                    data.Add(false, true, CreateSubstituteIPAddressRange(subnet.Head, subnet.Tail.Increment(-2))); // partially within of private

                    // has private and public
                    data.Add(true, true, CreateSubstituteIPAddressRange(subnet.Head.Increment(-2), subnet.Tail)); // partially outside of private
                    data.Add(true, true, CreateSubstituteIPAddressRange(subnet.Head, subnet.Tail.Increment(2))); // partially outside of private
                }

                // public only
                var publicSpace = new (IPAddress head, IPAddress tail)[]
                {
                    (IPAddress.Parse("128.64.32.0"), IPAddress.Parse("128.64.32.16")),
                    (IPAddress.Parse("FFFF:7FFF:3FFF::"), IPAddress.Parse("FFFF:7FFF:3FFF:1FFF::")),
                };

                foreach (var (head, tail) in publicSpace)
                {
                    data.Add(true, false, CreateSubstituteIPAddressRange(head, tail)); // on the border of public
                    data.Add(true, false, CreateSubstituteIPAddressRange(head.Increment(2), tail.Increment(-2))); // wholly inside public
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAnyPrivateAddresses_Test(bool expectedHasPublic, bool expectedHasPrivate, IIPAddressRange range)
        {
            // Arrange
            // Act
            var result = range.ContainsAnyPrivateAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            // Assert
            Assert.Equal(expectedHasPrivate, result);
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAllPrivateAddresses_Test(bool expectedHasPublic, bool expectedHasPrivate, IIPAddressRange range)
        {
            // Arrange
            // Act
            var result = range.ContainsAllPrivateAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            // Assert
            Assert.Equal(expectedHasPrivate && !expectedHasPublic, result);
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAnyPublicAddresses_Test(bool expectedHasPublic, bool expectedHasPrivate, IIPAddressRange range)
        {
            // Arrange
            // Act
            var result = range.ContainsAnyPublicAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            // Assert
            Assert.Equal(expectedHasPublic, result);
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAllPublicAddresses_Test(bool expectedHasPublic, bool expectedHasPrivate, IIPAddressRange range)
        {
            // Arrange
            // Act
            var result = range.ContainsAllPublicAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            // Assert
            Assert.Equal(!expectedHasPrivate && expectedHasPublic, result);
        }

        [Fact]
        public void ContainsAnyPrivateAddresses_RangeSpansPrivateBlock_WithPublicEndpoints_Test()
        {
            // Regression: endpoints 11.0.0.0 and 173.0.0.0 are both public, but the range
            // spans 172.16.0.0/12 — the endpoint heuristic incorrectly returned false.
            var range = CreateSubstituteIPAddressRange(IPAddress.Parse("11.0.0.0"), IPAddress.Parse("173.0.0.0"));

            Assert.True(range.ContainsAnyPrivateAddresses());
        }

        [Fact]
        public void ContainsAllPublicAddresses_RangeSpansPrivateBlock_WithPublicEndpoints_Test()
        {
            // Regression: endpoints are both public, but the range spans 172.16.0.0/12 —
            // not all addresses are public, so this must return false.
            var range = CreateSubstituteIPAddressRange(IPAddress.Parse("11.0.0.0"), IPAddress.Parse("173.0.0.0"));

            Assert.False(range.ContainsAllPublicAddresses());
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAnyPrivateAddresses_IsInverseOf_ContainsAllPublicAddresses_Test(
            bool expectedHasPublic,
            bool expectedHasPrivate,
            IIPAddressRange range
        )
        {
            // ContainsAnyPrivateAddresses and ContainsAllPublicAddresses must be logical inverses.
            var hasPrivate = range.ContainsAnyPrivateAddresses();
            var allPublic = range.ContainsAllPublicAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            Assert.NotEqual(hasPrivate, allPublic);
        }

        [Theory]
        [MemberData(nameof(ContainsPublicPrivate_Data))]
        public void ContainsAllPrivateAddresses_IsInverseOf_ContainsAnyPublicAddresses_Test(
            bool expectedHasPublic,
            bool expectedHasPrivate,
            IIPAddressRange range
        )
        {
            // ContainsAllPrivateAddresses and ContainsAnyPublicAddresses must be logical inverses.
            var allPrivate = range.ContainsAllPrivateAddresses();
            var hasPublic = range.ContainsAnyPublicAddresses();

            this._testOutputHelper.WriteLine($"pr:{expectedHasPrivate} pu:{expectedHasPublic} {range.Head} - {range.Tail}");

            Assert.NotEqual(allPrivate, hasPublic);
        }

        #endregion // end: Contains Any/All Public/Private Addresses
    }
}
