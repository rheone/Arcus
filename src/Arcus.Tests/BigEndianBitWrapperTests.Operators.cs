using Xunit;

namespace Arcus.Tests
{
    public partial class BigEndianBitWrapperTests
    {
        #region Bitwise AND

        [Fact]
        public void BitwiseAnd_AddressAndMask_ReturnsNetworkAddress_Test()
        {
            // Arrange: 192.168.1.100 & 255.255.255.0 = 192.168.1.0
            var address = WrapIPv4("192.168.1.100");
            var mask = BigEndianBitWrapper.CreateMask(4, 24);

            // Act
            var network = address & mask;

            // Assert
            Assert.Equal(WrapIPv4("192.168.1.0"), network);
        }

        [Fact]
        public void BitwiseAnd_WithAllZerosMask_ReturnsZero_Test()
        {
            // Arrange
            var address = WrapIPv4("10.20.30.40");
            var zero = BigEndianBitWrapper.CreateMask(4, 0); // all zeros

            // Act
            var result = address & zero;

            // Assert
            Assert.Equal(WrapIPv4("0.0.0.0"), result);
        }

        [Fact]
        public void BitwiseAnd_WithAllOnesMask_ReturnsSameValue_Test()
        {
            // Arrange
            var address = WrapIPv4("10.20.30.40");
            var allOnes = BigEndianBitWrapper.CreateMask(4, 32); // all ones

            // Act
            var result = address & allOnes;

            // Assert
            Assert.Equal(address, result);
        }

        #endregion // end: Bitwise AND

        #region Bitwise OR

        [Fact]
        public void BitwiseOr_AddressOrInverseMask_ReturnsBroadcastAddress_Test()
        {
            // Arrange: 192.168.1.0 | ~255.255.255.0 = 192.168.1.255
            var network = WrapIPv4("192.168.1.0");
            var inverseMask = ~BigEndianBitWrapper.CreateMask(4, 24);

            // Act
            var broadcast = network | inverseMask;

            // Assert
            Assert.Equal(WrapIPv4("192.168.1.255"), broadcast);
        }

        [Fact]
        public void BitwiseOr_WithAllZeros_ReturnsSameValue_Test()
        {
            // Arrange
            var address = WrapIPv4("10.20.30.40");
            var zero = BigEndianBitWrapper.CreateMask(4, 0);

            // Act
            var result = address | zero;

            // Assert
            Assert.Equal(address, result);
        }

        [Fact]
        public void BitwiseOr_WithAllOnes_ReturnsAllOnes_Test()
        {
            // Arrange
            var address = WrapIPv4("10.20.30.40");
            var allOnes = BigEndianBitWrapper.CreateMask(4, 32);

            // Act
            var result = address | allOnes;

            // Assert
            Assert.Equal(WrapIPv4("255.255.255.255"), result);
        }

        #endregion // end: Bitwise OR

        #region Bitwise NOT

        [Fact]
        public void BitwiseNot_Mask24_ReturnsInverseMask_Test()
        {
            // Arrange: ~255.255.255.0 = 0.0.0.255
            var mask = BigEndianBitWrapper.CreateMask(4, 24);

            // Act
            var inverse = ~mask;

            // Assert
            Assert.Equal(WrapIPv4("0.0.0.255"), inverse);
            Assert.Equal(4, inverse.ByteWidth);
        }

        [Fact]
        public void BitwiseNot_AllZeros_ReturnsMaxForWidth_Test()
        {
            // Arrange
            var zeros = BigEndianBitWrapper.CreateMask(4, 0); // 0.0.0.0

            // Act
            var result = ~zeros;

            // Assert
            Assert.Equal(WrapIPv4("255.255.255.255"), result);
        }

        [Fact]
        public void BitwiseNot_AllOnes_ReturnsZero_Test()
        {
            // Arrange
            var allOnes = BigEndianBitWrapper.CreateMask(4, 32);

            // Act
            var result = ~allOnes;

            // Assert
            Assert.Equal(WrapIPv4("0.0.0.0"), result);
        }

        [Fact]
        public void BitwiseNot_IPv6Mask64_ReturnsCorrectInverse_Test()
        {
            // Arrange: ffff:ffff:ffff:ffff:: → NOT → ::ffff:ffff:ffff:ffff
            var mask = BigEndianBitWrapper.CreateMask(16, 64);

            // Act
            var inverse = ~mask;

            // Assert
            var expected = WrapIPv6("::ffff:ffff:ffff:ffff");
            Assert.Equal(expected, inverse);
        }

        [Fact]
        public void BitwiseNot_DoesNotAffectByteWidth_Test()
        {
            // Arrange
            var w = WrapIPv4("0.0.0.0");

            // Act
            var result = ~w;

            // Assert — byte width unchanged
            Assert.Equal(w.ByteWidth, result.ByteWidth);
        }

        #endregion // end: Bitwise NOT

        #region Operators: < > <= >=

        [Theory]
        [MemberData(nameof(CompareTo_Test_Data))]
        public void LessThan_Operator_ReturnsExpected_Test(string leftStr, string rightStr, int expectedSign)
        {
            // Arrange
            var left = WrapIPv4(leftStr);
            var right = WrapIPv4(rightStr);

            // Act / Assert
            Assert.Equal(expectedSign < 0, left < right);
        }

        [Theory]
        [MemberData(nameof(CompareTo_Test_Data))]
        public void GreaterThan_Operator_ReturnsExpected_Test(string leftStr, string rightStr, int expectedSign)
        {
            // Arrange
            var left = WrapIPv4(leftStr);
            var right = WrapIPv4(rightStr);

            // Act / Assert
            Assert.Equal(expectedSign > 0, left > right);
        }

        [Theory]
        [MemberData(nameof(CompareTo_Test_Data))]
        public void LessThanOrEqual_Operator_ReturnsExpected_Test(string leftStr, string rightStr, int expectedSign)
        {
            // Arrange
            var left = WrapIPv4(leftStr);
            var right = WrapIPv4(rightStr);

            // Act / Assert
            Assert.Equal(expectedSign <= 0, left <= right);
        }

        [Theory]
        [MemberData(nameof(CompareTo_Test_Data))]
        public void GreaterThanOrEqual_Operator_ReturnsExpected_Test(string leftStr, string rightStr, int expectedSign)
        {
            // Arrange
            var left = WrapIPv4(leftStr);
            var right = WrapIPv4(rightStr);

            // Act / Assert
            Assert.Equal(expectedSign >= 0, left >= right);
        }

        #endregion // end: Operators: < > <= >=
    }
}
