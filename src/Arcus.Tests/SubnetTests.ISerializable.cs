#if NET48   // maintained for .NET 4.8 compatibility
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Arcus;
using Xunit;

namespace Arcus.Tests
{
    /// <content>
    ///     <see cref="Subnet"/> tests for <see cref="ISerializable"/>
    /// </content>
    public partial class SubnetTests
    {
        /// <summary>Gets theory data for <see cref="CanSerializable_Test"/>.</summary>
        /// <returns>Parameters: subnet to serialize (<see cref="Subnet"/>).</returns>
        public static TheoryData<Subnet> CanSerializable_Test_Values()
        {
            return new TheoryData<Subnet>
            {
                { new Subnet(IPAddress.Parse("192.168.1.0")) },
                { new Subnet(IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.1.255")) },
                { new Subnet(IPAddress.Parse("::"), IPAddress.Parse("::FFFF")) },
            };
        }

        /// <summary>Verifies that a <see cref="Subnet"/> can be round-trip serialized and deserialized using <see cref="BinaryFormatter"/>.</summary>
        /// <param name="subnet">The subnet to serialize.</param>
        [Theory]
        [MemberData(nameof(CanSerializable_Test_Values))]
        public void CanSerializable_Test(Subnet subnet)
        {
            // Arrange
            var formatter = new BinaryFormatter();

            // Act
            using (var writeStream = new MemoryStream())
            {
                formatter.Serialize(writeStream, subnet);
                writeStream.Seek(0, SeekOrigin.Begin);

                // Deserialize the object from the stream
                var result = formatter.Deserialize(writeStream);

                // Assert
                var actual = Assert.IsType<Subnet>(result);

                // using explicit EqualityComparer to avoid comparing elements of enumerable
                Assert.Equal(subnet, actual, SubnetEqualityComparer.Instance);
            }
        }
    }
}
#endif
