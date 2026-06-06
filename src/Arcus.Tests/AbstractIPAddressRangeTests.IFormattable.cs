using System;
using System.Globalization;
using System.Net;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> tests for <see cref="IFormattable"/>
    /// </content>
    public partial class AbstractIPAddressRangeTests
    {
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
    }
}
