using Xunit;

namespace Arcus.Tests
{
    public partial class BigEndianBitWrapperTests
    {
        #region CompareTo

        public static TheoryData<string, string, int> CompareTo_Test_Data =>
            new()
            {
                // IPv4
                { "10.0.0.1", "10.0.0.1", 0 },
                { "10.0.0.1", "10.0.0.2", -1 },
                { "10.0.0.2", "10.0.0.1", 1 },
                { "0.0.0.0", "255.255.255.255", -1 },
                { "255.255.255.255", "0.0.0.0", 1 },
                { "192.168.1.0", "192.168.1.255", -1 },
            };

        [Theory]
        [MemberData(nameof(CompareTo_Test_Data))]
        public void CompareTo_IPv4Values_ReturnsExpectedSign_Test(string leftStr, string rightStr, int expectedSign)
        {
            // Arrange
            var left = WrapIPv4(leftStr);
            var right = WrapIPv4(rightStr);

            // Act
            var result = left.CompareTo(right);

            // Assert: only the sign matters
            Assert.Equal(expectedSign, System.Math.Sign(result));
        }

        [Fact]
        public void CompareTo_IPv6_AscendingOrder_Test()
        {
            // Arrange
            var zero = WrapIPv6("::");
            var one = WrapIPv6("::1");
            var max = WrapIPv6("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

            // Act / Assert
            Assert.True(zero.CompareTo(one) < 0);
            Assert.True(one.CompareTo(max) < 0);
            Assert.Equal(0, zero.CompareTo(zero));
        }

        #endregion // end: CompareTo
    }
}
