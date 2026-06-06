using System.Linq;
using System.Net;
using Arcus;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="IPAddressRange"/> tests for static factory methods
    /// </content>
    public partial class IPAddressRangeTests
    {
        #region TryMerge

        [Theory]
        [InlineData(null, null, null, null, null)]
        [InlineData(null, "192.168.1.1", "192.168.1.9", "::", "::")]
        [InlineData(null, "192.168.1.1", "192.168.1.9", "192.168.1.11", "192.168.1.20")]
        [InlineData(null, "192.168.1.1", "192.168.1.9", null, null)]
        [InlineData(null, null, null, "192.168.1.1", "192.168.1.9")]
        [InlineData("192.168.1.1-192.168.1.20", "192.168.1.1", "192.168.1.10", "192.168.1.11", "192.168.1.20")]
        [InlineData("192.168.1.1-192.168.1.20", "192.168.1.1", "192.168.1.10", "192.168.1.10", "192.168.1.20")]
        [InlineData("192.168.1.1-192.168.1.20", "192.168.1.11", "192.168.1.20", "192.168.1.1", "192.168.1.10")]
        [InlineData("192.168.1.1-192.168.1.20", "192.168.1.10", "192.168.1.20", "192.168.1.1", "192.168.1.10")]
        [InlineData("192.168.1.10-192.168.1.20", "192.168.1.10", "192.168.1.20", "192.168.1.10", "192.168.1.20")]
        [InlineData("::-::", "::", "::", "::", "::")]
        [InlineData("::1-::4", "::1", "::2", "::3", "::4")]
        [InlineData(null, null, null, "::", "::")]
        [InlineData(null, "::", "::", null, null)]
        public void TryMergeResultTest(string expected, string alphaHead, string alphaTail, string betaHead, string betaTail)
        {
            // Arrange
            var alphaAddressRange =
                IPAddress.TryParse(alphaHead, out var alphaHeadAddress)
                && IPAddress.TryParse(alphaTail, out var alphaTailAddress)
                    ? new IPAddressRange(alphaHeadAddress, alphaTailAddress)
                    : null;

            var betaAddressRange =
                IPAddress.TryParse(betaHead, out var betaHeadAddress) && IPAddress.TryParse(betaTail, out var betaTailAddress)
                    ? new IPAddressRange(betaHeadAddress, betaTailAddress)
                    : null;

            // Act
            var successResult = IPAddressRange.TryMerge(alphaAddressRange, betaAddressRange, out var mergeResult);

            // Assert
            Assert.Equal(expected != null, successResult);

            if (expected == null)
            {
                Assert.Null(mergeResult);
            }
            else
            {
                Assert.NotNull(mergeResult);
                Assert.Equal(expected, $"{mergeResult.Head}-{mergeResult.Tail}");
            }
        }

        #endregion

        #region TryCollapseAll

        [Fact]
        public void TryCollapseAll_Consecutive_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                new IPAddressRange(IPAddress.Parse("192.168.1.6"), IPAddress.Parse("192.168.1.7")),
                new IPAddressRange(IPAddress.Parse("192.168.1.8"), IPAddress.Parse("192.168.1.20")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            var result = collection.Single();
            Assert.Equal(IPAddress.Parse("192.168.1.0"), result.Head);
            Assert.Equal(IPAddress.Parse("192.168.1.20"), result.Tail);
        }

        [Fact]
        public void TryCollapse_MismatchedAddressFamilies_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                new IPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("abcd::ef00")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.False(success);
            Assert.NotNull(results);
            Assert.False(results.Any());
        }

        [Fact]
        public void TryCollapseAll_EmptyInput_Test()
        {
            // Act
            var success = IPAddressRange.TryCollapseAll([], out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            Assert.False(results.Any());
        }

        [Fact]
        public void TryCollapse_AllInvalidInput_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                null,
                new IPAddressRange(IPAddress.Parse("192.168.1.30"), IPAddress.Parse("192.168.1.35")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.False(success);
            Assert.NotNull(results);
            Assert.False(results.Any());
        }

        [Fact]
        public void TryCollapse_AllOverlap_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                new IPAddressRange(IPAddress.Parse("192.168.1.5"), IPAddress.Parse("192.168.1.10")),
                new IPAddressRange(IPAddress.Parse("192.168.1.8"), IPAddress.Parse("192.168.1.20")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            var result = collection.Single();
            Assert.Equal(IPAddress.Parse("192.168.1.0"), result.Head);
            Assert.Equal(IPAddress.Parse("192.168.1.20"), result.Tail);
        }

        [Fact]
        public void TryCollapseAll_SubsetContainsAll_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.20")),
                new IPAddressRange(IPAddress.Parse("192.168.1.8"), IPAddress.Parse("192.168.1.20")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            var result = collection.Single();
            Assert.Equal(IPAddress.Parse("192.168.1.0"), result.Head);
            Assert.Equal(IPAddress.Parse("192.168.1.20"), result.Tail);
        }

        [Fact]
        public void TryCollapseAll_WithGaps_Test()
        {
            // Arrange
            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.5")),
                new IPAddressRange(IPAddress.Parse("192.168.1.7"), IPAddress.Parse("192.168.1.20")),
                new IPAddressRange(IPAddress.Parse("192.168.1.30"), IPAddress.Parse("192.168.1.35")),
            };

            // Act
            var success = IPAddressRange.TryCollapseAll(ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var enumerable = results.ToList();
            Assert.Equal(3, enumerable.Count);
            Assert.Equal(enumerable, [.. ranges]);
        }

        #endregion // end: TryCollapseAll

        #region TryExcludeAll

        [Fact]
        public void TryExcludeAll_Carve_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.200"));

            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.10")),
                new IPAddressRange(IPAddress.Parse("192.168.1.50"), IPAddress.Parse("192.168.1.100")),
                new IPAddressRange(IPAddress.Parse("192.168.1.150"), IPAddress.Parse("192.168.1.200")),
            };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var enumerable = results.ToList();
            Assert.Equal(2, enumerable.Count);

            Assert.Equal(
                enumerable,
                [
                    new IPAddressRange(IPAddress.Parse("192.168.1.11"), IPAddress.Parse("192.168.1.49")),
                    new IPAddressRange(IPAddress.Parse("192.168.1.101"), IPAddress.Parse("192.168.1.149")),
                ]
            );
        }

        [Fact]
        public void TryExcludeAll_ConsecutiveCarve_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.200"));

            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("192.168.1.100")),
                new IPAddressRange(IPAddress.Parse("192.168.1.101"), IPAddress.Parse("192.168.1.199")),
            };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var enumerable = results.ToList();
            Assert.Equal(2, enumerable.Count);

            Assert.Equal(
                enumerable,
                [
                    new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.0")),
                    new IPAddressRange(IPAddress.Parse("192.168.1.200"), IPAddress.Parse("192.168.1.200")),
                ]
            );
        }

        [Fact]
        public void TryExcludeAll_Head_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.100"));

            var ranges = new[] { new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.50")) };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            var result = collection.Single();

            Assert.Equal(IPAddress.Parse("192.168.1.51"), result.Head);
            Assert.Equal(IPAddress.Parse("192.168.1.100"), result.Tail);
        }

        [Fact]
        public void TryExcludeAll_Overlap_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.100"));

            var ranges = new[]
            {
                new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.49")),
                new IPAddressRange(IPAddress.Parse("192.168.1.50"), IPAddress.Parse("192.168.1.75")),
                new IPAddressRange(IPAddress.Parse("192.168.1.75"), IPAddress.Parse("192.168.1.100")),
            };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            //Assert
            Assert.True(success);
            Assert.Empty(results);
        }

        [Fact]
        public void TryExcludeAll_Tail_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.100"));

            var ranges = new[] { new IPAddressRange(IPAddress.Parse("192.168.1.50"), IPAddress.Parse("192.168.1.100")) };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            var result = collection.Single();

            Assert.Equal(IPAddress.Parse("192.168.1.0"), result.Head);
            Assert.Equal(IPAddress.Parse("192.168.1.49"), result.Tail);
        }

        [Fact]
        public void TryExcludeAll_NoExclusions_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.200"));

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, [], out var results);

            // Assert
            Assert.True(success);
            Assert.NotNull(results);
            var collection = results.ToList();
            Assert.Single(collection);

            Assert.Equal(initialRange, collection.Single());
        }

        [Fact]
        public void TryExcludeAll_InitialMissMatchedAddressFamily_Test()
        {
            // Arrange
            var initialRange = new IPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("ffff::ffff"));

            var ranges = new[] { new IPAddressRange(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.10")) };

            // Act
            var success = IPAddressRange.TryExcludeAll(initialRange, ranges, out var results);

            // Assert
            Assert.False(success);
            Assert.Empty(results);
        }

        [Fact]
        public void TryExcludeAll_ExclusionTailAtIPv4Max_WithLeadingSegment_Test()
        {
            // Exclusion ends at 255.255.255.255; only the leading segment before the exclusion is retained.
            var initialRange = new IPAddressRange(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("200.0.0.0"));
            var exclusion = new IPAddressRange(IPAddress.Parse("100.0.0.0"), IPAddress.Parse("255.255.255.255"));

            var success = IPAddressRange.TryExcludeAll(initialRange, [exclusion], out var results);

            Assert.True(success);
            var list = results.ToList();
            Assert.Single(list);
            Assert.Equal(IPAddress.Parse("10.0.0.0"), list[0].Head);
            Assert.Equal(IPAddress.Parse("99.255.255.255"), list[0].Tail);
        }

        [Fact]
        public void TryExcludeAll_ExclusionTailAtIPv4Max_CoversAll_Test()
        {
            // Exclusion spans the entire address space; nothing remains.
            var initialRange = new IPAddressRange(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("200.0.0.0"));
            var exclusion = new IPAddressRange(IPAddress.Parse("0.0.0.0"), IPAddress.Parse("255.255.255.255"));

            var success = IPAddressRange.TryExcludeAll(initialRange, [exclusion], out var results);

            Assert.True(success);
            Assert.Empty(results);
        }

        [Fact]
        public void TryExcludeAll_ExclusionHeadAtIPv4Min_WithTrailingSegment_Test()
        {
            // Exclusion starts at 0.0.0.0; only the trailing segment after the exclusion is retained.
            var initialRange = new IPAddressRange(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("200.0.0.0"));
            var exclusion = new IPAddressRange(IPAddress.Parse("0.0.0.0"), IPAddress.Parse("50.0.0.0"));

            var success = IPAddressRange.TryExcludeAll(initialRange, [exclusion], out var results);

            Assert.True(success);
            var list = results.ToList();
            Assert.Single(list);
            Assert.Equal(IPAddress.Parse("50.0.0.1"), list[0].Head);
            Assert.Equal(IPAddress.Parse("200.0.0.0"), list[0].Tail);
        }

        [Fact]
        public void TryExcludeAll_ExclusionTailAtIPv6Max_WithLeadingSegment_Test()
        {
            // IPv6: exclusion ends at the family maximum; only the leading segment is retained.
            var initialRange = new IPAddressRange(IPAddress.Parse("::1"), IPAddress.Parse("f000::"));
            var exclusion = new IPAddressRange(
                IPAddress.Parse("8000::"),
                IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
            );

            var success = IPAddressRange.TryExcludeAll(initialRange, [exclusion], out var results);

            Assert.True(success);
            var list = results.ToList();
            Assert.Single(list);
            Assert.Equal(IPAddress.Parse("::1"), list[0].Head);
            Assert.Equal(IPAddress.Parse("7fff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"), list[0].Tail);
        }

        [Fact]
        public void TryExcludeAll_ExclusionHeadAtIPv6Min_WithTrailingSegment_Test()
        {
            // IPv6: exclusion starts at the family minimum; only the trailing segment is retained.
            var initialRange = new IPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("f000::"));
            var exclusion = new IPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("8000::"));

            var success = IPAddressRange.TryExcludeAll(initialRange, [exclusion], out var results);

            Assert.True(success);
            var list = results.ToList();
            Assert.Single(list);
            Assert.Equal(IPAddress.Parse("8000::1"), list[0].Head);
            Assert.Equal(IPAddress.Parse("f000::"), list[0].Tail);
        }

        #endregion // end: TryExcludeAll
    }
}
