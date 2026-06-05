using System.Collections.Generic;
using System.Linq;

namespace Arcus.Tests
{
    // C# 13 (SDK 10) changed overload resolution to prefer MemoryExtensions.Reverse(Span<T>) over
    // Enumerable.Reverse<T>() for arrays. This method (same namespace) takes priority and restores
    // the expected IEnumerable<byte> return type so existing test code compiles unchanged.
    internal static class TestByteArrayExtensions
    {
        public static IEnumerable<byte> Reverse(this byte[] bytes) => Enumerable.Reverse(bytes);
    }
}
