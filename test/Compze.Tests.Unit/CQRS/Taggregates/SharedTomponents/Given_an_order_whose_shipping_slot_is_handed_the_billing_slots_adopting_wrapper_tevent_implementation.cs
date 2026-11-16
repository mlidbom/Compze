using Compze.Must;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>A slot is handed its adopting wrapper tevent implementation as a <see cref="System.Type"/>, which the compiler can't check.<br/>
/// Handed another slot's, the slot's tevents would be wrapped as the other slot's and routed to its tomponent, with no error.</summary>
public class Given_an_order_whose_shipping_slot_is_handed_the_billing_slots_adopting_wrapper_tevent_implementation
{
   [XF] public void constructing_the_order_throws_an_ArgumentException_naming_the_wrong_implementation() =>
      Invoking(() => OrderTaggregateWithAMisWiredShippingSlot.Construct())
        .Must().Throw<ArgumentException>()
        .Which.Message.Must().Contain(nameof(BillingAddressTevent<>));
}
