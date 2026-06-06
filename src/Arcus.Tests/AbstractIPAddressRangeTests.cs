using System.Net;
using System.Numerics;
using Arcus.Math;
using NSubstitute;
using Xunit;

namespace Arcus.Tests
{
    public partial class AbstractIPAddressRangeTests
    {
        #region Setup / Teardown

        public AbstractIPAddressRangeTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        private readonly ITestOutputHelper _testOutputHelper;

        #endregion // end: Setup / Teardown

        #region other members

        private static AbstractIPAddressRange CreateSubstituteIPAddressRange(IPAddress head, IPAddress tail)
        {
            return Substitute.For<AbstractIPAddressRange>(head, tail);
        }

        #endregion // end: other members

        #region Length / TryGetLength

        /// <summary>
        ///     Gets parameters: expected (BigInteger), ipAddressRange (AbstractIPAddressRange)
        /// </summary>
        /// <value>
        ///     Parameters: expected (BigInteger), ipAddressRange (AbstractIPAddressRange)
        /// </value>
        public static TheoryData<BigInteger, AbstractIPAddressRange> Length_Test_Data
        {
            get
            {
                return new TheoryData<BigInteger, AbstractIPAddressRange>
                {
                    // single address
                    { new BigInteger(1), CreateSubstituteIPAddressRange(IPAddress.Any, IPAddress.Any) },
                    { new BigInteger(1), CreateSubstituteIPAddressRange(IPAddress.IPv6Any, IPAddress.IPv6Any) },
                    // maximum length ipv4
                    {
                        BigInteger.Pow(2, 32),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("0.0.0.0"), IPAddress.Parse("255.255.255.255"))
                    },
                    // maximum length ipv6
                    {
                        BigInteger.Pow(2, 128),
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")
                        )
                    },
                    // ipv6 length at int.MaxValue
                    {
                        new BigInteger(int.MaxValue),
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(int.MaxValue - 1))
                    },
                    // ipv6 length at long.MaxValue
                    {
                        new BigInteger(long.MaxValue),
                        CreateSubstituteIPAddressRange(
                            IPAddress.Parse("::"),
                            IPAddress.Parse("::").Increment(long.MaxValue - 1)
                        )
                    },
                    // ipv6 length at int.MaxValue + 1
                    {
                        new BigInteger(int.MaxValue) + 1,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(int.MaxValue))
                    },
                    // ipv6 length at long.MaxValue + 1
                    {
                        new BigInteger(long.MaxValue) + 1,
                        CreateSubstituteIPAddressRange(IPAddress.Parse("::"), IPAddress.Parse("::").Increment(long.MaxValue))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(Length_Test_Data))]
        public static void Length_Test(BigInteger expected, AbstractIPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var result = ipAddressRange.Length;

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Length / TryGetLength

        #region Class

        [Fact]
        public void Implementation_Test()
        {
            // Arrange
            var type = typeof(AbstractIPAddressRange);

            // Act
            // Assert
            Assert.True(typeof(IIPAddressRange).IsAssignableFrom(type));
        }

        [Fact]
        public void AbstractClass_Test()
        {
            // Arrange
            var type = typeof(AbstractIPAddressRange);

            // Act
            var isAbstract = type.IsAbstract;

            // Assert
            Assert.True(isAbstract);
        }

        #endregion // end: Class

        #region Ctor

        [Theory]
        [InlineData("192.168.1.1", "192.168.1.5")]
        [InlineData("::beef", "::dead")]
        public void Ctor_HappyPath_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Assert
            Assert.Equal(head, iPAddressRange.Head);
            Assert.Equal(tail, iPAddressRange.Tail);
        }

        [Theory]
        [InlineData("192.168.1.1", null)]
        [InlineData(null, "192.168.1.5")]
        [InlineData(null, null)]
        public void Ctor_Null_Input_Throws_ArgumentNullException_Test(string headString, string tailString)
        {
            // Arrange
            var head = headString != null ? IPAddress.Parse(headString) : null;

            var tail = tailString != null ? IPAddress.Parse(tailString) : null;

            // Act
            // Assert
            var exception = Assert.ThrowsAny<System.Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            Assert.IsAssignableFrom<System.ArgumentNullException>(exception.InnerException);
        }

        [Theory]
        [InlineData("192.168.1.1", "::beef")]
        [InlineData("::beef", "192.168.1.5")]
        public void Ctor_MismatchAddressFamilies_Throws_InvalidOperationException_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            // Assert
            var exception = Assert.ThrowsAny<System.Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            var inner = Assert.IsAssignableFrom<System.InvalidOperationException>(exception.InnerException);
            Assert.Contains("matching address families", inner.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("192.168.1.5", "192.168.1.1")]
        [InlineData("::dead", "::beef")]
        public void Ctor_TailBeforeHead_Throws_InvalidOperationException_Test(string headString, string tailString)
        {
            // Arrange
            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            // Act
            // Assert
            var exception = Assert.ThrowsAny<System.Exception>(() => CreateSubstituteIPAddressRange(head, tail));
            var inner = Assert.IsAssignableFrom<System.InvalidOperationException>(exception.InnerException);
            Assert.Contains("greater or equal", inner.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        #endregion // end: Ctor

        #region AddressFamily

        [Fact]
        public void AddressFamily_IPv4_Test()
        {
            // Arrange
            const string headString = "192.168.1.1";
            const string tailString = "192.168.1.5";

            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            // Assert
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetwork, iPAddressRange.AddressFamily);
            Assert.True(iPAddressRange.IsIPv4);
            Assert.False(iPAddressRange.IsIPv6);
        }

        [Fact]
        public void AddressFamily_IPv6_Test()
        {
            // Arrange
            const string headString = "::beef";
            const string tailString = "::dead";

            var head = IPAddress.Parse(headString);
            var tail = IPAddress.Parse(tailString);

            var iPAddressRange = CreateSubstituteIPAddressRange(head, tail);

            // Act
            // Assert
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetworkV6, iPAddressRange.AddressFamily);
            Assert.False(iPAddressRange.IsIPv4);
            Assert.True(iPAddressRange.IsIPv6);
        }

        #endregion // end: AddressFamily
    }
}
