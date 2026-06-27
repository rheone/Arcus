using System.Net.Sockets;

namespace Arcus.Comparers
{
    /// <summary>
    ///     Default <see cref="AddressFamily" /> <see cref="Comparer{AddressFamily}" />
    ///     Executes the <see cref="Enum.CompareTo" /> of <see cref="AddressFamily" /> method
    /// </summary>
    public class DefaultAddressFamilyComparer : Comparer<AddressFamily>
    {
        /// <summary>
        ///     Default instance of <see cref="DefaultAddressFamilyComparer"/> using <see cref="DefaultIPAddressComparer.Instance"/>
        /// </summary>
        public static readonly DefaultAddressFamilyComparer Instance = new();

        /// <inheritdoc />
        public override int Compare(AddressFamily x, AddressFamily y)
        {
            return x.CompareTo(y);
        }
    }
}
