using Compze.Abstractions.Public;
using Compze.Tessaging;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

interface IOrderTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested tevent interface follows semantic events naming convention (compze.net/paradigms/semantic-events/event-naming.html)
   interface Created : IOrderTevent, ITaggregateCreatedTevent;
#pragma warning restore CA1715
}

interface IOrderTevent<out T> : ITaggregateTevent<T> where T : IOrderTevent;

class OrderTevent : TaggregateTevent, IOrderTevent
{
   protected OrderTevent() {}
   OrderTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId taggregateId) : OrderTevent(taggregateId), IOrderTevent.Created;
}

class OrderTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IOrderTevent<T> where T : IOrderTevent;

///<summary>The adopting wrapper tevent of the order's SHIPPING address slot: an <see cref="IOrderTevent"/> that adopts an<br/>
/// <see cref="IPostalAddressTevent"/> into the order's tevent hierarchy and identifies WHICH postal-address member published it.</summary>
interface IShippingAddressTevent<out T> : IOrderTevent, IPublisherTevent<T> where T : IPostalAddressTevent;

class ShippingAddressTevent<T>(T tevent) : OrderTevent, IShippingAddressTevent<T> where T : IPostalAddressTevent
{
   public T Tevent { get; } = tevent;
}

interface IBillingAddressTevent<out T> : IOrderTevent, IPublisherTevent<T> where T : IPostalAddressTevent;

class BillingAddressTevent<T>(T tevent) : OrderTevent, IBillingAddressTevent<T> where T : IPostalAddressTevent
{
   public T Tevent { get; } = tevent;
}

///<summary>The owner side of the shared tomponent fixture: an order with TWO members of the same shared tomponent type.<br/>
/// Only the slots' adopting wrapper tevent types tell the two apart - the inner tevent types are identical.</summary>
class OrderTaggregate : Taggregate<OrderTaggregate, IOrderTevent, OrderTevent, IOrderTevent<IOrderTevent>, OrderTevent<OrderTevent>>
{
   public PostalAddress ShippingAddress { get; }
   public PostalAddress BillingAddress { get; }

   OrderTaggregate()
   {
      RegisterTeventAppliers().For<IOrderTevent.Created>(_ => {}); //The base class applies the taggregate id and version; the order has no state of its own from creation.
      ShippingAddress = new PostalAddress(new SharedTomponentSlot<IOrderTevent, OrderTevent, IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>>(this, typeof(ShippingAddressTevent<IPostalAddressTevent>)));
      BillingAddress = new PostalAddress(new SharedTomponentSlot<IOrderTevent, OrderTevent, IPostalAddressTevent, IBillingAddressTevent<IPostalAddressTevent>>(this, typeof(BillingAddressTevent<IPostalAddressTevent>)));
   }

   public static OrderTaggregate Create()
   {
      var order = new OrderTaggregate();
      order.Publish(new OrderTevent.Created(new TaggregateId()));
      return order;
   }

   public static OrderTaggregate LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var order = new OrderTaggregate();
      ((ITaggregate)order).LoadFromHistory(history);
      return order;
   }
}
