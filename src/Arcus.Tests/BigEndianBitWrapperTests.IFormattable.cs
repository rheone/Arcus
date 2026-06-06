using System.Globalization;
using Xunit;

namespace Arcus.Tests
{
    public partial class BigEndianBitWrapperTests
    {
        #region ToHexString / ToString("HC")

        public static TheoryData<byte[], string> ToHexString_KnownValues_Test_Data =>
            new()
            {
                { new byte[] { 0, 0, 0, 0 }, "00000000" },
                { new byte[] { 255, 255, 255, 255 }, "FFFFFFFF" },
                { new byte[] { 192, 168, 1, 1 }, "C0A80101" },
                { new byte[] { 10, 0, 0, 1 }, "0A000001" },
                {
                    new byte[] { 0x20, 0x01, 0x0D, 0xB8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                    "20010DB8000000000000000000000001"
                },
            };

        [Theory]
        [MemberData(nameof(ToHexString_KnownValues_Test_Data))]
        public void ToHexString_KnownValues_ReturnsUppercaseHex_Test(byte[] input, string expected)
        {
            // Act
            var result = Wrap(input).ToHexString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToHexString_LengthIsDoubleByteWidth_Test()
        {
            // Arrange / Act
            var result = WrapIPv4("192.168.1.1").ToHexString();

            // Assert
            Assert.Equal(8, result.Length); // 4 bytes × 2
        }

        [Fact]
        public void ToString_FormatHC_MatchesToHexString_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(w.ToHexString(), w.ToString("HC", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ToString_NullFormat_ReturnsHex_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(w.ToHexString(), w.ToString(null, null));
        }

        [Fact]
        public void ToString_FormatG_ReturnsHex_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(w.ToHexString(), w.ToString("G", null));
        }

        [Fact]
        public void ToString_NoArgs_ReturnsHex_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal("C0A80101", w.ToString());
        }

        #endregion // end: ToHexString / ToString("HC")

        #region ToDecimalString / ToString("IBE")

        public static TheoryData<byte[], string> ToDecimalString_KnownValues_Test_Data =>
            new()
            {
                { new byte[] { 0, 0, 0, 0 }, "0" },
                { new byte[] { 0, 0, 0, 1 }, "1" },
                { new byte[] { 255, 255, 255, 255 }, "4294967295" },
                { new byte[] { 192, 168, 1, 1 }, "3232235777" },
            };

        [Theory]
        [MemberData(nameof(ToDecimalString_KnownValues_Test_Data))]
        public void ToDecimalString_KnownValues_ReturnsDecimalString_Test(byte[] input, string expected)
        {
            // Act
            var result = Wrap(input).ToDecimalString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToString_FormatIBE_MatchesToDecimalString_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(w.ToDecimalString(), w.ToString("IBE", CultureInfo.InvariantCulture));
        }

        #endregion // end: ToDecimalString / ToString("IBE")

        #region ToBinaryString / ToString("b")

        [Fact]
        public void ToBinaryString_SingleByte_ReturnsEightCharacters_Test()
        {
            // Arrange
            var w = Wrap(0x0F);

            // Act
            var result = w.ToBinaryString();

            // Assert
            Assert.Equal("00001111", result);
        }

        [Fact]
        public void ToBinaryString_AllZerosIPv4_AllZeroChars_Test()
        {
            // Act
            var result = WrapIPv4("0.0.0.0").ToBinaryString();

            // Assert
            Assert.Equal(new string('0', 32), result);
        }

        [Fact]
        public void ToBinaryString_AllOnesIPv4_AllOneChars_Test()
        {
            // Act
            var result = WrapIPv4("255.255.255.255").ToBinaryString();

            // Assert
            Assert.Equal(new string('1', 32), result);
        }

        [Fact]
        public void ToBinaryString_LengthIsEightTimesWidth_Test()
        {
            // Arrange / Act
            var ipv4Result = WrapIPv4("10.0.0.1").ToBinaryString();
            var ipv6Result = WrapIPv6("::1").ToBinaryString();

            // Assert
            Assert.Equal(32, ipv4Result.Length); // 4 × 8
            Assert.Equal(128, ipv6Result.Length); // 16 × 8
        }

        [Fact]
        public void ToBinaryString_ContainsOnlyZeroAndOne_Test()
        {
            // Act
            var result = WrapIPv4("192.168.1.1").ToBinaryString();

            // Assert
            Assert.All(result, c => Assert.True(c is '0' or '1'));
        }

        [Fact]
        public void ToString_FormatB_MatchesToBinaryString_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act / Assert
            Assert.Equal(w.ToBinaryString(), w.ToString("b", null));
        }

        #endregion // end: ToBinaryString / ToString("b")

        #region ToString unknown format

        [Fact]
        public void ToString_UnknownFormat_ThrowsFormatException_Test()
        {
            // Arrange
            var w = WrapIPv4("10.0.0.1");

            // Act / Assert
            Assert.Throws<System.FormatException>(() => w.ToString("X", null));
        }

        #endregion // end: ToString unknown format
    }
}
