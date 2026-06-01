using System;
using System.Globalization;
using System.Numerics;
#if !NET8_0_OR_GREATER
using System.Text;
#endif

namespace Arcus
{
    /// <summary>
    ///     An internal unsigned integer with big-endian byte semantics, used for binary operations on
    ///     network addresses. <see cref="ByteWidth" /> (4 for IPv4, 6 for MAC-48, 16 for IPv6) is stored
    ///     alongside the value so that overflow/underflow detection and byte-array round-trips are always
    ///     bounded to the correct address-family width.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On .NET 8+ the backing store is a <see cref="UInt128" />; on earlier targets two
    ///         <see cref="ulong" /> fields (high / low) provide equivalent 128-bit arithmetic. All arithmetic
    ///         is bounded by <see cref="ByteWidth" />, not by the full 128-bit range.
    ///     </para>
    /// </remarks>
    internal readonly struct BigEndianBitWrapper
        : IComparable<BigEndianBitWrapper>,
            IEquatable<BigEndianBitWrapper>,
            IFormattable
    {
        /// <summary>Gets the byte width of this value (e.g. 4 for IPv4, 6 for MAC, 16 for IPv6).</summary>
        public readonly int ByteWidth;

#if NET8_0_OR_GREATER
        private readonly UInt128 _value; // unsigned 128-bit backing store; always bounded to [0, MaxValueForWidth]

        /// <summary>Initializes a new instance of the <see cref="BigEndianBitWrapper" /> struct.</summary>
        /// <param name="value">The unsigned 128-bit value, already bounded to the representable range for <paramref name="byteWidth" /> bytes.</param>
        /// <param name="byteWidth">Byte width of the address family; must be in [1, 16].</param>
        private BigEndianBitWrapper(UInt128 value, int byteWidth)
        {
            _value = value;
            ByteWidth = byteWidth;
        }

        /// <summary>Gets the maximum unsigned integer representable in <see cref="ByteWidth" /> bytes.</summary>
        /// <value>
        ///     <c>UInt128.MaxValue</c> when <see cref="ByteWidth" /> is 16; otherwise
        ///     <c>(1 &lt;&lt; (ByteWidth × 8)) - 1</c>. Serves as the arithmetic ceiling for overflow
        ///     detection and as the all-ones base for bitwise NOT and mask operations.
        /// </value>
        private UInt128 MaxValueForWidth => ByteWidth >= 16 ? UInt128.MaxValue : (UInt128.One << (ByteWidth * 8)) - 1;
#else
        private readonly ulong _hi; // most-significant 64 bits
        private readonly ulong _lo; // least-significant 64 bits

        /// <summary>Initializes a new instance of the <see cref="BigEndianBitWrapper" /> struct.</summary>
        /// <param name="hi">Most-significant 64 bits of the value; always <c>0</c> for byte widths 1–8.</param>
        /// <param name="lo">Least-significant 64 bits of the value.</param>
        /// <param name="byteWidth">Byte width of the address family; must be in [1, 16].</param>
        private BigEndianBitWrapper(ulong hi, ulong lo, int byteWidth)
        {
            _hi = hi;
            _lo = lo;
            ByteWidth = byteWidth;
        }

        /// <summary>Gets the maximum unsigned integer for <see cref="ByteWidth" /> bytes, split into high and low 64-bit components.</summary>
        /// <value>
        ///     A tuple where <c>Hi</c> is <c>0</c> for byte widths 1–8 (value fits entirely in <c>Lo</c>)
        ///     and carries the most-significant bits for widths 9–16. Serves as the arithmetic ceiling
        ///     for overflow detection and as the all-ones base for bitwise NOT and mask operations.
        /// </value>
        private (ulong Hi, ulong Lo) MaxHiLoForWidth
        {
            get
            {
                if (ByteWidth >= 16)
                {
                    return (ulong.MaxValue, ulong.MaxValue);
                }

                if (ByteWidth == 8)
                {
                    return (0UL, ulong.MaxValue);
                }

                // ByteWidth in [1,7]: value fits entirely in lo
                return (0UL, (1UL << (ByteWidth * 8)) - 1);
            }
        }
#endif

        #region Factories

        /// <summary>
        ///     Creates a <see cref="BigEndianBitWrapper" /> from a big-endian byte array.
        ///     <see cref="ByteWidth" /> is set to <paramref name="bigEndianBytes" />.Length.
        /// </summary>
        /// <param name="bigEndianBytes">A non-empty, non-null big-endian byte array of at most 16 bytes.</param>
        /// <returns>A wrapper whose value and byte width match the supplied array.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bigEndianBytes" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="bigEndianBytes" /> is empty or longer than 16 bytes.</exception>
        public static BigEndianBitWrapper FromBytes(byte[] bigEndianBytes)
        {
            if (bigEndianBytes == null)
            {
                throw new ArgumentNullException(nameof(bigEndianBytes));
            }

            if (bigEndianBytes.Length == 0)
            {
                throw new ArgumentException("Byte array cannot be empty.", nameof(bigEndianBytes));
            }

            if (bigEndianBytes.Length > 16)
            {
                throw new ArgumentException("Byte array cannot exceed 16 bytes.", nameof(bigEndianBytes));
            }

            return FromBytesCore(bigEndianBytes, bigEndianBytes.Length);
        }

        /// <summary>
        ///     Creates a <see cref="BigEndianBitWrapper" /> from a big-endian byte array, zero-padding
        ///     the most-significant side to reach <paramref name="targetWidth" />.
        ///     <see cref="ByteWidth" /> is set to <paramref name="targetWidth" />.
        /// </summary>
        /// <param name="bigEndianBytes">
        ///     A non-null big-endian byte array whose length does not exceed
        ///     <paramref name="targetWidth" />.
        /// </param>
        /// <param name="targetWidth">Target byte width (1–16).</param>
        /// <returns>
        ///     A wrapper with the value from <paramref name="bigEndianBytes" /> and
        ///     <see cref="ByteWidth" /> equal to <paramref name="targetWidth" />.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="bigEndianBytes" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="targetWidth" /> is less than 1 or greater than 16.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="bigEndianBytes" />.Length exceeds <paramref name="targetWidth" />.
        /// </exception>
        public static BigEndianBitWrapper FromBytes(byte[] bigEndianBytes, int targetWidth)
        {
            if (bigEndianBytes == null)
            {
                throw new ArgumentNullException(nameof(bigEndianBytes));
            }

            if (targetWidth < 1 || targetWidth > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(targetWidth), "Target width must be between 1 and 16.");
            }

            if (bigEndianBytes.Length > targetWidth)
            {
                throw new ArgumentException(
                    $"Byte array length ({bigEndianBytes.Length}) exceeds target width ({targetWidth}).",
                    nameof(bigEndianBytes)
                );
            }

            // Leading zero bytes don't affect the integer value, so shorter arrays can be
            // passed directly; FromBytesCore sets ByteWidth = targetWidth regardless.
            return FromBytesCore(bigEndianBytes, targetWidth);
        }

