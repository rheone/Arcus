using System.Collections.Generic;
using System.Linq;
using Arcus.Math;

namespace Arcus
{
    /// <content>
    ///     <see cref="IPAddressRange"/> static factory methods
    /// </content>
    public partial class IPAddressRange
    {
        #region static methods

        /// <summary>
        ///     Attempt collapse the given input of ranges into fewer ranges thus optimizing
        ///     Ranges that overlap, or butt against each other may be collapsed into a single range
        /// </summary>
        /// <param name="ranges">ranges to collapse</param>
        /// <param name="result">resulting ranges post collapse</param>
        /// <returns>true on success</returns>
        public static bool TryCollapseAll(IEnumerable<IPAddressRange> ranges, out IEnumerable<IPAddressRange> result)
        {
            var rangeList = (ranges ?? []).ToList();

            // item null check
            if (rangeList.Contains(null))
            {
                result = [];
                return false;
            }

            // no ranges provided
            if (!rangeList.Any()) // no ranges
            {
                result = [];
                return true; // assume success
            }

            // all families don't match match
            if (rangeList.Any(r => r.AddressFamily != rangeList[0].AddressFamily))
            {
                result = [];
                return false;
            }

            // sort range list, has to be done post validation check, as invalid cannot be sorted
            rangeList = [.. rangeList.OrderBy(r => r)];

            var resultList = new List<IPAddressRange>
            {
                rangeList[0], // start with first item in the sorted list of ranges
            };

            // iterate over items in range list, no need to iterate over first as it is included by default
            foreach (var range in rangeList.Skip(1))
            {
                var last = resultList[resultList.Count - 1]; // take end of result to process over (is first in first iteration)

                // can be assumed that assume that the range head is greater than or equal to last head because everything is ordered

                // if TryMerge succeeded then overlap exists, merge overlap and re-assign to end of results
                if (TryMerge(last, range, out var merge))
                {
                    resultList[resultList.Count - 1] = merge; // overwrite with merged values
                    continue;
                }

                // could not merge, simply add to end and iterate to next
                resultList.Add(range);
            }

            result = resultList;
            return true;
        }

