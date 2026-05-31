using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Arcus.Math;
using Arcus.Utilities;
using Gulliver;
using Xunit;

namespace Arcus.Tests.Utilities
{
#if NET6_0_OR_GREATER
#pragma warning disable IDE0062 // Make local function static (IDE0062); purposely allowing non-static functions that could be static for .net4.8 compatibility
#endif
    public class IPAddressUtilitiesTests
    {
        #region IPv4MaxAddress

        [Fact]
        public void IPv4MaxAddress_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv4MaxAddress;

            // Assert
            Assert.Equal(IPAddress.Parse("255.255.255.255"), address);
        }

        #endregion // end: IPv4MaxAddress

        #region IPv4MinAddress

        [Fact]
        public void IPv4MinAddress_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv4MinAddress;

            // Assert
            Assert.Equal(IPAddress.Parse("0.0.0.0"), address);
        }

        #endregion // end: IPv4MinAddress

        #region IPv4OctetCount

        [Fact]
        public void IPv4OctetCount_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(4, IPAddressUtilities.IPv4OctetCount);
        }

        #endregion // end: IPv4OctetCount

        #region IPv6HextetCount

        [Fact]
        public void IPv6HextetCount_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(8, IPAddressUtilities.IPv6HextetCount);
        }

        #endregion // end: IPv6HextetCount

        #region IPv6MaxAddress

        [Fact]
        public void IPv6MaxAddress_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv6MaxAddress;

            // Assert
            Assert.Equal(IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"), address);
        }

        #endregion // end: IPv6MaxAddress

        #region IPv6MinAddress

        [Fact]
        public void IPv6MinAddress_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv6MinAddress;

            // Assert
            Assert.Equal(IPAddress.Parse("::"), address);
        }

        #endregion // end: IPv6MinAddress

        #region IsIPv4

        public static TheoryData<bool, string> IsIPv4_Test_Data =>
            new TheoryData<bool, string>
            {
                { false, null },
                { false, "::" },
                { false, "ffff::" },
                { true, "192.168.1.1" },
                { true, "0.0.0.0" },
            };

        [Theory]
        [MemberData(nameof(IsIPv4_Test_Data))]
        public void IsIPv4_ReturnsExpected_Test(bool expected, string input)
        {
            // Arrange
            _ = IPAddress.TryParse(input, out var address);

            // Act
            var result = address.IsIPv4();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsIPv4

        #region IsIPv4MappedIPv6

        public static TheoryData<bool, string> IsIPv4MappedIPv6_Test_Data =>
            new TheoryData<bool, string>
            {
                { false, null },
                { false, "::" },
                { false, "192.168.1.1" },
                { true, "::ffff:222.1.41.90" },
                { true, "::ffff:ab:cd" },
                { false, "1234::ffff:222.1.41.90" },
                { false, "1234::ffff:ab:cd" },
            };

        [Theory]
        [MemberData(nameof(IsIPv4MappedIPv6_Test_Data))]
        public void IsIPv4MappedIPv6_ReturnsExpected_Test(bool expected, string input)
        {
            // Arrange
            _ = IPAddress.TryParse(input, out var address);

            // Act
            var result = address.IsIPv4MappedIPv6();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsIPv4MappedIPv6

        #region IsIPv6

        public static TheoryData<bool, string> IsIPv6_Test_Data =>
            new TheoryData<bool, string>
            {
                { false, null },
                { true, "::" },
                { true, "ffff::" },
                { false, "192.168.1.1" },
                { false, "0.0.0.0" },
            };

        [Theory]
        [MemberData(nameof(IsIPv6_Test_Data))]
        public void IsIPv6_ReturnsExpected_Test(bool expected, string input)
        {
            // Arrange
            _ = IPAddress.TryParse(input, out var address);

            // Act
            var result = address.IsIPv6();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsIPv6

        #region shared helpers (private)

        private static IEnumerable<AddressFamily> NonStandardAddressFamilies()
        {
            return Enum.GetValues(typeof(AddressFamily))
                .Cast<AddressFamily>()
                .Except(new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 });
        }

        private static IEnumerable<IPAddress> GeneralPurposeIPv4Addresses()
        {
            var addressStrings = new[] { "10.0.0.0", "10.0.0.128", "0.0.0.0", "255.255.255.255", "192.168.1.1" };

            foreach (var addressString in addressStrings)
            {
                yield return IPAddress.Parse(addressString);
            }

            yield return IPAddress.Any;
            yield return IPAddress.Broadcast;
            yield return IPAddress.Loopback;
            yield return IPAddress.None;
        }

        private static IEnumerable<IPAddress> GeneralPurposeIPv6Addresses()
        {
            var addressStrings = new[]
            {
                "::",
                "::1",
                "1::",
                "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
                "1234:ffff:ffff:ffff::",
                "::ffff:ffff:ffff:abcd",
                "1234::abcd",
                "1234::ffff:abcd",
                "1234:ffff::abcd",
                "1234:ffff::ffff:abcd",
                "1:2:3:4:5:6:7:8",
                "a:b:c::",
                "::a:b:c",
            };

            foreach (var addressString in addressStrings)
            {
                yield return IPAddress.Parse(addressString);
            }

            yield return IPAddress.IPv6Any;
            yield return IPAddress.IPv6Loopback;
        }

        #endregion // end: shared helpers (private)

        #region IsValidNetMask

        public static TheoryData<bool, IPAddress> IsValidNetMask_Test_Data()
        {
            var data = new TheoryData<bool, IPAddress>();

            // all valid netmask values
            for (var i = 0; i <= 32; i++)
            {
                var netmaskBytes = Enumerable.Repeat((byte)0xFF, 4).ToArray().ShiftBitsLeft(32 - i);
                data.Add(true, new IPAddress(netmaskBytes));
            }

            data.Add(false, null);

            var invalidNetmaskAddressStrings = new[]
            {
                "::",
                "ffff::",
                "255.255.0.255",
                "255.0.255.255",
                "0.255.255.255",
                "0.0.0.255",
                "0.0.0.1",
            };

            foreach (var s in invalidNetmaskAddressStrings)
            {
                data.Add(false, IPAddress.Parse(s));
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(IsValidNetMask_Test_Data))]
        public void IsValidNetMask_ReturnsExpected_Test(bool expected, IPAddress input)
        {
            // Arrange
            // Act
            var result = input.IsValidNetMask();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: IsValidNetMask

        #region Parse / TryParse

        #region ParseFromHexString / TryParseFromHexString

        public static TheoryData<IPAddress, string, AddressFamily> ParseFromHexString_Valid_Test_Data()
        {
            var data = new TheoryData<IPAddress, string, AddressFamily>();

            foreach (var address in HexParseAddresses())
            {
                var asHex = AddressToHexString(address);

                data.Add(address, asHex.ToUpperInvariant(), address.AddressFamily);
                data.Add(address, asHex.ToLowerInvariant(), address.AddressFamily);
                data.Add(address, $"0x{asHex}".ToUpperInvariant(), address.AddressFamily);
                data.Add(address, $"0x{asHex}".ToLowerInvariant(), address.AddressFamily);

                var msbZeroTrim = new string(asHex.SkipWhile(c => c == '0').ToArray());

                if (!string.IsNullOrEmpty(msbZeroTrim))
                {
                    data.Add(address, msbZeroTrim.ToUpperInvariant(), address.AddressFamily);
                    data.Add(address, msbZeroTrim.ToLowerInvariant(), address.AddressFamily);
                    data.Add(address, $"0x{msbZeroTrim}".ToUpperInvariant(), address.AddressFamily);
                    data.Add(address, $"0x{msbZeroTrim}".ToLowerInvariant(), address.AddressFamily);
                }
            }

            data.Add(IPAddress.Parse("128.128.128.128"), "00000000080808080", AddressFamily.InterNetwork);

            return data;
        }

        public static TheoryData<AddressFamily> ParseFromHexString_InvalidAddressFamily_Test_Data()
        {
            var data = new TheoryData<AddressFamily>();
            foreach (var af in NonStandardAddressFamilies())
            {
                data.Add(af);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_Valid_Test_Data))]
        public void ParseFromHexString_ValidInput_ReturnsExpected_Test(
            IPAddress expected,
            string addressString,
            AddressFamily addressFamily
        )
        {
            // Arrange
            // Act
            var result = IPAddressUtilities.ParseFromHexString(addressString, addressFamily);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseFromHexString_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressUtilities.ParseFromHexString(null, default));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\t")]
        [InlineData(" ")]
        public void ParseFromHexString_EmptyOrWhitespaceInput_Throws_ArgumentException_Test(string input)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => IPAddressUtilities.ParseFromHexString(input, AddressFamily.InterNetwork));
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void ParseFromHexString_InvalidAddressFamily_Throws_ArgumentException_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => IPAddressUtilities.ParseFromHexString("abc123", addressFamily));
        }

        [Theory]
        [InlineData("abcdxyz")]
        [InlineData("potato")]
        [InlineData("%$#")]
        public void ParseFromHexString_NonHexInput_Throws_ArgumentException_Test(string input)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => IPAddressUtilities.ParseFromHexString(input, AddressFamily.InterNetwork));
        }

        [Fact]
        public void ParseFromHexString_IPv6HexTooLargeForIPv4_Throws_ArgumentOutOfRangeException_Test()
        {
            // Arrange
            var ipv6HexString = AddressToHexString(IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"));

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                IPAddressUtilities.ParseFromHexString(ipv6HexString, AddressFamily.InterNetwork)
            );
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_Valid_Test_Data))]
        public void TryParseFromHexString_ValidInput_ReturnsTrue_Test(
            IPAddress expected,
            string addressString,
            AddressFamily addressFamily
        )
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseFromHexString(addressString, addressFamily, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseFromHexString_NullInput_ReturnsFalse_Test()
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseFromHexString(null, AddressFamily.InterNetwork, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void TryParseFromHexString_InvalidAddressFamily_ReturnsFalse_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseFromHexString("abc123", addressFamily, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        private static IEnumerable<IPAddress> HexParseAddresses()
        {
            yield return IPAddress.Any;
            yield return IPAddress.Loopback;
            yield return IPAddress.None;
            yield return IPAddress.Parse("192.168.1.1");
            yield return IPAddress.Parse("255.255.255");

            yield return IPAddress.IPv6Any;
            yield return IPAddress.IPv6Loopback;
            yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            yield return IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
            yield return IPAddress.Parse("ffff:ffff:ffff:ffff::");
            yield return IPAddress.Parse("::abc:ffff:ffff:ffff");
        }

        private static string AddressToHexString(IPAddress address)
        {
            return string.Concat(address.GetAddressBytes().Select(b => Convert.ToString(b, 16).PadLeft(2, '0')));
        }

        #endregion // end: ParseFromHexString / TryParseFromHexString

        #region ParseIgnoreOctalInIPv4 / TryParseIgnoreOctalInIPv4

        public static TheoryData<IPAddress, string> ParseIgnoreOctalInIPv4_Valid_Test_Data()
        {
            var data = new TheoryData<IPAddress, string>();

            foreach (var address in OctalParseAddresses())
            {
                data.Add(address, address.ToString());

                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    data.Add(address, AddressToQuads(address));
                }
            }

            data.Add(IPAddress.Parse("0.0.0.192"), "192");
            data.Add(IPAddress.Parse("1.0.0.192"), "1.192");
            data.Add(IPAddress.Parse("1.255.0.192"), "1.255.192");

            // octal case
            data.Add(IPAddress.Parse("7.7.7.0"), "007.007.7.0");

            return data;
        }

        public static TheoryData<string> ParseIgnoreOctalInIPv4_Invalid_Test_Data =>
            new TheoryData<string> { { "potato" }, { "255.255.255.255.255" } };

        [Theory]
        [MemberData(nameof(ParseIgnoreOctalInIPv4_Valid_Test_Data))]
        public void ParseIgnoreOctalInIPv4_ValidInput_ReturnsExpected_Test(IPAddress expected, string input)
        {
            // Arrange
            // Act
            var result = IPAddressUtilities.ParseIgnoreOctalInIPv4(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseIgnoreOctalInIPv4_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressUtilities.ParseIgnoreOctalInIPv4(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\t")]
        [InlineData(" ")]
        public void ParseIgnoreOctalInIPv4_EmptyOrWhitespaceInput_Throws_ArgumentException_Test(string input)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => IPAddressUtilities.ParseIgnoreOctalInIPv4(input));
        }

        [Theory]
        [MemberData(nameof(ParseIgnoreOctalInIPv4_Invalid_Test_Data))]
        public void ParseIgnoreOctalInIPv4_InvalidInput_Throws_Test(string input)
        {
            // Arrange
            // Act
            // Assert
            Assert.ThrowsAny<Exception>(() => IPAddressUtilities.ParseIgnoreOctalInIPv4(input));
        }

        [Theory]
        [MemberData(nameof(ParseIgnoreOctalInIPv4_Valid_Test_Data))]
        public void TryParseIgnoreOctalInIPv4_ValidInput_ReturnsTrue_Test(IPAddress expected, string input)
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseIgnoreOctalInIPv4(input, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseIgnoreOctalInIPv4_NullInput_ReturnsFalse_Test()
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseIgnoreOctalInIPv4(null, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(ParseIgnoreOctalInIPv4_Invalid_Test_Data))]
        public void TryParseIgnoreOctalInIPv4_InvalidInput_ReturnsFalse_Test(string input)
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParseIgnoreOctalInIPv4(input, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        private static IEnumerable<IPAddress> OctalParseAddresses()
        {
            yield return IPAddress.Any;
            yield return IPAddress.Loopback;
            yield return IPAddress.None;
            yield return IPAddress.Parse("7.7.7.7");
            yield return IPAddress.Parse("0.0.0.192");
            yield return IPAddress.Parse("192.168.1.1");
            yield return IPAddress.Parse("255.255.255");

            yield return IPAddress.IPv6Any;
            yield return IPAddress.IPv6Loopback;
            yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            yield return IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
            yield return IPAddress.Parse("ffff:ffff:ffff:ffff::");
            yield return IPAddress.Parse("::abc:ffff:ffff:ffff");
        }

        private static string AddressToQuads(IPAddress address)
        {
            return string.Join(".", address.GetAddressBytes().Select(b => Convert.ToString(b, 10).PadLeft(3, '0')));
        }

        #endregion // end: ParseIgnoreOctalInIPv4 / TryParseIgnoreOctalInIPv4

        #region Parse(byte[]) / TryParse(byte[])

        public static TheoryData<IPAddress, byte[], AddressFamily> Parse_ByteArray_Valid_Test_Data()
        {
            var data = new TheoryData<IPAddress, byte[], AddressFamily>();

            foreach (var address in GeneralPurposeIPv4Addresses().Concat(GeneralPurposeIPv6Addresses()))
            {
                data.Add(address, address.GetAddressBytes().ToArray(), address.AddressFamily);
            }

            // underflow — pad with MSB zeros
            data.Add(IPAddress.Parse("0.0.0.0"), Array.Empty<byte>(), AddressFamily.InterNetwork);
            data.Add(IPAddress.Parse("::"), Array.Empty<byte>(), AddressFamily.InterNetworkV6);
            data.Add(IPAddress.Parse("0.0.0.255"), new byte[] { 0x00, 0xff }, AddressFamily.InterNetwork);
            data.Add(IPAddress.Parse("::acca"), new byte[] { 0x00, 0x00, 0xac, 0xca }, AddressFamily.InterNetworkV6);

            return data;
        }

        [Theory]
        [MemberData(nameof(Parse_ByteArray_Valid_Test_Data))]
        public void Parse_ByteArray_ValidInput_ReturnsExpected_Test(
            IPAddress expected,
            byte[] bytes,
            AddressFamily addressFamily
        )
        {
            // Arrange
            // Act
            var result = IPAddressUtilities.Parse(bytes, addressFamily);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_ByteArray_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => IPAddressUtilities.Parse(null, AddressFamily.InterNetwork));
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void Parse_ByteArray_InvalidAddressFamily_Throws_ArgumentOutOfRangeException_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => IPAddressUtilities.Parse(new byte[] { 0x42 }, addressFamily));
        }

        [Theory]
        [InlineData(17, AddressFamily.InterNetworkV6)]
        [InlineData(5, AddressFamily.InterNetwork)]
        public void Parse_ByteArray_InputTooLong_Throws_ArgumentOutOfRangeException_Test(int count, AddressFamily addressFamily)
        {
            // Arrange
            var bytes = Enumerable.Repeat((byte)0x00, count).ToArray();

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => IPAddressUtilities.Parse(bytes, addressFamily));
        }

        [Theory]
        [MemberData(nameof(Parse_ByteArray_Valid_Test_Data))]
        public void TryParse_ByteArray_ValidInput_ReturnsTrue_Test(
            IPAddress expected,
            byte[] bytes,
            AddressFamily addressFamily
        )
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParse(bytes, addressFamily, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParse_ByteArray_NullInput_ReturnsFalse_Test()
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParse(null, AddressFamily.InterNetwork, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void TryParse_ByteArray_InvalidAddressFamily_ReturnsFalse_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            var success = IPAddressUtilities.TryParse(new byte[] { 0x42 }, addressFamily, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        #endregion // end: Parse(byte[]) / TryParse(byte[])

        #endregion // end: Parse / TryParse

        #region MaxIPAddress

        [Fact]
        public void MaxIPAddress_IPv4_ReturnsIPv4MaxAddress_Test()
        {
            // Arrange
            // Act
            var result = AddressFamily.InterNetwork.MaxIPAddress();

            // Assert
            Assert.Same(IPAddressUtilities.IPv4MaxAddress, result);
        }

        [Fact]
        public void MaxIPAddress_IPv6_ReturnsIPv6MaxAddress_Test()
        {
            // Arrange
            // Act
            var result = AddressFamily.InterNetworkV6.MaxIPAddress();

            // Assert
            Assert.Same(IPAddressUtilities.IPv6MaxAddress, result);
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void MaxIPAddress_InvalidAddressFamily_Throws_ArgumentException_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => addressFamily.MaxIPAddress());
        }

        #endregion // end: MaxIPAddress

        #region MinIPAddress

        [Fact]
        public void MinIPAddress_IPv4_ReturnsIPv4MinAddress_Test()
        {
            // Arrange
            // Act
            var result = AddressFamily.InterNetwork.MinIPAddress();

            // Assert
            Assert.Same(IPAddressUtilities.IPv4MinAddress, result);
        }

        [Fact]
        public void MinIPAddress_IPv6_ReturnsIPv6MinAddress_Test()
        {
            // Arrange
            // Act
            var result = AddressFamily.InterNetworkV6.MinIPAddress();

            // Assert
            Assert.Same(IPAddressUtilities.IPv6MinAddress, result);
        }

        [Theory]
        [MemberData(nameof(ParseFromHexString_InvalidAddressFamily_Test_Data))]
        public void MinIPAddress_InvalidAddressFamily_Throws_ArgumentException_Test(AddressFamily addressFamily)
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => addressFamily.MinIPAddress());
        }

        #endregion // end: MinIPAddress

        #region Constant Values

        #region BitCount

        [Fact]
        public void IPv4BitCount_Value_IsThirtyTwo_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(32, IPAddressUtilities.IPv4BitCount);
        }

        [Fact]
        public void IPv4BitCount_MatchesIPv4ByteCount_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(IPAddressUtilities.IPv4BitCount, IPAddress.Any.GetAddressBytes().Length * 8);
        }

        [Fact]
        public void IPv6BitCount_Value_IsOneTwentyEight_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(128, IPAddressUtilities.IPv6BitCount);
        }

        [Fact]
        public void IPv6BitCount_MatchesIPv6ByteCount_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(IPAddressUtilities.IPv6BitCount, IPAddress.IPv6Any.GetAddressBytes().Length * 8);
        }

        #endregion // end: BitCount

        #region ByteCount

        [Fact]
        public void IPv4ByteCount_Value_IsFour_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(4, IPAddressUtilities.IPv4ByteCount);
        }

        [Fact]
        public void IPv4ByteCount_MatchesIPv4AddressBytes_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(IPAddressUtilities.IPv4ByteCount, IPAddress.Any.GetAddressBytes().Length);
        }

        [Fact]
        public void IPv6ByteCount_Value_IsSixteen_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(16, IPAddressUtilities.IPv6ByteCount);
        }

        [Fact]
        public void IPv6ByteCount_MatchesIPv6AddressBytes_Test()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal(IPAddressUtilities.IPv6ByteCount, IPAddress.IPv6Any.GetAddressBytes().Length);
        }

        #endregion // end: ByteCount

        #region MaxAddress

        [Fact]
        public void IPv6MaxAddress_Value_IsAllFF_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv6MaxAddress;

            // Assert
            Assert.Equal(new IPAddress(Enumerable.Repeat((byte)0xff, 16).ToArray()), address);
            Assert.Equal(AddressFamily.InterNetworkV6, address.AddressFamily);
        }

        [Fact]
        public void IPv4MaxAddress_Value_IsAllFF_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv4MaxAddress;

            // Assert
            Assert.Equal(new IPAddress(Enumerable.Repeat((byte)0xff, 4).ToArray()), address);
            Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
        }

        #endregion // end: MaxAddress

        #region MinAddress

        [Fact]
        public void IPv6MinAddress_Value_IsAllZero_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv6MinAddress;

            // Assert
            Assert.Equal(new IPAddress(Enumerable.Repeat((byte)0x00, 16).ToArray()), address);
            Assert.Equal(AddressFamily.InterNetworkV6, address.AddressFamily);
        }

        [Fact]
        public void IPv4MinAddress_Value_IsAllZero_Test()
        {
            // Arrange
            // Act
            var address = IPAddressUtilities.IPv4MinAddress;

            // Assert
            Assert.Equal(new IPAddress(Enumerable.Repeat((byte)0x00, 4).ToArray()), address);
            Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
        }

        #endregion // end: MinAddress

        #region ValidAddressFamilies

        [Fact]
        public void ValidAddressFamilies_IsReadOnlyCollection_ContainsBothFamilies_Test()
        {
            // Arrange
            var validAddressFamilies = IPAddressUtilities.ValidAddressFamilies;

            // Act

            // Assert
            Assert.IsAssignableFrom<IReadOnlyCollection<AddressFamily>>(validAddressFamilies);
            Assert.Equal(2, validAddressFamilies.Count);
            Assert.Contains(AddressFamily.InterNetworkV6, validAddressFamilies);
            Assert.Contains(AddressFamily.InterNetwork, validAddressFamilies);
        }

        #endregion // end: ValidAddressFamilies

        #endregion // end: Constant Values

        #region IsPrivate

        public static TheoryData<bool, IPAddress> IsPrivate_Test_Data()
        {
            var data = new TheoryData<bool, IPAddress>();

            foreach (var subnet in SubnetUtilities.PrivateIPAddressRangesList)
            {
                data.Add(true, subnet.NetworkPrefixAddress);
                data.Add(true, subnet.NetworkPrefixAddress.Increment(2));
                data.Add(true, subnet.BroadcastAddress);
                data.Add(true, subnet.BroadcastAddress.Increment(-2));
            }

            data.Add(false, IPAddressUtilities.IPv4MaxAddress);
            data.Add(false, IPAddressUtilities.IPv4MinAddress);
            data.Add(false, IPAddressUtilities.IPv6MaxAddress);
            data.Add(false, IPAddressUtilities.IPv6MinAddress);

            return data;
        }

        [Theory]
        [MemberData(nameof(IsPrivate_Test_Data))]
        public void IsPrivate_ReturnsExpected_Test(bool expected, IPAddress address)
        {
            // Arrange
            // Act
            var isPrivate = address.IsPrivate();

            // Assert
            Assert.Equal(expected, isPrivate);
        }

        [Fact]
        public void IsPrivate_NullAddress_Throws_ArgumentNullException_Test()
        {
            // Arrange
            IPAddress address = null;

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => address.IsPrivate());
        }

        #endregion // end: IsPrivate
    }
}
