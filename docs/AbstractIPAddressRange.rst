.. _AbstractIPAddressRange:

AbstractIPAddressRange
======================

The ``AbstractIPAddressRange`` is an abstract implementation of :ref:`IIPAddressRange`. It is extended by both :ref:`IPAddressRange`, and :ref:`Subnet`.

.. note::

   **Overlaps — symmetry fix:** ``Overlaps(IIPAddressRange)`` now returns ``true`` in both directions when one range is wholly contained inside the other. Previously, calling ``inner.Overlaps(outer)`` returned ``false`` when ``inner`` was wholly inside ``outer``.

   **ContainsAnyPrivateAddresses / ContainsAllPublicAddresses — range-overlap fix:** these methods previously used an endpoint heuristic that produced incorrect results when both endpoints of a range were public but the range's interior spanned a private subnet (e.g., ``11.0.0.0 – 173.0.0.0`` spans ``172.16.0.0/12``). All four ``ContainsAny/AllPrivate/PublicAddresses`` methods now use range-overlap detection against ``SubnetUtilities.PrivateIPAddressRangesList``.

Functionality Implementation
----------------------------

IFormatable
^^^^^^^^^^^

Extensions of ``AbstractIPAddressRange``, depending on overrides and implementation, provide a general format (``G``, ``g``, or empty string) that will express a range of IP addresses in a ``head - tail`` format for example ``192.168.1.1 - 192.168.1.10``.

.. code-block:: c#
   :emphasize-lines: 12
   :caption: AbstractIPAddressRange  IFormattable Example
   :name: AbstractIPAddressRange  IFormattable Example

   [Fact]
   public void IFormattable_Example()
   {
       // Arrange
       var head = IPAddress.Parse("192.168.0.0");
       var tail = IPAddress.Parse("192.168.128.0");
       var ipAddressRange = new IPAddressRange(head, tail);

       const string expected = "192.168.0.0 - 192.168.128.0";

       // Act
       var formattableString = string.Format("{0:g}", ipAddressRange);

       // Assert
       Assert.Equal(expected, formattableString);
   }