        /// <summary>
        ///     Rebuild the initial range as an <see cref="IEnumerable{T}" /> of ranges excluding the excluded ranges.
        ///     Excluded ranges are expected to each be sub-ranges of the initial range.
        /// </summary>
        /// <param name="initialRange">the initial <see cref="IPAddressRange" /> to exclude from</param>
        /// <param name="excludedRanges">
        ///     the various <see cref="IPAddressRange" /> to exclude from the
        ///     <paramref name="initialRange" />
        /// </param>
        /// <param name="result">the resulting  <see cref="IPAddressRange" /> <see cref="IEnumerable{T}" /></param>
        /// <returns>true on success</returns>
        /// <remarks>
        ///     <para>
        ///         A return value of <see langword="false" /> indicates an error condition (null input, address-family mismatch,
        ///         or null elements in <paramref name="excludedRanges" />), not an empty result set. A <see langword="true" />
        ///         return with an empty <paramref name="result" /> means the exclusions cover the entire
        ///         <paramref name="initialRange" />.
        ///     </para>
        ///     <para>
        ///         <b>Family-maximum boundary:</b> when an exclusion ends at the family maximum address
        ///         (e.g., <c>255.255.255.255</c> for IPv4 or <c>ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff</c> for IPv6),
        ///         no trailing segment can be produced after it. The method returns <see langword="true" /> with only the
        ///         leading segment in <paramref name="result" /> (which may itself be empty if the exclusion also covers
        ///         the start of the range).
        ///     </para>
        ///     <para>
        ///         <b>Family-minimum boundary:</b> when an exclusion starts at the family minimum address
        ///         (e.g., <c>0.0.0.0</c> for IPv4 or <c>::</c> for IPv6), no leading segment can be produced before it.
        ///         The method returns <see langword="true" /> with only the trailing segment in <paramref name="result" />
        ///         (which may itself be empty if the exclusion also covers the end of the range).
        ///     </para>
        /// </remarks>
        public static bool TryExcludeAll(
            IPAddressRange initialRange,
            IEnumerable<IPAddressRange> excludedRanges,
            out IEnumerable<IPAddressRange> result
        )
        {
            if (initialRange is null || excludedRanges is null)
            {
                result = [];
                return false;
            }

            var excludedRangesList = excludedRanges as IList<IPAddressRange> ?? [.. excludedRanges];

            // item null check
            if (excludedRangesList.Any(r => r is null))
            {
                result = [];
                return false;
            }

            // no ranges to exclude; return copy of original
            if (!excludedRangesList.Any())
            {
                result = [new(initialRange.Head, initialRange.Tail)];
                return true;
            }

            // all families must match
            if (excludedRangesList.Any(r => r.AddressFamily != initialRange.AddressFamily))
            {
                result = [];
                return false;
            }

            // results is initialized with a *copy* of initialRange
            var resultList = new List<IPAddressRange> { new(initialRange.Head, initialRange.Tail) };

            foreach (var exclusion in excludedRangesList)
            {
                var last = resultList[resultList.Count - 1];
                var lastIndex = resultList.Count - 1;

                if (exclusion.Contains(last))
                {
                    resultList.RemoveAt(lastIndex);
                    break;
                }

                if (exclusion.Contains(initialRange.Tail))
                {
                    // exclusion reaches the end of the initial range; no trailing segment possible.
                    // Retain the leading segment only if the exclusion does not also start at the family minimum.
                    if (!exclusion.Head.IsAtMin())
                    {
                        var head = resultList[lastIndex].Head;
                        var tail = exclusion.Head.Increment(-1);
                        resultList[lastIndex] = new IPAddressRange(head, tail);
                    }
                    else
                    {
                        // exclusion starts at the family minimum; nothing remains
                        resultList.RemoveAt(lastIndex);
                    }

                    break;
                }

                // exclusion contains head of remaining segment
                if (exclusion.Contains(last.Head))
                {
                    if (exclusion.Tail.IsAtMax())
                    {
                        // exclusion reaches the family maximum; nothing remains in this segment
                        resultList.RemoveAt(lastIndex);
                        break;
                    }

                    // push head one point beyond tail of exclusion
                    var head = exclusion.Tail.Increment();
                    var tail = resultList[lastIndex].Tail;
                    resultList[lastIndex] = new IPAddressRange(head, tail);
                    continue;
                }

                // exclusion is within last; carve last into a leading and trailing segment
                if (!last.Overlaps(exclusion))
                {
                    throw new System.InvalidOperationException("An unexpected overlap check operation occurred");
                }

                if (!exclusion.Head.IsAtMin())
                {
                    // retain the leading segment up to one address before the exclusion starts
                    resultList[lastIndex] = new IPAddressRange(resultList[lastIndex].Head, exclusion.Head.Increment(-1));
                }
                else
                {
                    // exclusion starts at the family minimum; no leading segment
                    resultList.RemoveAt(lastIndex);
                }

                if (!exclusion.Tail.IsAtMax())
                {
                    // add the trailing segment from one address after the exclusion ends to the end of the initial range
                    resultList.Add(new IPAddressRange(exclusion.Tail.Increment(), initialRange.Tail));
                }
            }

            result = resultList;
            return true;
        }

        /// <summary>
        ///     Merge two touching or overlapping address ranges
        /// </summary>
        /// <param name="left">the left operand</param>
        /// <param name="right">the right operand</param>
        /// <param name="mergedRange">the resulting <see cref="IPAddressRange" /></param>
        /// <returns>true on success</returns>
        public static bool TryMerge(IPAddressRange left, IPAddressRange right, out IPAddressRange mergedRange)
        {
            if (left is null || right is null || left.AddressFamily != right.AddressFamily)
            {
                mergedRange = null;
                return false;
            }

            // overlap or touch occurs
            if (left.Overlaps(right) || left.Touches(right))
            {
                var newHead = IPAddressMath.Min(left.Head, right.Head);
                var newTail = IPAddressMath.Max(left.Tail, right.Tail);
                mergedRange = new IPAddressRange(newHead, newTail);

                return true;
            }

            mergedRange = null;
            return false;
        }

        #endregion // end: static methods
    }
}
