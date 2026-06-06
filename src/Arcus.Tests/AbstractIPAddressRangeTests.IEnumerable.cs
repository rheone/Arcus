using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> tests for <see cref="IEnumerable{T}"/>
    /// </content>
    public partial class AbstractIPAddressRangeTests
    {
        #region IEnumerable

        /// <summary>Verifies the generic enumerator yields every address from head to tail inclusive.</summary>
        /// <param name="headAddressString">The head IP address string for the range.</param>
        /// <param name="tailAddressString">The tail IP address string for the range.</param>
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

        /// <summary>Verifies the non-generic IEnumerable enumerator yields every address from head to tail inclusive.</summary>
        /// <param name="headAddressString">The head IP address string for the range.</param>
        /// <param name="tailAddressString">The tail IP address string for the range.</param>
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

        #region IEnumerable / IEnumerable<IPAddress>

        /// <summary>Verifies that enumerating an IPv6 range yields exactly the expected addresses.</summary>
        [Fact]
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

        /// <summary>Verifies that taking more elements than the IPv6 range contains stops at the tail without error.</summary>
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

        /// <summary>Verifies that taking more elements than the IPv4 range contains stops at the tail without error.</summary>
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

        /// <summary>Verifies that enumerating an IPv4 range yields exactly the expected addresses.</summary>
        [Fact]
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

        /// <summary>Verifies that a range whose head and tail are the same address enumerates to exactly one element.</summary>
        [Fact]
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

        /// <summary>Verifies that skipping and taking within a huge IPv6 range completes in finite time (lazy enumeration).</summary>
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
    }
}
