using System.Net;
using System.Numerics;
using Arcus.Math;
using Arcus.Tests.XunitSerializers;

[assembly: RegisterXunitSerializer(typeof(IPAddressXunitSerializer), typeof(IPAddress))]
[assembly: RegisterXunitSerializer(typeof(IIPAddressRangeXunitSerializer), typeof(IIPAddressRange))]

namespace Arcus.Tests
{
    public partial class AbstractIPAddressRangeTests(ITestOutputHelper testOutputHelper)
    {
        #region Setup / Teardown

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        #endregion // end: Setup / Teardown

        #region other members

        private static IPAddressRange CreateSubstituteIPAddressRange(IPAddress head, IPAddress tail)
        {
            return new IPAddressRange(head, tail);
        }

        #endregion // end: other members

        #region Length / TryGetLength

        /// <summary>
        ///     Gets parameters: expected (BigInteger), ipAddressRange (AbstractIPAddressRange)
        /// </summary>
        /// <value>
        ///     Parameters: expected (BigInteger), ipAddressRange (AbstractIPAddressRange)
        /// </value>
        public static TheoryData<BigInteger, IPAddressRange> Length_Test_Data
        {
            get
            {
                return new TheoryData<BigInteger, IPAddressRange>
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
        public static void Length_Test(BigInteger expected, IPAddressRange ipAddressRange)
        {
            // Arrange
            // Act
            var result = ipAddressRange.Length;

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion // end: Length / TryGetLength

        #region Class

        /// <summary>Verifies that AbstractIPAddressRange implements IIPAddressRange.</summary>
        [Fact]
        public void Implementation_Test()
        {
            // Arrange
            var type = typeof(AbstractIPAddressRange);

            // Act
            // Assert
            Assert.True(typeof(IIPAddressRange).IsAssignableFrom(type));
        }

        /// <summary>Verifies that AbstractIPAddressRange is declared as an abstract class.</summary>
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

        /// <summary>Verifies the constructor sets Head and Tail correctly for valid same-family address pairs.</summary>
        /// <param name="headString">The head IP address string.</param>
        /// <param name="tailString">The tail IP address string.</param>
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

        /// <summary>Verifies the constructor throws ArgumentNullException when either address is null.</summary>
        /// <param name="headString">The head IP address string, or null.</param>
        /// <param name="tailString">The tail IP address string, or null.</param>
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
            Assert.Throws<System.ArgumentNullException>(() => CreateSubstituteIPAddressRange(head, tail));
        }

        /// <summary>Verifies the constructor throws InvalidOperationException when head and tail are from different address families.</summary>
        /// <param name="headString">The head IP address string.</param>
        /// <param name="tailString">The tail IP address string.</param>
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
            var exception = Assert.Throws<System.InvalidOperationException>(() => CreateSubstituteIPAddressRange(head, tail));
            Assert.Contains("matching address families", exception.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the constructor throws InvalidOperationException when tail is less than head.</summary>
        /// <param name="headString">The head IP address string.</param>
        /// <param name="tailString">The tail IP address string (less than head).</param>
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
            var exception = Assert.Throws<System.InvalidOperationException>(() => CreateSubstituteIPAddressRange(head, tail));
            Assert.Contains("greater or equal", exception.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        #endregion // end: Ctor

        #region AddressFamily

        /// <summary>Verifies AddressFamily is InterNetwork and IsIPv4 is true for an IPv4 range.</summary>
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

        /// <summary>Verifies AddressFamily is InterNetworkV6 and IsIPv6 is true for an IPv6 range.</summary>
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
