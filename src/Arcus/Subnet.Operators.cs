namespace Arcus
{
    /// <content>
    ///     <see cref="Subnet"/> operators
    /// </content>
    public partial class Subnet
    {
        /// <summary>
        /// Determines whether two <see cref="Subnet"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if both instances are equal; otherwise, false.</returns>
        public static bool operator ==(Subnet left, Subnet right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="Subnet"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if the instances are not equal; otherwise, false.</returns>
        public static bool operator !=(Subnet left, Subnet right) => !(left == right);

        /// <summary>
        /// Compares two <see cref="Subnet"/> instances to determine if the first is less than the second.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if the first instance is less than the second; otherwise, false.</returns>
        public static bool operator <(Subnet left, Subnet right)
        {
            if (left is null)
            {
                return right is not null; // null is less than any non-null instance
            }

            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Compares two <see cref="Subnet"/> instances to determine if the first is greater than the second.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if the first instance is greater than the second; otherwise, false.</returns>
        public static bool operator >(Subnet left, Subnet right)
        {
            if (left is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Compares two <see cref="Subnet"/> instances to determine if the first is less than or equal to the second.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if the first instance is less than or equal to the second; otherwise, false.</returns>
        public static bool operator <=(Subnet left, Subnet right) => left < right || left == right;

        /// <summary>
        /// Compares two <see cref="Subnet"/> instances to determine if the first is greater than or equal to the second.
        /// </summary>
        /// <param name="left">The first <see cref="Subnet"/> instance.</param>
        /// <param name="right">The second <see cref="Subnet"/> instance.</param>
        /// <returns>true if the first instance is greater than or equal to the second; otherwise, false.</returns>
        public static bool operator >=(Subnet left, Subnet right) => left > right || left == right;
    }
}
