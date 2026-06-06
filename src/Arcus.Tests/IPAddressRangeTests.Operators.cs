using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="IPAddressRange"/> tests for operators
    /// </content>
    public partial class IPAddressRangeTests
    {
        #region Operators

        /// <summary>Verifies the == operator returns true only when the comparison result is zero.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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

        /// <summary>Verifies the != operator returns true when the comparison result is non-zero.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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

        /// <summary>Verifies the &gt; operator returns true only when the comparison result is positive.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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

        /// <summary>Verifies the &gt;= operator returns true when the comparison result is zero or positive.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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

        /// <summary>Verifies the &lt; operator returns true only when the comparison result is negative.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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

        /// <summary>Verifies the &lt;= operator returns true when the comparison result is zero or negative.</summary>
        /// <param name="expected">Expected comparison sign (-1, 0, or 1).</param>
        /// <param name="left">Left operand range.</param>
        /// <param name="right">Right operand range.</param>
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
