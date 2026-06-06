using System;
using Arcus.Comparers;

namespace Arcus
{
    /// <content>
    ///     <see cref="Subnet"/> implementation of <see cref="IComparable{Subnet}"/> and <see cref="IComparable"/>
    /// </content>
    public partial class Subnet : IComparable<Subnet>, IComparable
    {
        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is Subnet other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException("Object is not an Subnet");
        }

        /// <inheritdoc />
        public int CompareTo(Subnet other)
        {
            if (other is null)
            {
                return 1;
            }

            return DefaultIIPAddressRangeComparer.Instance.Compare(this, other);
        }
    }
}
