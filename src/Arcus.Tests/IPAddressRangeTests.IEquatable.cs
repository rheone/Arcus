using System.Net;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="IPAddressRange"/> tests for <see cref="System.IEquatable{T}"/>
    /// </content>
    public partial class IPAddressRangeTests
    {
        #region Equals

        #region Equals(IPAddressRange)

        /// <summary>Verifies Equals(IPAddressRange) returns the expected result for equal, null, and differing ranges.</summary>
        /// <param name="expected">Expected result.</param>
        /// <param name="xHead">The head address of the left range.</param>
        /// <param name="xTail">The tail address of the left range.</param>
        /// <param name="yHead">The head address of the right range, or null.</param>
        /// <param name="yTail">The tail address of the right range, or null.</param>
        [Theory]
        [InlineData(true, "192.168.1.1", "192.168.1.10", "192.168.1.1", "192.168.1.10")]
        [InlineData(false, "192.168.1.1", "192.168.1.5", null, null)]
        [InlineData(false, "192.168.1.1", "192.168.1.10", "192.168.1.1", "192.168.1.11")]
        [InlineData(false, "12.168.1.1", "12.168.1.10", "192.18.1.1", "192.18.1.11")]
        public void Equals_IPAddressRange_Test(bool expected, string xHead, string xTail, string yHead, string yTail)
        {
            // Arrange
            _ = IPAddress.TryParse(xHead, out var xHeadAddress);
            _ = IPAddress.TryParse(xTail, out var xTailAddress);
            var xAddressRange = new IPAddressRange(xHeadAddress, xTailAddress);

            var yAddressRange =
                IPAddress.TryParse(yHead, out var yHeadAddress) && IPAddress.TryParse(yTail, out var yTailAddress)
                    ? new IPAddressRange(yHeadAddress, yTailAddress)
                    : null;

            // Act
            var result = xAddressRange.Equals(yAddressRange);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Equals(IPAddressRange)

        #region Equals(object)

        /// <summary>Verifies Equals(object) returns the expected result when the argument is boxed as object.</summary>
        /// <param name="expected">Expected result.</param>
        /// <param name="xHead">The head address of the left range.</param>
        /// <param name="xTail">The tail address of the left range.</param>
        /// <param name="yHead">The head address of the right range, or null.</param>
        /// <param name="yTail">The tail address of the right range, or null.</param>
        [Theory]
        [InlineData(true, "192.168.1.1", "192.168.1.10", "192.168.1.1", "192.168.1.10")]
        [InlineData(false, "192.168.1.1", "192.168.1.5", null, null)]
        [InlineData(false, "192.168.1.1", "192.168.1.10", "192.168.1.1", "192.168.1.11")]
        [InlineData(false, "12.168.1.1", "12.168.1.10", "192.18.1.1", "192.18.1.11")]
        public void Equals_Object_Test(bool expected, string xHead, string xTail, string yHead, string yTail)
        {
            // Arrange
            _ = IPAddress.TryParse(xHead, out var xHeadAddress);
            _ = IPAddress.TryParse(xTail, out var xTailAddress);
            var xAddressRange = new IPAddressRange(xHeadAddress, xTailAddress);

            var yAddressRange =
                IPAddress.TryParse(yHead, out var yHeadAddress) && IPAddress.TryParse(yTail, out var yTailAddress)
                    ? new IPAddressRange(yHeadAddress, yTailAddress)
                    : null;

            // Act
            var result = xAddressRange.Equals((object)yAddressRange);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Equals(object)

        #region GetHashCode

        /// <summary>Verifies equal ranges produce the same hash code and differing ranges produce different hash codes.</summary>
        /// <param name="expected">Whether the two hash codes should be equal.</param>
        /// <param name="xHead">The head address of the left range.</param>
        /// <param name="xTail">The tail address of the left range.</param>
        /// <param name="yHead">The head address of the right range.</param>
        /// <param name="yTail">The tail address of the right range.</param>
        [Theory]
        [InlineData(true, "192.168.1.5", "192.168.1.100", "192.168.1.5", "192.168.1.100")]
        [InlineData(false, "192.168.1.5", "192.168.1.100", "10.168.1.0", "10.168.1.100")]
        [InlineData(true, "::abcd", "ff:12::abcd", "::abcd", "ff:12::abcd")]
        [InlineData(false, "::abcd", "ff:12::abcd", "::ef", "ff:12::1234")]
        public void GetHashCode_Test(bool expected, string xHead, string xTail, string yHead, string yTail)
        {
            // Arrange
            _ = IPAddress.TryParse(xHead, out var xHeadAddress);
            _ = IPAddress.TryParse(xTail, out var xTailAddress);

            var xAddressRange = new IPAddressRange(xHeadAddress, xTailAddress);

            _ = IPAddress.TryParse(yHead, out var yHeadAddress);
            _ = IPAddress.TryParse(yTail, out var yTailAddress);

            var yAddressRange = new IPAddressRange(yHeadAddress, yTailAddress);

            // Act
            var xHash = xAddressRange.GetHashCode();
            var yHash = yAddressRange.GetHashCode();
            var result = xHash.Equals(yHash);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: GetHashCode

        #endregion // end: Equals
    }
}
