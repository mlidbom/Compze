using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>An order whose shipping address is constructed with the BILLING slot by mistake, the copy-and-paste error the slots must refuse:<br/>
/// the shipping address would publish as the billing address, and its changes would be applied to the billing address.</summary>
class OrderWithItsShippingAddressInTheBillingSlot : Taggregate<OrderWithItsShippingAddressInTheBillingSlot, IOrderTevent, OrderTevent, IOrderTevent<IOrderTevent>, OrderTevent<OrderTevent>>
{
   static class Slots
   {
      internal static readonly SharedTomponentSlotOfAClass<OrderWithItsShippingAddressInTheBillingSlot, IOrderTevent, OrderTevent, IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>> Shipping =
         new(typeof(ShippingAddressTevent<>), static order => order.ShippingAddress);

      internal static readonly SharedTomponentSlotOfAClass<OrderWithItsShippingAddressInTheBillingSlot, IOrderTevent, OrderTevent, IPostalAddressTevent, IBillingAddressTevent<IPostalAddressTevent>> Billing =
         new(typeof(BillingAddressTevent<>), static order => order.BillingAddress);
   }

   static readonly TeventRouting<OrderWithItsShippingAddressInTheBillingSlot, IOrderTevent> Appliers = DeclareAppliers(it => it
     .For<IOrderTevent.Created>(static (_, _) => {})
     .SharedTomponent(Slots.Shipping)
     .SharedTomponent(Slots.Billing));

   protected override TeventRouting TeventAppliers => Appliers;

   public PostalAddress ShippingAddress { get; }
   public PostalAddress BillingAddress { get; }

   OrderWithItsShippingAddressInTheBillingSlot()
   {
      ShippingAddress = new PostalAddress(Slots.Billing.In(this));
      BillingAddress = new PostalAddress(Slots.Billing.In(this));
   }

   public static OrderWithItsShippingAddressInTheBillingSlot Create()
   {
      var order = new OrderWithItsShippingAddressInTheBillingSlot();
      order.Publish(new OrderTevent.Created(new TaggregateId()));
      return order;
   }

   public static OrderWithItsShippingAddressInTheBillingSlot LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var order = new OrderWithItsShippingAddressInTheBillingSlot();
      ((ITaggregate)order).LoadFromHistory(history);
      return order;
   }
}