        /// <summary>
        ///     Creates a subnet mask with the top <paramref name="prefixLength" /> bits set to 1 and the
        ///     remaining host bits set to 0. For example, <c>CreateMask(4, 24)</c> produces the IPv4
        ///     netmask 255.255.255.0.
        /// </summary>
        /// <param name="byteWidth">Byte width of the address family (1–16).</param>
        /// <param name="prefixLength">Number of leading network bits to set; must be in [0, byteWidth × 8].</param>
        /// <returns>A <see cref="BigEndianBitWrapper" /> representing the subnet mask.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="byteWidth" /> is not in [1, 16], or <paramref name="prefixLength" /> is not
        ///     in [0, byteWidth × 8].
        /// </exception>
        public static BigEndianBitWrapper CreateMask(int byteWidth, int prefixLength)
        {
            if (byteWidth < 1 || byteWidth > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(byteWidth), "Byte width must be between 1 and 16.");
            }

            var totalBits = byteWidth * 8;

            if (prefixLength < 0 || prefixLength > totalBits)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prefixLength),
                    $"Prefix length must be between 0 and {totalBits} for a {byteWidth}-byte address."
                );
            }

            if (prefixLength == 0)
#if NET8_0_OR_GREATER
                return new BigEndianBitWrapper(UInt128.Zero, byteWidth);
#else
                return new BigEndianBitWrapper(0UL, 0UL, byteWidth);
#endif

            var hostBits = totalBits - prefixLength;

