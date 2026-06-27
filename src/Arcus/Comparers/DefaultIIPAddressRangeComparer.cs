using System.Net;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Arcus.Comparers
{
    /// <summary>
    ///     Default <see cref="IIPAddressRange" /> <see cref="Comparer{IIPAddressRange}" />
    ///     Compares by <see cref="IIPAddressRange.Head" /> and then by range length ordinal
    /// </summary>
#pragma warning disable S101 // Types should be named in PascalCase
    public class DefaultIIPAddressRangeComparer : Comparer<IIPAddressRange>
#pragma warning restore S101 // Types should be named in PascalCase
    {
        /// <summary>
        ///     Default instance of <see cref="DefaultIIPAddressRangeComparer"/> using <see cref="DefaultIPAddressComparer.Instance"/>
        /// </summary>
        public static readonly DefaultIIPAddressRangeComparer Instance = new();

        private readonly IComparer<IPAddress> _ipAddressComparer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultIIPAddressRangeComparer" /> class.
        /// </summary>
        /// <param name="ipAddressComparer">comparer used to compare <see cref="IPAddress" /></param>
        /// <exception cref="ArgumentNullException"><paramref name="ipAddressComparer" /> is <see langword="null" />.</exception>
        public DefaultIIPAddressRangeComparer(IComparer<IPAddress> ipAddressComparer)
        {
            this._ipAddressComparer = ipAddressComparer ?? throw new ArgumentNullException(nameof(ipAddressComparer));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultIIPAddressRangeComparer" /> class.
        ///     Defaults to use the DefaultIIPAddressComparer
        /// </summary>
        public DefaultIIPAddressRangeComparer()
            : this(DefaultIPAddressComparer.Instance) { }

        /// <inheritdoc />
        public override int Compare(
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [AllowNull]
#endif
            IIPAddressRange x,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [AllowNull]
#endif
            IIPAddressRange y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            var headComparison = this._ipAddressComparer.Compare(x.Head, y.Head);
            return headComparison != 0 ? headComparison : x.Length.CompareTo(y.Length);
        }
    }
}
