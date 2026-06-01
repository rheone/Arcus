using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Arcus.Converters;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.DocExamples
{
    public class IPAddressConvertersExamples
    {
        #region Setup / Teardown

        public IPAddressConvertersExamples(ITestOutputHelper output)
        {
            this._output = output;
        }

        private readonly ITestOutputHelper _output;

        #endregion

        [Fact]
        public void NetmaskToCidrRoutePrefix_Example()
        {
            // build all valid net masks by setting the top i bits of 4 bytes
            var allNetMasks = Enumerable.Range(7, 10).Select(i => MakeNetmaskBytes(i)).Select(b => new IPAddress(b)).ToArray();

            var sb = new StringBuilder();

            foreach (var netmask in allNetMasks)
            {
                var routePrefix = netmask.NetmaskToCidrRoutePrefix();

                sb.Append(routePrefix)
                    .Append('\t')
                    .AppendFormat(CultureInfo.InvariantCulture, "{0,-15}", netmask)
                    .Append('\t')
                    .Append(string.Concat(netmask.GetAddressBytes().Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))))
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());

            static byte[] MakeNetmaskBytes(int prefixLength)
            {
                var result = new byte[4];
                for (var i = 0; i < prefixLength && i < 32; i++)
                {
                    result[i / 8] |= (byte)(0x80 >> (i % 8));
                }

                return result;
            }
        }

        [Fact]
        public void ToBase85String_Example()
        {
            var addresses = new[]
            {
                "::",
                "::ffff",
                "1080:0:0:0:8:800:200C:417A", // specific example from RFC 1924
                "ffff::",
                "ffff::0102:0304",
                "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
            }
                .Select(IPAddress.Parse)
                .ToArray();

            var sb = new StringBuilder();

            foreach (var address in addresses)
            {
                var base85String = address.ToBase85String();

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-40}", address)
                    .Append("\t=>\t")
                    .Append(base85String)
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());
        }

        [Fact]
        public void ToDottedQuadString_Example()
        {
            var addresses = new[]
            {
                "::",
                "::ffff",
                "a:b:c::ff00:ff",
                "ffff::",
                "ffff::0102:0304",
                "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
            }
                .Select(IPAddress.Parse)
                .ToArray();

            var sb = new StringBuilder();

            foreach (var address in addresses)
            {
                var dottedQuadString = address.ToDottedQuadString();

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-40}", address)
                    .Append("\t=>\t")
                    .Append(dottedQuadString)
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());
        }

        [Fact]
        public void ToHexString_Example()
        {
            var addresses = new[]
            {
                "::",
                "::ffff",
                "10.1.1.1",
                "192.168.1.1",
                "255.255.255.255",
                "ffff::",
                "ffff::0102:0304",
                "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
            }
                .Select(IPAddress.Parse)
                .ToArray();

            var sb = new StringBuilder();

            foreach (var address in addresses)
            {
                var hexString = address.ToHexString();

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-40}", address)
                    .Append("\t=>\t")
                    .Append(hexString)
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());
        }

        [Fact]
        public void ToNumericString_Example()
        {
            var addresses = new[]
            {
                "::",
                "::ffff",
                "10.1.1.1",
                "192.168.1.1",
                "255.255.255.255",
                "ffff::",
                "ffff::0102:0304",
                "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff",
            }
                .Select(IPAddress.Parse)
                .ToArray();

            var sb = new StringBuilder();

            foreach (var address in addresses)
            {
                var numericString = address.ToNumericString();

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-40}", address)
                    .Append("\t=>\t")
                    .Append(numericString)
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());
        }

        [Fact]
        public void ToUncompressedString_Example()
        {
            var addresses = new[] { "::", "::ffff", "10.1.1.1", "192.168.1.1", "255.255.255.255", "ffff::", "ffff::0102:0304" }
                .Select(IPAddress.Parse)
                .ToArray();

            var sb = new StringBuilder();

            foreach (var address in addresses)
            {
                var uncompressedString = address.ToUncompressedString();

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0,-40}", address)
                    .Append("\t=>\t")
                    .Append(uncompressedString)
                    .AppendLine();
            }

            this._output.WriteLine(sb.ToString());
        }
    }
}