#if NET8_0_OR_GREATER
            var allOnes = byteWidth >= 16 ? UInt128.MaxValue : (UInt128.One << totalBits) - 1;
            // Shift right to drop the low hostBits, then shift back left to restore bit positions —
            // this clears the trailing hostBits while keeping the leading prefixLength bits set.
            var mask = hostBits == 0 ? allOnes : (allOnes >> hostBits) << hostBits;
            return new BigEndianBitWrapper(mask, byteWidth);
#else
            return CreateMaskLegacy(byteWidth, hostBits);
#endif
        }

        /// <summary>
        ///     Converts a validated big-endian byte array into a <see cref="BigEndianBitWrapper" />,
        ///     setting <see cref="ByteWidth" /> to <paramref name="byteWidth" />. Callers are responsible
        ///     for all argument validation.
        /// </summary>
        /// <param name="bytes">
        ///     Big-endian byte array whose length is at most <paramref name="byteWidth" />; bytes absent
        ///     from the leading (most-significant) side are treated as implicit zeros.
        /// </param>
        /// <param name="byteWidth">Byte width to assign to the returned wrapper; must be in [1, 16].</param>
        /// <returns>A wrapper whose numeric value equals <paramref name="bytes" /> interpreted as an unsigned big-endian integer, with <see cref="ByteWidth" /> set to <paramref name="byteWidth" />.</returns>
        private static BigEndianBitWrapper FromBytesCore(byte[] bytes, int byteWidth)
        {
#if NET8_0_OR_GREATER
            UInt128 value = 0;
            foreach (var b in bytes)
            {
                value = (value << 8) | b;
            }

            return new BigEndianBitWrapper(value, byteWidth);
#else
            ulong hi = 0,
                lo = 0;
            var len = bytes.Length;

            if (len <= 8)
            {
                foreach (var b in bytes)
                {
                    lo = (lo << 8) | b;
                }
            }
            else
            {
                // Bytes [0, hiCount) fill hi MSB-first; the remaining 8 bytes fill lo.
                var hiCount = len - 8;
                for (var i = 0; i < hiCount; i++)
                {
                    hi = (hi << 8) | bytes[i];
                }

                for (var i = hiCount; i < len; i++)
                {
                    lo = (lo << 8) | bytes[i];
                }
            }

            return new BigEndianBitWrapper(hi, lo, byteWidth);
#endif
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        ///     Builds a subnet mask for the pre-.NET 8 two-field (hi/lo) representation.
        ///     Called from <see cref="CreateMask" /> on targets that lack <see cref="UInt128" />.
        /// </summary>
        /// <param name="byteWidth">Byte width of the address family; must be in [1, 16].</param>
        /// <param name="hostBits">Number of low bits to clear; equals total bits minus prefix length. Must be &gt; 0.</param>
        /// <returns>A wrapper whose high <c>byteWidth × 8 − hostBits</c> bits are set and whose low <c>hostBits</c> bits are cleared.</returns>
        private static BigEndianBitWrapper CreateMaskLegacy(int byteWidth, int hostBits)
        {
            // Establish the all-ones ceiling for this width so that host bits can be cleared below.
            ulong allHi,
                allLo;
            if (byteWidth <= 8)
            {
                allHi = 0UL;
                allLo = byteWidth == 8 ? ulong.MaxValue : (1UL << (byteWidth * 8)) - 1;
            }
            else
            {
                allLo = ulong.MaxValue;
                allHi = byteWidth == 16 ? ulong.MaxValue : (1UL << ((byteWidth - 8) * 8)) - 1;
            }

            if (hostBits == 0)
            {
                return new BigEndianBitWrapper(allHi, allLo, byteWidth);
            }

            ulong maskHi,
                maskLo;

            if (byteWidth <= 8)
            {
                // All bits live in lo. For valid inputs (byteWidth ≤ 8, prefixLength > 0), hostBits < 64,
                // so the >= 64 guard is defensive; the shift-right-then-left clears the low hostBits.
                maskHi = 0UL;
                maskLo = hostBits >= 64 ? 0UL : (allLo >> hostBits) << hostBits;
            }
            else if (hostBits < 64)
            {
                // Host bits fall entirely within lo; the full hi word is the network prefix.
                maskHi = allHi;
                maskLo = (ulong.MaxValue >> hostBits) << hostBits; // clear the low hostBits of lo
            }
            else if (hostBits == 64)
            {
                // Entire lo word is host bits; hi carries the full network prefix.
                maskHi = allHi;
                maskLo = 0UL;
            }
            else
            {
                // Host bits spill into hi; clear hiHostBits low bits of hi and zero lo entirely.
                var hiHostBits = hostBits - 64;
                maskHi = hiHostBits >= 64 ? 0UL : (allHi >> hiHostBits) << hiHostBits;
                maskLo = 0UL;
            }

            return new BigEndianBitWrapper(maskHi, maskLo, byteWidth);
        }
#endif

        #endregion // end: Factories

        #region Arithmetic

        /// <summary>
        ///     Attempts to add <paramref name="delta" /> to this value. Returns <see langword="false" /> if
        ///     the result would exceed the maximum value for <see cref="ByteWidth" /> (overflow) or fall
        ///     below zero (underflow). On failure <paramref name="result" /> is set to
        ///     <see langword="default" />.
        /// </summary>
        /// <param name="delta">Signed amount to add; may be negative for a decrement.</param>
        /// <param name="result">Receives the result on success.</param>
        /// <returns><see langword="true" /> if the addition succeeded within the byte-width bounds.</returns>
        public bool TryAdd(long delta, out BigEndianBitWrapper result)
        {
            if (delta == 0)
            {
                result = this;
                return true;
            }

#if NET8_0_OR_GREATER
            var maxValue = MaxValueForWidth;

            if (delta > 0)
            {
                // Route through ulong before widening to UInt128 to make unsigned intent explicit.
                var ud = (UInt128)(ulong)delta;
                // Compare against headroom (maxValue - _value) rather than checking _value + ud > maxValue
                // directly: _value can be UInt128.MaxValue when ByteWidth == 16, making the sum overflow.
                if (ud > maxValue - _value)
                {
                    result = default;
                    return false;
                }

                result = new BigEndianBitWrapper(_value + ud, ByteWidth);
            }
            else
            {
                // Avoid -long.MinValue overflow; its magnitude as ulong is 2^63.
                var magnitude = delta == long.MinValue ? 9223372036854775808UL : (ulong)(-delta);
                if ((UInt128)magnitude > _value)
                {
                    result = default;
                    return false;
                }

                result = new BigEndianBitWrapper(_value - magnitude, ByteWidth);
            }
#else
            var (maxHi, maxLo) = MaxHiLoForWidth;

            if (delta > 0)
            {
                var ud = (ulong)delta;
                // Compute headroom = maxValue - _value (safe because _value ≤ maxValue).
                // If remHi > 0, headroom ≥ 2^64 which exceeds any ulong addend, so overflow is
                // impossible; only check remLo when headroom fits entirely in 64 bits.
                var (remHi, remLo) = SubtractHiLo(maxHi, maxLo, _hi, _lo);
                if (remHi == 0 && ud > remLo)
                {
                    result = default;
                    return false;
                }

                var (newHi, newLo) = AddUlongToHiLo(_hi, _lo, ud);
                result = new BigEndianBitWrapper(newHi, newLo, ByteWidth);
            }
            else
            {
                var magnitude = delta == long.MinValue ? 9223372036854775808UL : (ulong)(-delta);
                // Underflow if magnitude > _value. If _hi > 0, _value ≥ 2^64 > magnitude.
                if (_hi == 0 && magnitude > _lo)
                {
                    result = default;
                    return false;
                }

                var (newHi, newLo) = SubtractHiLo(_hi, _lo, 0UL, magnitude);
                result = new BigEndianBitWrapper(newHi, newLo, ByteWidth);
            }
#endif
            return true;
        }

        /// <summary>
        ///     Subtracts <paramref name="other" /> from this value. The caller must ensure
        ///     <c>this &gt;= other</c>; a negative result throws.
        /// </summary>
        /// <param name="other">The value to subtract.</param>
        /// <returns>The non-negative difference, retaining <see cref="ByteWidth" />.</returns>
        /// <exception cref="InvalidOperationException">The subtraction would produce a negative result.</exception>
        public BigEndianBitWrapper Subtract(BigEndianBitWrapper other)
        {
            if (CompareTo(other) < 0)
            {
                throw new InvalidOperationException("Subtraction would produce a negative result.");
            }

#if NET8_0_OR_GREATER
            return new BigEndianBitWrapper(_value - other._value, ByteWidth);
#else
            var (newHi, newLo) = SubtractHiLo(_hi, _lo, other._hi, other._lo);
            return new BigEndianBitWrapper(newHi, newLo, ByteWidth);
#endif
        }

#if !NET8_0_OR_GREATER
        /// <summary>Adds a 64-bit <paramref name="addend" /> to a 128-bit value represented as (<paramref name="hi" />, <paramref name="lo" />) fields.</summary>
        /// <param name="hi">Most-significant 64 bits of the operand.</param>
        /// <param name="lo">Least-significant 64 bits of the operand.</param>
        /// <param name="addend">The value to add to the low word; must not overflow the 128-bit total.</param>
        /// <returns>The (hi, lo) sum, with any carry from <paramref name="lo" /> propagated into hi.</returns>
        private static (ulong Hi, ulong Lo) AddUlongToHiLo(ulong hi, ulong lo, ulong addend)
        {
            var newLo = lo + addend;
            var carry = newLo < lo ? 1UL : 0UL; // Unsigned wrap-around in newLo means a carry occurred.
            return (hi + carry, newLo);
        }

        /// <summary>Subtracts a 128-bit value (<paramref name="bHi" />, <paramref name="bLo" />) from (<paramref name="aHi" />, <paramref name="aLo" />). The caller must guarantee a ≥ b.</summary>
        /// <param name="aHi">Most-significant 64 bits of the minuend.</param>
        /// <param name="aLo">Least-significant 64 bits of the minuend.</param>
        /// <param name="bHi">Most-significant 64 bits of the subtrahend.</param>
        /// <param name="bLo">Least-significant 64 bits of the subtrahend.</param>
        /// <returns>The (hi, lo) difference, with any borrow from <paramref name="aLo" /> propagated into hi.</returns>
        private static (ulong Hi, ulong Lo) SubtractHiLo(ulong aHi, ulong aLo, ulong bHi, ulong bLo)
        {
            var newLo = aLo - bLo;
            var borrow = newLo > aLo ? 1UL : 0UL; // Unsigned wrap-around in newLo means a borrow occurred.
            return (aHi - bHi - borrow, newLo);
        }
#endif

        #endregion // end: Arithmetic

        #region Bitwise Operators

        /// <summary>Computes the bitwise AND of two wrappers; the result inherits the <see cref="ByteWidth" /> of <paramref name="left" />.</summary>
        /// <param name="left">The left operand; its <see cref="ByteWidth" /> is used for the result.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>A new wrapper containing the bitwise AND of the two values.</returns>
        public static BigEndianBitWrapper operator &(BigEndianBitWrapper left, BigEndianBitWrapper right)
        {
#if NET8_0_OR_GREATER
            return new BigEndianBitWrapper(left._value & right._value, left.ByteWidth);
#else
            return new BigEndianBitWrapper(left._hi & right._hi, left._lo & right._lo, left.ByteWidth);
#endif
        }

        /// <summary>Computes the bitwise OR of two wrappers; the result inherits the <see cref="ByteWidth" /> of <paramref name="left" />.</summary>
        /// <param name="left">The left operand; its <see cref="ByteWidth" /> is used for the result.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>A new wrapper containing the bitwise OR of the two values.</returns>
        public static BigEndianBitWrapper operator |(BigEndianBitWrapper left, BigEndianBitWrapper right)
        {
#if NET8_0_OR_GREATER
            return new BigEndianBitWrapper(left._value | right._value, left.ByteWidth);
#else
            return new BigEndianBitWrapper(left._hi | right._hi, left._lo | right._lo, left.ByteWidth);
#endif
        }

        /// <summary>
        ///     Computes the bitwise NOT of <paramref name="value" />, bounded to <see cref="ByteWidth" />. Bits above the
        ///     byte-width boundary are never set in the result.
        /// </summary>
        /// <param name="value">The operand to invert.</param>
        /// <returns>A new wrapper with all bits in the <see cref="ByteWidth" /> range flipped.</returns>
        public static BigEndianBitWrapper operator ~(BigEndianBitWrapper value)
        {
#if NET8_0_OR_GREATER
            return new BigEndianBitWrapper(value.MaxValueForWidth ^ value._value, value.ByteWidth);
#else
            var (maxHi, maxLo) = value.MaxHiLoForWidth;
            return new BigEndianBitWrapper(maxHi ^ value._hi, maxLo ^ value._lo, value.ByteWidth);
#endif
        }

        #endregion // end: Bitwise Operators

        #region Conversion

        /// <summary>
        ///     Returns the value as a big-endian byte array of exactly <see cref="ByteWidth" /> bytes,
        ///     zero-padded on the most-significant side.
        /// </summary>
        /// <returns>A new byte array of length <see cref="ByteWidth" />.</returns>
        public byte[] ToBytes()
        {
            var result = new byte[ByteWidth];
#if NET8_0_OR_GREATER
            ToBytes(result.AsSpan());
#else
            ToBytesCore(result);
#endif
            return result;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        ///     Writes the value as a big-endian sequence of exactly <see cref="ByteWidth" /> bytes into
        ///     <paramref name="destination" />, zero-padded on the most-significant side.
        ///     Prefer this overload over <see cref="ToBytes()" /> when the caller can supply a
        ///     <c>stackalloc</c> buffer to avoid a heap allocation.
        /// </summary>
        /// <param name="destination">A span of at least <see cref="ByteWidth" /> bytes to write into.</param>
        public void ToBytes(Span<byte> destination)
        {
            var v = _value;
            for (var i = ByteWidth - 1; i >= 0; i--)
            {
                destination[i] = (byte)(v & 0xFF);
                v >>= 8;
            }
        }
#else
        /// <summary>
        ///     Writes the value as a big-endian sequence of exactly <see cref="ByteWidth" /> bytes into
        ///     <paramref name="result" />, zero-padding the most-significant side as needed.
        ///     This is the pre-.NET 8 equivalent of <see cref="ToBytes()" />, operating on the two-field
        ///     <c>(_hi, _lo)</c> representation. All argument validation is the caller's responsibility.
        /// </summary>
        /// <param name="result">Pre-allocated byte array of length <see cref="ByteWidth" /> to write into.</param>
        private void ToBytesCore(byte[] result)
        {
            if (ByteWidth <= 8)
            {
                var lo = _lo;
                for (var i = ByteWidth - 1; i >= 0; i--)
                {
                    result[i] = (byte)(lo & 0xFF);
                    lo >>= 8;
                }
            }
            else
            {
                // Lower 8 bytes come from lo, upper (ByteWidth - 8) bytes come from hi.
                var lo = _lo;
                for (var i = ByteWidth - 1; i >= ByteWidth - 8; i--)
                {
                    result[i] = (byte)(lo & 0xFF);
                    lo >>= 8;
                }

                var hi = _hi;
                var hiByteCount = ByteWidth - 8;
                for (var i = hiByteCount - 1; i >= 0; i--)
                {
                    result[i] = (byte)(hi & 0xFF);
                    hi >>= 8;
                }
            }
        }
#endif

        /// <summary>
        ///     Converts the value to a non-negative <see cref="BigInteger" /> using the unsigned
        ///     big-endian interpretation.
        /// </summary>
        /// <returns>A non-negative <see cref="BigInteger" /> equal to the unsigned integer value.</returns>
        public BigInteger ToBigInteger()
        {
#if NET8_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[ByteWidth];
            ToBytes(bytes);
            return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
#else
            var bytes = ToBytes();
            // BigInteger(byte[]) expects little-endian with an optional sign byte.
            // Allocate one extra byte (le[bytes.Length] stays 0) so the leading zero
            // forces an unsigned (positive) interpretation regardless of the MSB.
            var le = new byte[bytes.Length + 1];
            for (var i = 0; i < bytes.Length; i++)
            {
                le[i] = bytes[bytes.Length - 1 - i]; // reverse byte order: big-endian → little-endian
            }

            return new BigInteger(le);
#endif
        }

        #endregion // end: Conversion

        #region Formatting

        /// <summary>
        ///     Returns an uppercase hex string with no separators or prefix.
        ///     Length is always <c>ByteWidth × 2</c> characters.
        /// </summary>
        /// <returns>Example: 192.168.1.1 → "C0A80101".</returns>
        public string ToHexString()
        {
#if NET8_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[ByteWidth];
            ToBytes(bytes);
            return Convert.ToHexString(bytes);
#else
            var bytes = ToBytes();
            var sb = new StringBuilder(ByteWidth * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
#endif
        }

        /// <summary>
        ///     Returns the decimal string of the unsigned big-endian integer value.
        /// </summary>
        /// <returns>Example: 192.168.1.1 → "3232235777".</returns>
        public string ToDecimalString() => ToBigInteger().ToString(CultureInfo.InvariantCulture);

        /// <summary>
        ///     Returns a binary string of exactly <c>ByteWidth × 8</c> characters, MSB first.
        /// </summary>
        /// <returns>Example: 0x0F (1 byte) → "00001111".</returns>
        public string ToBinaryString()
        {
#if NET8_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[ByteWidth];
            ToBytes(bytes);
            Span<char> chars = stackalloc char[ByteWidth * 8];
            var pos = 0;
            foreach (var b in bytes)
            {
                for (var bit = 7; bit >= 0; bit--)
                {
                    // Shift the target bit to position 0, mask to isolate it, then map 0→'0' and 1→'1'.
                    chars[pos++] = (char)('0' + ((b >> bit) & 1));
                }
            }

            return new string(chars);
#else
            var bytes = ToBytes();
            var sb = new StringBuilder(ByteWidth * 8);
            foreach (var b in bytes)
            {
                for (var bit = 7; bit >= 0; bit--)
                {
                    // Shift the target bit to position 0, mask to isolate it; Append(int) renders 0 or 1.
                    sb.Append((b >> bit) & 1);
                }
            }

            return sb.ToString();
#endif
        }

        /// <summary>
        ///     Formats the value using the specified format specifier.
        /// </summary>
        /// <param name="format">
        ///     <list type="bullet">
        ///         <item>
        ///             <term><c>"HC"</c></term>
        ///             <description>Hex compact — uppercase, no separators (e.g. "C0A80101").</description>
        ///         </item>
        ///         <item>
        ///             <term><c>"IBE"</c></term>
        ///             <description>Integer big-endian — decimal string of the unsigned value.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>"b"</c></term>
        ///             <description>Binary string, MSB first, length ByteWidth × 8.</description>
        ///         </item>
        ///         <item>
        ///             <term><c>null</c> or <c>"G"</c></term>
        ///             <description>Default — same as "HC".</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="formatProvider">Ignored; present to satisfy <see cref="IFormattable" />.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="FormatException">An unrecognised format specifier was supplied.</exception>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format) || format == "G")
            {
                return ToHexString();
            }

            switch (format)
            {
                case "HC":
                    return ToHexString();
                case "IBE":
                    return ToDecimalString();
                case "b":
                    return ToBinaryString();
                default:
                    throw new FormatException($"Unknown format specifier '{format}' for {nameof(BigEndianBitWrapper)}.");
            }
        }

        /// <summary>Returns the hex compact representation (same as <see cref="ToHexString" />).</summary>
        /// <returns>An uppercase hex string of length <c>ByteWidth × 2</c>.</returns>
        public override string ToString() => ToHexString();

        #endregion // end: Formatting

        #region Comparison

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

        #endregion // end: Comparison

        #region Equality

        /// <summary>
        ///     Returns <see langword="true" /> when <paramref name="other" /> has the same numeric value
        ///     and the same <see cref="ByteWidth" />.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><see langword="true" /> if equal.</returns>
        public bool Equals(BigEndianBitWrapper other)
        {
#if NET8_0_OR_GREATER
            return _value == other._value && ByteWidth == other.ByteWidth;
#else
            return _hi == other._hi && _lo == other._lo && ByteWidth == other.ByteWidth;
#endif
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is BigEndianBitWrapper other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
#if NET8_0_OR_GREATER
            return HashCode.Combine(_value, ByteWidth);
#else
            return HashCode.Combine(_hi, _lo, ByteWidth);
#endif
        }

        /// <summary>Returns <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal in both numeric value and <see cref="ByteWidth" />.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true" /> if both operands are equal; otherwise <see langword="false" />.</returns>
        public static bool operator ==(BigEndianBitWrapper left, BigEndianBitWrapper right) => left.Equals(right);

        /// <summary>Returns <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> differ in numeric value or <see cref="ByteWidth" />.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true" /> if the operands are not equal; otherwise <see langword="false" />.</returns>
        public static bool operator !=(BigEndianBitWrapper left, BigEndianBitWrapper right) => !left.Equals(right);

        #endregion // end: Equality
    }
}
