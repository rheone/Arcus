using Xunit;

namespace Arcus.Tests
{
    public partial class BigEndianBitWrapperTests
    {
        #region Equals / == / !=

        [Fact]
        public void Equals_SameValueAndWidth_ReturnsTrue_Test()
        {
            // Arrange
            var a = WrapIPv4("192.168.1.1");
            var b = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse_Test()
        {
            // Arrange
            var a = WrapIPv4("192.168.1.1");
            var b = WrapIPv4("192.168.1.2");

            // Act / Assert
            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void Equals_DifferentByteWidth_ReturnsFalse_Test()
        {
            // Arrange: same numeric value 1, but different byte widths
            var ipv4 = BigEndianBitWrapper.FromBytes([0, 0, 0, 1]);
            var ipv6 = BigEndianBitWrapper.FromBytes([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);

            // Act / Assert
            Assert.False(ipv4.Equals(ipv6));
            Assert.False(ipv4 == ipv6);
            Assert.True(ipv4 != ipv6);
        }

        [Fact]
        public void Equals_ObjectBoxed_ReturnsTrueWhenEqual_Test()
        {
            // Arrange
            var a = WrapIPv4("10.0.0.1");
            object b = WrapIPv4("10.0.0.1");

            // Act / Assert
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_NonWrapper_ReturnsFalse_Test()
        {
            // Arrange
            var a = WrapIPv4("10.0.0.1");

            // Act / Assert
            Assert.False(a.Equals("not a wrapper"));
        }

        [Fact]
        public void GetHashCode_EqualValues_HaveSameHashCode_Test()
        {
            // Arrange
            var a = WrapIPv4("192.168.1.1");
            var b = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        #endregion // end: Equals / == / !=
    }
}
