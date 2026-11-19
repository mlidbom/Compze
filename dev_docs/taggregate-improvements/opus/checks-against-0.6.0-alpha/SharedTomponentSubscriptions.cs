using System;
using System.Collections.Generic;
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;
#pragma warning disable
namespace Checks;

//The shared tomponent: its tevents are bare ITevents of its own hierarchy.
public interface IPostalAddressTevent : ITevent
{
   interface Changed : IPostalAddressTevent { string Street { get; } }
}
public class PostalAddressTevent : IPostalAddressTevent
{
   public class Changed(string street) : PostalAddressTevent, IPostalAddressTevent.Changed { public string Street { get; } = street; }
}
public class PostalAddress : SharedTomponent<IPostalAddressTevent>
{
   public PostalAddress(ISharedTomponentSlot<IPostalAddressTevent> slot) : base(slot) =>
      RegisterTeventAppliers().For<IPostalAddressTevent.Changed>(tevent => Street = tevent.Street);
   public string? Street { get; private set; }
   public void ChangeStreet(string street) => Publish(new PostalAddressTevent.Changed(street));
}

//A marker, outside every owner's hierarchy, that each owner's adopting wrapper interfaces implement.
public interface IAddressAdopted : ITevent;

//Owner one: an order with two slots of the same tomponent type.
public interface IOrderTevent : ITaggregateTevent { interface Created : IOrderTevent, ITaggregateCreatedTevent; }
public interface IOrderTevent<out T> : ITaggregateTevent<T> where T : IOrderTevent;
public class OrderTevent : TaggregateTevent, IOrderTevent
{
   protected OrderTevent() {}
   OrderTevent(TaggregateId id) : base(id) {}
   public class Created(TaggregateId id) : OrderTevent(id), IOrderTevent.Created;
}
public class OrderTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IOrderTevent<T> where T : IOrderTevent;
public interface IShippingAddressTevent<out T> : IOrderTevent, IAddressAdopted, IPublisherTevent<T> where T : IPostalAddressTevent;
public class ShippingAddressTevent<T>(T tevent) : OrderTevent, IShippingAddressTevent<T> where T : IPostalAddressTevent { public T Tevent { get; } = tevent; }
public interface IBillingAddressTevent<out T> : IOrderTevent, IAddressAdopted, IPublisherTevent<T> where T : IPostalAddressTevent;
public class BillingAddressTevent<T>(T tevent) : OrderTevent, IBillingAddressTevent<T> where T : IPostalAddressTevent { public T Tevent { get; } = tevent; }
public class Order : Taggregate<Order, IOrderTevent, OrderTevent, IOrderTevent<IOrderTevent>, OrderTevent<OrderTevent>>
{
   public PostalAddress Shipping { get; }
   public PostalAddress Billing { get; }
   Order()
   {
      RegisterTeventAppliers().For<IOrderTevent.Created>(_ => {});
      Shipping = new PostalAddress(new SharedTomponentSlot<IOrderTevent, OrderTevent, IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>>(this, typeof(ShippingAddressTevent<IPostalAddressTevent>)));
      Billing = new PostalAddress(new SharedTomponentSlot<IOrderTevent, OrderTevent, IPostalAddressTevent, IBillingAddressTevent<IPostalAddressTevent>>(this, typeof(BillingAddressTevent<IPostalAddressTevent>)));
   }
   public static Order Create() { var order = new Order(); order.Publish(new OrderTevent.Created(new TaggregateId())); return order; }
}

//Owner two: an unrelated taggregate, with a tevent root of its own, and one slot.
public interface IWarehouseTevent : ITaggregateTevent { interface Created : IWarehouseTevent, ITaggregateCreatedTevent; }
public interface IWarehouseTevent<out T> : ITaggregateTevent<T> where T : IWarehouseTevent;
public class WarehouseTevent : TaggregateTevent, IWarehouseTevent
{
   protected WarehouseTevent() {}
   WarehouseTevent(TaggregateId id) : base(id) {}
   public class Created(TaggregateId id) : WarehouseTevent(id), IWarehouseTevent.Created;
}
public class WarehouseTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IWarehouseTevent<T> where T : IWarehouseTevent;
public interface IWarehouseAddressTevent<out T> : IWarehouseTevent, IAddressAdopted, IPublisherTevent<T> where T : IPostalAddressTevent;
public class WarehouseAddressTevent<T>(T tevent) : WarehouseTevent, IWarehouseAddressTevent<T> where T : IPostalAddressTevent { public T Tevent { get; } = tevent; }
public class Warehouse : Taggregate<Warehouse, IWarehouseTevent, WarehouseTevent, IWarehouseTevent<IWarehouseTevent>, WarehouseTevent<WarehouseTevent>>
{
   public PostalAddress Address { get; }
   Warehouse()
   {
      RegisterTeventAppliers().For<IWarehouseTevent.Created>(_ => {});
      Address = new PostalAddress(new SharedTomponentSlot<IWarehouseTevent, WarehouseTevent, IPostalAddressTevent, IWarehouseAddressTevent<IPostalAddressTevent>>(this, typeof(WarehouseAddressTevent<IPostalAddressTevent>)));
   }
   public static Warehouse Create() { var warehouse = new Warehouse(); warehouse.Publish(new WarehouseTevent.Created(new TaggregateId())); return warehouse; }
}

public static class Program
{
   public static void Main()
   {
      //What a consumer's corpus-wide dispatcher would be: every taggregate's wrapped tevents dispatched into one dispatcher.
      var corpus = IMutableTeventDispatcher<ITevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled);
      var byWrapperOfAWrapper = new List<string>();
      var byMarker = new List<string>();
      var byWrapperOfAWrapperForChangedOnly = new List<string>();
      corpus.Register()
            .ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent>>>(it => byWrapperOfAWrapper.Add($"{it.Tevent.GetType().Name.Split('`')[0]}: {((IPostalAddressTevent.Changed)it.Tevent.Tevent).Street}"))
            .ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent.Changed>>>(it => byWrapperOfAWrapperForChangedOnly.Add(it.Tevent.Tevent.Street))
            .ForGenericTevent<IAddressAdopted>(it => byMarker.Add($"{it.GetType().Name.Split('`')[0]}: {((IPublisherTevent<IPostalAddressTevent.Changed>)it).Tevent.Street}"));

      var order = Order.Create();
      var warehouse = Warehouse.Create();
      ((ITaggregate)order).TeventStream.Subscribe(new Observer(corpus.Dispatch));
      ((ITaggregate)warehouse).TeventStream.Subscribe(new Observer(corpus.Dispatch));

      order.Shipping.ChangeStreet("Shipping street");
      order.Billing.ChangeStreet("Billing street");
      warehouse.Address.ChangeStreet("Warehouse street");

      Console.WriteLine($"ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent>>>: {string.Join(" | ", byWrapperOfAWrapper)}");
      Console.WriteLine($"ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent.Changed>>>: {string.Join(" | ", byWrapperOfAWrapperForChangedOnly)}");
      Console.WriteLine($"ForGenericTevent<IAddressAdopted>, a marker on each owner's adopting wrappers: {string.Join(" | ", byMarker)}");
   }

   class Observer(Action<ITaggregateTevent<ITaggregateTevent>> onNext) : IObserver<ITaggregateTevent<ITaggregateTevent>>
   {
      public void OnNext(ITaggregateTevent<ITaggregateTevent> value) => onNext(value);
      public void OnError(Exception error) {}
      public void OnCompleted() {}
   }
}
