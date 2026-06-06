using System;
using System.Globalization;

namespace Arcus
{
    /// <content>
    ///     <see cref="AbstractIPAddressRange"/> implementation of <see cref="IFormattable"/>
    /// </content>
    public abstract partial class AbstractIPAddressRange
    {
        #region Formatting

        /// <inheritdoc />
        public override string ToString()
        {
            return this.ToString("G", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format?.Trim())
            {
                case null:
                case "":

                // general formats
                case "g":
                case "G":
                    return $"{this.Head} - {this.Tail}";
                default:
                    throw new FormatException($"The format \"{format}\" is not supported.");
            }
        }

        #endregion // end: Formatting
    }
}
