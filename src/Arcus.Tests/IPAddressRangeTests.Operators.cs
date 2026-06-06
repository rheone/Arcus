using Arcus;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="IPAddressRange"/> tests for operators
    /// </content>
    public partial class IPAddressRangeTests
    {
        #region Operators

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_Equals_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left == right;

            // Assert
            Assert.Equal(expected == 0, result);
        }

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_NotEquals_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left != right;

            // Assert
            Assert.Equal(expected != 0, result);
        }

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_GreaterThan_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left > right;

            // Assert
            Assert.Equal(expected > 0, result);
        }

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_GreaterThanOrEqual_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left >= right;

            // Assert
            Assert.Equal(expected >= 0, result);
        }

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_LessThan_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left < right;

            // Assert
            Assert.Equal(expected < 0, result);
        }

        [Theory]
        [MemberData(nameof(Comparison_Values))]
        public void Operator_LessThanOrEqual_Test(int expected, IPAddressRange left, IPAddressRange right)
        {
            // Arrange
            // Act
            var result = left <= right;

            // Assert
            Assert.Equal(expected <= 0, result);
        }

        #endregion // end: Operators
    }
}
