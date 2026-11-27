using Compze.Must;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>A slot is handed its adopting wrapper tevent implementation as a <see cref="System.Type"/>, which the compiler can't check.<br/>
/// Handed another slot's, the slot's tevents would be wrapped as the other slot's and routed to its tomponent, with no error.</summary>
public class Given_an_order_whose_shipping_slot_is_handed_the_billing_slots_adopting_wrapper_tevent_implementation
{
   [XF] public void making_the_slot_throws_an_ArgumentException_naming_the_wrong_implementation() =>
      Invoking(() => new SharedTomponentSlot<OrderTaggregate, IOrderTevent, OrderTevent, IPostalAddressTevent, IShippingAddressTevent<IPostalAddressTevent>>(
                  typeof(BillingAddressTevent<>), static order => order.ShippingAddress))
        .Must().Throw<ArgumentException>()
        .Which.Message.Must().Contain(nameof(BillingAddressTevent<>));
}
