using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using Arcus.Comparers;
using NSubstitute;
using Xunit;

namespace Arcus.Tests.Comparers
{
#pragma warning disable CS0618 // Type or member is obsolete — intentionally testing the obsolete DefaultIPAddressRangeComparer
    public class DefaultIPAddressRangeComparerTests
    {
        [Fact]
        public void Assignability_Test()
        {
            // Arrange
            var type = typeof(DefaultIPAddressRangeComparer);

            // Act
            var isAssignableFrom = typeof(IComparer<IIPAddressRange>).IsAssignableFrom(type);

            // Assert
            Assert.True(isAssignableFrom);
        }

        #region Ctor

        [Fact]
        public void Ctor_NullInput_Throws_ArgumentNullException_Test()
        {
            // Arrange
            // Act
            // Assert

            Assert.Throws<ArgumentNullException>(() => new DefaultIPAddressRangeComparer(null));
        }

        #endregion // end: Ctor

        #region Compare

        /// <summary>
        ///     Test data for <see cref="Compare_Test" />.
        ///     Covers equal ranges, same-reference ranges, null operands, cross-family comparisons,
        ///     and ordinal ordering by length within the same address family.
        ///     <para>Parameters: expected comparison result (int), x (IIPAddressRange), y (IIPAddressRange).</para>
        /// </summary>
        public static TheoryData<int, IIPAddressRange, IIPAddressRange> Compare_Test_Data
        {
            get
            {
                var data = new TheoryData<int, IIPAddressRange, IIPAddressRange>();

                // equal ranges
                data.Add(0, CreateRangeFromHead("192.168.1.0", 0), CreateRangeFromHead("192.168.1.0", 0));
                data.Add(0, CreateRangeFromHead("a::", 0), CreateRangeFromHead("a::", 0));

                // same range (reference equality)
                var ipv4Same = CreateRangeFromHead("192.168.1.0");
                data.Add(0, ipv4Same, ipv4Same);

                var ipv6Same = CreateRangeFromHead("a::");
                data.Add(0, ipv6Same, ipv6Same);

                // null compare
                data.Add(0, null, null);
                data.Add(-1, null, CreateRangeFromHead("192.168.1.0"));
                data.Add(1, CreateRangeFromHead("192.168.1.0"), null);
                data.Add(-1, null, CreateRangeFromHead("a::"));
                data.Add(1, CreateRangeFromHead("a::"), null);

                // numerically equivalent, different address families
                data.Add(-1, CreateRangeFromHead("0.0.0.0", 100), CreateRangeFromHead("::", 100));
                data.Add(1, CreateRangeFromHead("::", 100), CreateRangeFromHead("0.0.0.0", 100));

                // satisfies ordinal ordering by length
                data.Add(-1, CreateRangeFromHead("192.0.0.0", 100), CreateRangeFromHead("192.0.0.0", 500));
                data.Add(1, CreateRangeFromHead("192.0.0.0", 500), CreateRangeFromHead("192.0.0.0", 100));
                data.Add(-1, CreateRangeFromHead("ab::", 100), CreateRangeFromHead("ab::", 500));
                data.Add(1, CreateRangeFromHead("ab::", 500), CreateRangeFromHead("ab::", 100));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Compare_Test_Data))]
        public void Compare_Test(int expected, IIPAddressRange x, IIPAddressRange y)
        {
            // Arrange
            var comparer = new DefaultIPAddressRangeComparer();

            // Act
            var result = comparer.Compare(x, y);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DeferToIPAddressComparerTest()
        {
            // Arrange
            const int expectedResult = 42;

            var xHead = IPAddress.Parse("0.0.0.0");
            var yHead = IPAddress.Parse("abc::");

            var x = CreateRangeFromIPAddress(xHead);
            var y = CreateRangeFromIPAddress(yHead);

            var substituteIPAddressComparer = Substitute.For<IComparer<IPAddress>>();
            substituteIPAddressComparer.Compare(xHead, yHead).Returns(expectedResult);

            var comparer = new DefaultIPAddressRangeComparer(substituteIPAddressComparer);

            // Act
            var result = comparer.Compare(x, y);

            // Assert
            Assert.Equal(expectedResult, result);
            substituteIPAddressComparer.Received(1).Compare(xHead, yHead);
        }

        #endregion // end: Compare

        private static IIPAddressRange CreateRangeFromHead(string head, BigInteger? length = null)
        {
            var substitute = Substitute.For<IIPAddressRange>();
            substitute.Head.Returns(IPAddress.Parse(head));
            if (length != null)
            {
                substitute.Length.Returns(length.Value);
            }

            return substitute;
        }

        private static IIPAddressRange CreateRangeFromIPAddress(IPAddress head)
        {
            var substitute = Substitute.For<IIPAddressRange>();
            substitute.Head.Returns(head);
            return substitute;
        }
    }
#pragma warning restore CS0618
}
