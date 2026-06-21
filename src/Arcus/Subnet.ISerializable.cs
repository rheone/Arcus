#if NETSTANDARD2_0
using System;
using System.Runtime.Serialization;

namespace Arcus
{
    /// <content>
    ///     <see cref="Subnet"/> implementation of <see cref="ISerializable"/>
    /// </content>
    public partial class Subnet : ISerializable
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="info" /> is <see langword="null" /></exception>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(this.BroadcastAddress), this.BroadcastAddress.GetAddressBytes());
            info.AddValue(nameof(this.RoutingPrefix), this.RoutingPrefix);
            info.AddValue("SerVersion", 2);
            info.AddValue(nameof(this.MaxEnumerationExponent), this.MaxEnumerationExponent);
        }
    }
}
#endif
