using Compze.Abstractions.Tessaging.Public;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>Shared tomponents through one taggregate: an order owns TWO <see cref="PostalAddress"/> members whose inner tevent types are identical,<br/>
/// so ONLY each slot's adopting wrapper tevent (<see cref="IShippingAddressTevent{T}"/> vs <see cref="IBillingAddressTevent{T}"/>) tells them apart -<br/>
/// for routing tevents back to the right member, for what history records, and for what subscribers can subscribe to.</summary>
public class Given_an_order_taggregate_with_shipping_and_billing_shared_postal_address_tomponents
{
   readonly OrderTaggregate _order = OrderTaggregate.Create();

   public class after_the_shipping_address_street_changes : Given_an_order_taggregate_with_shipping_and_billing_shared_postal_address_tomponents
   {
      readonly List<ITaggregateIdentifyingTevent<ITaggregateTevent>> _committedTevents = [];
      readonly ITaggregateIdentifyingTevent<ITaggregateTevent> _committedShippingChangeTevent;

      public after_the_shipping_address_street_changes()
      {
         _order.ShippingAddress.ChangeStreet("221B Baker Street");
         ((ITaggregate)_order).Commit(_committedTevents.AddRange);
         _committedShippingChangeTevent = _committedTevents[^1];
      }

      [XF] public void the_shipping_address_applies_the_change() => _order.ShippingAddress.Street.Must().NotBeNull().Be("221B Baker Street");
      [XF] public void the_billing_address_is_unaffected() => _order.BillingAddress.Street.Must().BeNull();

      [XF] public void the_committed_tevent_is_the_orders_wrapping_of_the_shipping_slots_adoption_of_the_change() =>
         (_committedShippingChangeTevent is IOrderTevent<IShippingAddressTevent<IPostalAddressTevent.Changed>>).Must().BeTrue();

      [XF] public void the_committed_tevent_does_not_identify_the_billing_slot() =>
         (_committedShippingChangeTevent is IOrderTevent<IBillingAddressTevent<IPostalAddressTevent.Changed>>).Must().BeFalse();

      public class and_the_committed_change_is_dispatched_to_subscribers : after_the_shipping_address_street_changes
      {
         readonly List<IPostalAddressTevent.Changed> _receivedByFullPublicationPathSubscriber = [];
         readonly List<IPostalAddressTevent.Changed> _receivedByShippingSlotSubscriber = [];
         readonly List<IPostalAddressTevent.Changed> _receivedByBareInnerTeventSubscriber = [];

         public and_the_committed_change_is_dispatched_to_subscribers()
         {
            var dispatcher = IMutableTeventDispatcher<ITevent>.New();
            dispatcher.Register()
                      .ForWrapped<IOrderTevent<IShippingAddressTevent<IPostalAddressTevent.Changed>>>(wrapped => _receivedByFullPublicationPathSubscriber.Add(wrapped.Tevent.Tevent))
                      .ForWrapped<IPublisherIdentifyingTevent<IShippingAddressTevent<IPostalAddressTevent.Changed>>>(wrapped => _receivedByShippingSlotSubscriber.Add(wrapped.Tevent.Tevent))
                      .For<IPostalAddressTevent.Changed>(_receivedByBareInnerTeventSubscriber.Add);

            dispatcher.Dispatch(_committedShippingChangeTevent);
         }

         [XF] public void the_subscriber_to_the_full_publication_path_receives_the_change() => _receivedByFullPublicationPathSubscriber.Single().Street.Must().Be("221B Baker Street");

         [XF] public void the_subscriber_to_the_shipping_slot_regardless_of_owner_wrapper_receives_the_same_change() =>
            _receivedByShippingSlotSubscriber.Single().Must().ReferenceEqual(_receivedByFullPublicationPathSubscriber.Single());

         ///<summary>Routing auto-translates and unwraps exactly ONE publisher-wrapper level; an adopted tevent is two levels deep,<br/>
         /// so subscribing to the bare inner tevent type does not match it - subscribe at a grain the wrapped type structure expresses.</summary>
         [XF] public void the_subscriber_to_the_bare_inner_tevent_type_receives_nothing() => _receivedByBareInnerTeventSubscriber.Must().BeEmpty();
      }

      public class and_a_new_order_instance_is_loaded_from_the_committed_history : after_the_shipping_address_street_changes
      {
         readonly OrderTaggregate _reloadedOrder;

         public and_a_new_order_instance_is_loaded_from_the_committed_history() => _reloadedOrder = OrderTaggregate.LoadFromHistory(_committedTevents);

         [XF] public void the_reloaded_shipping_address_street_is_restored() => _reloadedOrder.ShippingAddress.Street.Must().NotBeNull().Be("221B Baker Street");
         [XF] public void the_reloaded_billing_address_is_still_unaffected() => _reloadedOrder.BillingAddress.Street.Must().BeNull();
      }
   }
}
