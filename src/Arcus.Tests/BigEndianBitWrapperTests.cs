using System;
using System.Globalization;
using System.Net;
using Arcus;
using Xunit;

namespace Arcus.Tests
{
    public class BigEndianBitWrapperTests
    {
        // Convenience helpers — keep tests focused on behaviour, not construction noise.
        private static BigEndianBitWrapper Wrap(params byte[] bytes) => BigEndianBitWrapper.FromBytes(bytes);

        private static BigEndianBitWrapper WrapIPv4(string dotted) => Wrap(IPAddress.Parse(dotted).GetAddressBytes());

        private static BigEndianBitWrapper WrapIPv6(string addr) => Wrap(IPAddress.Parse(addr).GetAddressBytes());

        #region FromBytes

        public static TheoryData<byte[]> FromBytes_ValidInput_RoundTrips_Test_Data =>
            new TheoryData<byte[]>
            {
                // IPv4
                { new byte[] { 0, 0, 0, 0 } },
                { new byte[] { 255, 255, 255, 255 } },
                { new byte[] { 192, 168, 1, 1 } },
                { new byte[] { 10, 0, 0, 1 } },
                // MAC (6 bytes)
                { new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF } },
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
            Assert.Throws<ArgumentException>(() => BigEndianBitWrapper.FromBytes(Array.Empty<byte>()));
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
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianBitWrapper.FromBytes(new byte[] { 0x01 }, targetWidth));
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
            var wrapper = BigEndianBitWrapper.FromBytes(Array.Empty<byte>(), 4);

