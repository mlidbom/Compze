using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>An order whose shipping-address slot is handed the BILLING slot's adopting wrapper tevent implementation by mistake,<br/>
/// the copy-and-paste error a slot must refuse: its tevents would otherwise be routed to the billing address.</summary>
class OrderTaggregateWithAMisWiredShippingSlot : Taggregate<OrderTaggregateWithAMisWiredShippingSlot, IOrderTevent, OrderTevent, IOrderTevent<IOrderTevent>, OrderTevent<OrderTevent>>
{
   public PostalAddress ShippingAddress { get; }

   OrderTaggregateWithAMisWiredShippingSlot() =>
      ShippingAddress = new PostalAddress(new SharedTomponentSlot<IOrderTevent, OrderTevent, IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>>(this, typeof(BillingAddressTevent<IPostalAddressTevent>)));

   public static OrderTaggregateWithAMisWiredShippingSlot Construct() => new();
}
