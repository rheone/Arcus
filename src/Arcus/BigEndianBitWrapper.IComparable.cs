namespace Arcus
{
    /// <content><see cref="BigEndianBitWrapper"/> implementation of <see cref="System.IComparable{T}"/></content>
    internal readonly partial struct BigEndianBitWrapper
    {
        /// <summary>
        ///     Compares this instance to <paramref name="other" /> as unsigned integers.
        ///     If the two values have different <see cref="ByteWidth" /> values the shorter one is
        ///     treated as zero-extended to the larger width.
        /// </summary>
        /// <param name="other">The value to compare against.</param>
        /// <returns>A negative integer if less than, zero if equal, positive if greater than.</returns>
        public int CompareTo(BigEndianBitWrapper other)
        {
#if NET8_0_OR_GREATER
            return _value.CompareTo(other._value);
#else
            if (_hi != other._hi)
            {
                return _hi.CompareTo(other._hi);
            }

            return _lo.CompareTo(other._lo);
#endif
        }
    }
}