            // Assert
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, wrapper.ToBytes());
        }

        #endregion // end: FromBytes(bytes, targetWidth)

        #region CreateMask

        public static TheoryData<int, int, byte[]> CreateMask_IPv4_Test_Data =>
            new TheoryData<int, int, byte[]>
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
            new TheoryData<int, int, byte[]>
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

        #region TryAdd

        [Fact]
        public void TryAdd_DeltaZero_ReturnsTrueWithSameValue_Test()
        {
            // Arrange
            var w = WrapIPv4("10.0.0.1");

            // Act
            var success = w.TryAdd(0, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(w, result);
        }

        [Fact]
        public void TryAdd_PositiveDelta_IncrementsByDelta_Test()
        {
            // Arrange
            var w = WrapIPv4("10.0.0.1");

            // Act
            var success = w.TryAdd(1, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(WrapIPv4("10.0.0.2"), result);
        }

        [Fact]
        public void TryAdd_PositiveDeltaLarge_IncrementsByDelta_Test()
        {
            // Arrange
            var w = WrapIPv4("10.0.0.1");

            // Act
            var success = w.TryAdd(254, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(WrapIPv4("10.0.0.255"), result);
        }

        [Fact]
        public void TryAdd_NegativeDelta_DecrementsByDelta_Test()
        {
            // Arrange
            var w = WrapIPv4("10.0.0.5");

            // Act
            var success = w.TryAdd(-3, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(WrapIPv4("10.0.0.2"), result);
        }

        [Fact]
        public void TryAdd_PositiveDelta_IPv4MaxAddress_ReturnsFalse_Test()
        {
            // Arrange: 255.255.255.255 + 1 overflows IPv4
            var w = WrapIPv4("255.255.255.255");

            // Act
            var success = w.TryAdd(1, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(default(BigEndianBitWrapper), result);
        }

        [Fact]
        public void TryAdd_NegativeDelta_OnZeroAddress_ReturnsFalse_Test()
        {
            // Arrange: 0.0.0.0 - 1 underflows
            var w = WrapIPv4("0.0.0.0");

            // Act
            var success = w.TryAdd(-1, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(default(BigEndianBitWrapper), result);
        }

        [Fact]
        public void TryAdd_PositiveDeltaExceedsWidthBound_ReturnsFalse_Test()
        {
            // Arrange: any IPv4 + 2^32 overflows because delta > max for 4 bytes
            var w = WrapIPv4("0.0.0.0");

            // Act
            var success = w.TryAdd(4294967296L, out _); // 2^32

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void TryAdd_IPv6_MaxAddress_Overflow_ReturnsFalse_Test()
        {
            // Arrange
            var maxIPv6 = WrapIPv6("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

            // Act
            var success = maxIPv6.TryAdd(1, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void TryAdd_IPv6_ZeroAddress_Underflow_ReturnsFalse_Test()
        {
            // Arrange
            var zeroIPv6 = WrapIPv6("::");

            // Act
            var success = zeroIPv6.TryAdd(-1, out _);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void TryAdd_IPv6_IncrementAcrossWordBoundary_Test()
        {
            // Arrange: ::ffff + 1 = ::1:0000
            var w = WrapIPv6("::ffff");

            // Act
            var success = w.TryAdd(1, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(WrapIPv6("::1:0"), result);
        }

        [Fact]
        public void TryAdd_IPv4_IncrementAcrossByteRollover_Test()
        {
            // Arrange: 10.0.0.255 + 1 = 10.0.1.0
            var w = WrapIPv4("10.0.0.255");

            // Act
            var success = w.TryAdd(1, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(WrapIPv4("10.0.1.0"), result);
        }

        #endregion // end: TryAdd

        #region Subtract

        [Fact]
        public void Subtract_BasicDifference_ReturnsExpectedValue_Test()
        {
            // Arrange
            var tail = WrapIPv4("10.0.0.10");
            var head = WrapIPv4("10.0.0.1");

            // Act
            var diff = tail.Subtract(head);

            // Assert — difference is 9
            Assert.Equal(9, (int)diff.ToBigInteger());
        }

        [Fact]
        public void Subtract_EqualValues_ReturnsZero_Test()
        {
            // Arrange
            var w = WrapIPv4("192.168.1.1");

            // Act
            var diff = w.Subtract(w);

            // Assert
            Assert.Equal(0, (int)diff.ToBigInteger());
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, diff.ToBytes());
        }

        [Fact]
        public void Subtract_Underflow_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var small = WrapIPv4("10.0.0.1");
            var large = WrapIPv4("10.0.0.100");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => small.Subtract(large));
        }

        [Fact]
        public void Subtract_IPv6_LargeRange_ReturnsExpectedBigInteger_Test()
        {
            // Arrange: ::ffff:ffff - ::0 = 4294967295
            var tail = WrapIPv6("::ffff:ffff");
            var head = WrapIPv6("::");

            // Act
            var diff = tail.Subtract(head);

            // Assert
            Assert.Equal(System.Numerics.BigInteger.Parse("4294967295", CultureInfo.InvariantCulture), diff.ToBigInteger());
        }

        #endregion // end: Subtract

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

        #region ToBigInteger

        public static TheoryData<byte[], long> ToBigInteger_KnownValues_Test_Data =>
            new TheoryData<byte[], long>
            {
                { new byte[] { 0, 0, 0, 0 }, 0L },
                { new byte[] { 0, 0, 0, 1 }, 1L },
                { new byte[] { 0, 0, 1, 0 }, 256L },
                { new byte[] { 1, 0, 0, 0 }, 16777216L },
                { new byte[] { 255, 255, 255, 255 }, 4294967295L },
                { new byte[] { 192, 168, 1, 1 }, 3232235777L },
            };

        [Theory]
        [MemberData(nameof(ToBigInteger_KnownValues_Test_Data))]
        public void ToBigInteger_KnownIPv4Values_ReturnsExpected_Test(byte[] input, long expected)
        {
            // Arrange
            var w = Wrap(input);

            // Act
            var result = w.ToBigInteger();

            // Assert
            Assert.Equal(new System.Numerics.BigInteger(expected), result);
        }

        [Fact]
        public void ToBigInteger_ZeroIPv6_ReturnsZero_Test()
        {
            // Act
            var result = WrapIPv6("::").ToBigInteger();

            // Assert
            Assert.Equal(System.Numerics.BigInteger.Zero, result);
        }

        [Fact]
        public void ToBigInteger_IsNonNegative_Test()
        {
            // Arrange: max IPv4 value
            var w = WrapIPv4("255.255.255.255");

            // Act
            var result = w.ToBigInteger();

            // Assert
            Assert.True(result >= 0);
        }

        #endregion // end: ToBigInteger

        #region ToHexString / ToString("HC")

        public static TheoryData<byte[], string> ToHexString_KnownValues_Test_Data =>
            new TheoryData<byte[], string>
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
            new TheoryData<byte[], string>
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
            Assert.All(result, c => Assert.True(c == '0' || c == '1'));
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
            Assert.Throws<FormatException>(() => w.ToString("X", null));
        }

        #endregion // end: ToString unknown format

        #region CompareTo

        public static TheoryData<string, string, int> CompareTo_Test_Data =>
            new TheoryData<string, string, int>
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
            var ipv4 = BigEndianBitWrapper.FromBytes(new byte[] { 0, 0, 0, 1 });
            var ipv6 = BigEndianBitWrapper.FromBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

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

        #region ByteWidth preservation

        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(16)]
        public void ByteWidth_IsPreservedAcrossOperations_Test(int byteWidth)
        {
            // Arrange
            var bytes = new byte[byteWidth];
            bytes[byteWidth - 1] = 0x01;
            var w = Wrap(bytes);

            // Act: put the wrapper through multiple operations
            var notW = ~w;
            w.TryAdd(1, out var incremented);
            var andResult = w & notW;

            // Assert: ByteWidth is preserved through all operations
            Assert.Equal(byteWidth, notW.ByteWidth);
            Assert.Equal(byteWidth, incremented.ByteWidth);
            Assert.Equal(byteWidth, andResult.ByteWidth);
        }

        #endregion // end: ByteWidth preservation

        #region Subnet operations (combined)

        [Fact]
        public void SubnetOps_ComputeNetworkAndBroadcast_IPv4_Test()
        {
            // Reproduces the NormalizeAndCreateNetMask pattern from Subnet.cs
            // Arrange: host address 192.168.1.100/24
            var headBytes = IPAddress.Parse("192.168.1.100").GetAddressBytes();
            var head = Wrap(headBytes);
            var mask = BigEndianBitWrapper.CreateMask(4, 24);

            // Act
            var network = (head & mask).ToBytes();
            var broadcast = (head | ~mask).ToBytes();

            // Assert
            Assert.Equal(IPAddress.Parse("192.168.1.0").GetAddressBytes(), network);
            Assert.Equal(IPAddress.Parse("192.168.1.255").GetAddressBytes(), broadcast);
        }

        [Fact]
        public void SubnetOps_ComputeNetworkAndBroadcast_IPv6_Test()
        {
            // Arrange: 2001:db8::1/64
            var headBytes = IPAddress.Parse("2001:db8::1").GetAddressBytes();
            var head = Wrap(headBytes);
            var mask = BigEndianBitWrapper.CreateMask(16, 64);

            // Act
            var network = (head & mask).ToBytes();
            var broadcast = (head | ~mask).ToBytes();

            // Assert
            Assert.Equal(IPAddress.Parse("2001:db8::").GetAddressBytes(), network);
            Assert.Equal(IPAddress.Parse("2001:db8::ffff:ffff:ffff:ffff").GetAddressBytes(), broadcast);
        }

        #endregion // end: Subnet operations (combined)
    }
}
