using System;
using Xunit;

namespace Arcus.Tests
{
    public partial class BigEndianBitWrapperTests
    {
        #region FromBytes

        public static TheoryData<byte[]> FromBytes_ValidInput_RoundTrips_Test_Data =>
            new()
            {
                // IPv4
                { new byte[] { 0, 0, 0, 0 } },
                { new byte[] { 255, 255, 255, 255 } },
                { new byte[] { 192, 168, 1, 1 } },
                { new byte[] { 10, 0, 0, 1 } },
                // IPv6
                { new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
                {
                    new byte[]
                    {
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                    }
                },
                {
                    new byte[]
                    {
                        0x20,
                        0x01,
                        0x0D,
                        0xB8,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x00,
                        0x01,
                    }
                },
                // Leading zeros preserved
                { new byte[] { 0x00, 0x00, 0xFF, 0xFF } },
            };

        [Theory]
        [MemberData(nameof(FromBytes_ValidInput_RoundTrips_Test_Data))]
        public void FromBytes_ValidInput_RoundTrips_Test(byte[] input)
        {
            // Arrange / Act
            var wrapper = Wrap(input);

            // Assert
            Assert.Equal(input.Length, wrapper.ByteWidth);
            Assert.Equal(input, wrapper.ToBytes());
        }

        [Fact]
        public void FromBytes_NullInput_ThrowsArgumentNullException_Test()
        {
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => BigEndianBitWrapper.FromBytes(null));
        }

        [Fact]
        public void FromBytes_EmptyArray_ThrowsArgumentException_Test()
        {
            // Act / Assert
            Assert.Throws<ArgumentException>(() => BigEndianBitWrapper.FromBytes([]));
        }

        [Fact]
        public void FromBytes_ArrayLongerThan16_ThrowsArgumentException_Test()
        {
            // Act / Assert
            Assert.Throws<ArgumentException>(() => BigEndianBitWrapper.FromBytes(new byte[17]));
        }

        #endregion // end: FromBytes

        #region FromBytes(bytes, targetWidth)

        [Fact]
        public void FromBytesWithTarget_NullBytes_ThrowsArgumentNullException_Test()
        {
            Assert.Throws<ArgumentNullException>(() => BigEndianBitWrapper.FromBytes(null, 4));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(17)]
        [InlineData(-1)]
        public void FromBytesWithTarget_InvalidTargetWidth_ThrowsArgumentOutOfRangeException_Test(int targetWidth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianBitWrapper.FromBytes([0x01], targetWidth));
        }

        [Fact]
        public void FromBytesWithTarget_BytesLongerThanTarget_ThrowsArgumentException_Test()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act / Assert
            Assert.Throws<ArgumentException>(() => BigEndianBitWrapper.FromBytes(bytes, 4));
        }

        [Fact]
        public void FromBytesWithTarget_SameLength_MatchesFromBytes_Test()
        {
            // Arrange
            var bytes = new byte[] { 192, 168, 1, 1 };

            // Act
            var withTarget = BigEndianBitWrapper.FromBytes(bytes, 4);
            var direct = BigEndianBitWrapper.FromBytes(bytes);

            // Assert
            Assert.Equal(direct, withTarget);
        }

        [Fact]
        public void FromBytesWithTarget_ShorterBytes_ZeroPadsOnLeft_Test()
        {
            // Arrange: one-byte value 0x01, padded to IPv4 width
            var bytes = new byte[] { 0x01 };

            // Act
            var wrapper = BigEndianBitWrapper.FromBytes(bytes, 4);

            // Assert
            Assert.Equal(4, wrapper.ByteWidth);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, wrapper.ToBytes());
        }

        [Fact]
        public void FromBytesWithTarget_EmptyBytes_ProducesAllZeros_Test()
        {
            // Act
            var wrapper = BigEndianBitWrapper.FromBytes([], 4);

            // Assert
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, wrapper.ToBytes());
        }

        #endregion // end: FromBytes(bytes, targetWidth)

        #region CreateMask

        public static TheoryData<int, int, byte[]> CreateMask_IPv4_Test_Data =>
            new()
            {
                { 4, 0, new byte[] { 0x00, 0x00, 0x00, 0x00 } },
                { 4, 8, new byte[] { 0xFF, 0x00, 0x00, 0x00 } },
                { 4, 16, new byte[] { 0xFF, 0xFF, 0x00, 0x00 } },
                { 4, 24, new byte[] { 0xFF, 0xFF, 0xFF, 0x00 } },
                { 4, 28, new byte[] { 0xFF, 0xFF, 0xFF, 0xF0 } },
                { 4, 31, new byte[] { 0xFF, 0xFF, 0xFF, 0xFE } },
                { 4, 32, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF } },
            };

        [Theory]
        [MemberData(nameof(CreateMask_IPv4_Test_Data))]
        public void CreateMask_IPv4_ReturnsCorrectMask_Test(int byteWidth, int prefix, byte[] expected)
        {
            // Act
            var mask = BigEndianBitWrapper.CreateMask(byteWidth, prefix);

            // Assert
            Assert.Equal(byteWidth, mask.ByteWidth);
            Assert.Equal(expected, mask.ToBytes());
        }

        public static TheoryData<int, int, byte[]> CreateMask_IPv6_Test_Data =>
            new()
            {
                { 16, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
                { 16, 64, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0 } },
                { 16, 96, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0 } },
                {
                    16,
                    128,
                    new byte[]
                    {
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                        0xFF,
                    }
                },
                { 16, 32, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            };

        [Theory]
        [MemberData(nameof(CreateMask_IPv6_Test_Data))]
        public void CreateMask_IPv6_ReturnsCorrectMask_Test(int byteWidth, int prefix, byte[] expected)
        {
            // Act
            var mask = BigEndianBitWrapper.CreateMask(byteWidth, prefix);

            // Assert
            Assert.Equal(byteWidth, mask.ByteWidth);
            Assert.Equal(expected, mask.ToBytes());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(17)]
        [InlineData(-1)]
        public void CreateMask_InvalidByteWidth_ThrowsArgumentOutOfRangeException_Test(int byteWidth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianBitWrapper.CreateMask(byteWidth, 0));
        }

        [Theory]
        [InlineData(4, -1)]
        [InlineData(4, 33)]
        [InlineData(16, 129)]
        [InlineData(16, -1)]
        public void CreateMask_InvalidPrefixLength_ThrowsArgumentOutOfRangeException_Test(int byteWidth, int prefix)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianBitWrapper.CreateMask(byteWidth, prefix));
        }

        #endregion // end: CreateMask
    }
}
