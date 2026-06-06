using System;
using System.Collections.Generic;
using System.Net;
using Arcus;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="Subnet"/> tests for <see cref="IEquatable{Subnet}"/>
    /// </content>
    public partial class SubnetTests
    {
        #region Equals(Subnet)

        /// <summary>Gets theory data for <see cref="Equals_Subnet_Test"/> and <see cref="Equals_Object_BoxedSubnet_Test"/>.</summary>
        /// <returns>Parameters: expected equality result (bool), first subnet (<see cref="Subnet"/>), second subnet (<see cref="Subnet"/>).</returns>
        public static TheoryData<bool, Subnet, Subnet> Equals_Subnet_Test_Values()
        {
            var data = new TheoryData<bool, Subnet, Subnet>();
            var seen = new HashSet<string>();

            foreach (var ipAddress in IPv4Addresses())
            {
                for (var i = 0; i <= 32; i++)
                {
                    var sA = new Subnet(ipAddress, i);
                    var sB = new Subnet(ipAddress, (i + 2) % 32);
                    var sv6A = new Subnet(IPAddress.IPv6Any, i);
                    var sv6B = new Subnet(IPAddress.IPv6Loopback, i);

                    if (seen.Add($"T|{sA.ToString("f", null)}|{sA.ToString("f", null)}"))
                    {
                        data.Add(true, sA, sA); // equivalent
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sB.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sB); // differing prefix
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sv6A.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sv6A); // different family
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sv6B.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sv6B); // different family
                    }
                }
            }

            foreach (var ipAddress in IPv6Addresses())
            {
                for (var i = 0; i <= 128; i++)
                {
                    var sA = new Subnet(ipAddress, i);
                    var sB = new Subnet(ipAddress, (i + 2) % 128);

                    if (seen.Add($"T|{sA.ToString("f", null)}|{sA.ToString("f", null)}"))
                    {
                        data.Add(true, sA, sA); // equivalent
                    }

                    if (seen.Add($"F|{sA.ToString("f", null)}|{sB.ToString("f", null)}"))
                    {
                        data.Add(false, sA, sB); // differing prefix
                    }
                }
            }

            return data;

            IEnumerable<IPAddress> IPv4Addresses()
            {
                yield return IPAddress.Any;
                yield return IPAddress.Loopback;
                yield return IPAddress.None;
                yield return IPAddress.Parse("192.168.1.1");
            }

            IEnumerable<IPAddress> IPv6Addresses()
            {
                yield return IPAddress.IPv6Any;
                yield return IPAddress.IPv6Loopback;
                yield return IPAddress.Parse("2001:0db8:85a3:0042:1000:8a2e:0370:7334");
            }
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(Subnet)"/> returns the expected equality result.</summary>
        /// <param name="expected">Expected equality result.</param>
        /// <param name="subnetA">The subject subnet.</param>
        /// <param name="subnetB">The subnet to compare against.</param>
        [Theory]
        [MemberData(nameof(Equals_Subnet_Test_Values))]
        public void Equals_Subnet_Test(bool expected, Subnet subnetA, Subnet subnetB)
        {
            // Arrange

            // Act
            var result = subnetA.Equals(subnetB);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(Subnet)"/> returns <see langword="false"/> when the argument is null.</summary>
        [Fact]
        public void Equals_Subnet_NullOther_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals((Subnet)null);

            // Assert
            Assert.False(result);
        }

        #endregion // end: Equals(Subnet)

        #region Equals(object)

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns the expected equality result when the argument is a boxed subnet.</summary>
        /// <param name="expected">Expected equality result.</param>
        /// <param name="subnetA">The subject subnet.</param>
        /// <param name="subnetB">The boxed subnet to compare against.</param>
        [Theory]
        [MemberData(nameof(Equals_Subnet_Test_Values))]
        public void Equals_Object_BoxedSubnet_Test(bool expected, Subnet subnetA, object subnetB)
        {
            // Arrange

            // Act
            var result = subnetA.Equals(subnetB);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns <see langword="false"/> when the argument is not a <see cref="Subnet"/>.</summary>
        [Fact]
        public void Equals_Object_NonSubnetType_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals("not a subnet");

            // Assert
            Assert.False(result);
        }

        /// <summary>Verifies that <see cref="Subnet.Equals(object)"/> returns <see langword="false"/> when the argument is null.</summary>
        [Fact]
        public void Equals_Object_Null_ReturnsFalse_Test()
        {
            // Arrange
            var subnet = Subnet.Parse("192.168.0.0/16");

            // Act
            var result = subnet.Equals((object)null);

            // Assert
            Assert.False(result);
        }

        #endregion // end: Equals(object)

        #region GetHashCode

        /// <summary>Gets theory data for <see cref="GetHashCode_Test"/>.</summary>
        /// <returns>Parameters: expected hash equality (bool), left subnet (<see cref="Subnet"/>), right subnet (<see cref="Subnet"/>).</returns>
        public static TheoryData<bool, Subnet, Subnet> GetHashCode_Test_Values()
        {
            return new TheoryData<bool, Subnet, Subnet>
            {
                // value-equal IPv4
                { true, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.Any, 16) },
                // value-equal IPv6
                { true, new Subnet(IPAddress.IPv6Any, 16), new Subnet(IPAddress.IPv6Any, 16) },
                // expected different
                { false, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.IPv6Any, 16) },
                { false, new Subnet(IPAddress.Any, 8), new Subnet(IPAddress.Any, 16) },
                { false, new Subnet(IPAddress.IPv6Any, 8), new Subnet(IPAddress.IPv6Any, 16) },
                { false, new Subnet(IPAddress.Any, 16), new Subnet(IPAddress.Broadcast, 16) },
                { false, new Subnet(IPAddress.Parse("::")), new Subnet(IPAddress.Parse("ab::"), 16) },
            };
        }

        /// <summary>Verifies that <see cref="Subnet.GetHashCode"/> returns equal hashes for value-equal subnets and distinct hashes for different subnets.</summary>
        /// <param name="expectedEqual">Whether the hash codes are expected to be equal.</param>
        /// <param name="left">The left-hand subnet.</param>
        /// <param name="right">The right-hand subnet.</param>
        [Theory]
        [MemberData(nameof(GetHashCode_Test_Values))]
        public void GetHashCode_Test(bool expectedEqual, Subnet left, Subnet right)
        {
            // Arrange
            // Act
            var leftHash = left.GetHashCode();
            var rightHash = right.GetHashCode();

            // Assert
            Assert.Equal(expectedEqual, leftHash == rightHash);
        }

        #endregion // end: GetHashCode
    }
}
