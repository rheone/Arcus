using System.Collections.Generic;
using System.Linq;

namespace Arcus.Tests
{
    /// <summary>
    ///     Provides an <see cref="IEnumerable{T}" />-returning reverse operation for byte arrays
    ///     used by test code. This method avoids overload resolution selecting
    ///     <c>MemoryExtensions.Reverse(Span&lt;T&gt;)</c> instead of
    ///     <c>Enumerable.Reverse&lt;T&gt;()</c>, preserving the expected behavior and return type.
    /// </summary>
    internal static class TestByteArrayExtensions
    {
        /// <summary>
        ///     Reverse bytes in an array. This is needed to work around C# 13 overload resolution changes that prefer
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>bytes in reverse</returns>
        public static IEnumerable<byte> Reverse(this byte[] bytes) => Enumerable.Reverse(bytes);
    }
}
