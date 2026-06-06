using System;
using Arcus.Comparers;

namespace Arcus
{
    /// <content>
    ///     <see cref="IPAddressRange"/> implementation of <see cref="IComparable{IPAddressRange}"/>
    /// </content>
    public partial class IPAddressRange : IComparable<IPAddressRange>, IComparable
    {
        #region From Interface IComparable

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is IPAddressRange other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException("Object is not an IPAddressRange");
        }

        #endregion

        #region From Interface IComparable<IPAddressRange>

        /// <inheritdoc />
        public int CompareTo(IPAddressRange other)
        {
            if (other is null)
            {
                return 1;
            }

            return DefaultIIPAddressRangeComparer.Instance.Compare(this, other);
        }

        #endregion
    }
}
