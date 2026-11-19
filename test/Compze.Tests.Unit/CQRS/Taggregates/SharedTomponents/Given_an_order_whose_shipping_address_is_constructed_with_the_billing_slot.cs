using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;

///<summary>The slot a shared tomponent is constructed with is handed over as a value the compiler can't tie to the member the tomponent is kept in.<br/>
/// Constructed with another slot of its owner's class, the tomponent would publish as that slot's, and be applied the other tomponent's tevents.</summary>
public class Given_an_order_whose_shipping_address_is_constructed_with_the_billing_slot
{
   [XF] public void changing_its_shipping_street_throws_naming_the_billing_slot() =>
      Invoking(() => OrderWithItsShippingAddressInTheBillingSlot.Create().ShippingAddress.ChangeStreet("1 Harbour Road"))
        .Must().Throw<StateAssertionFailedException>()
        .Which.Message.Must().Contain(nameof(IBillingAddressTevent<>));

   [XF] public void loading_the_history_of_an_order_whose_shipping_street_changed_throws_naming_the_shipping_slot() =>
      Invoking(() => OrderWithItsShippingAddressInTheBillingSlot.LoadFromHistory(HistoryOfAnOrderWhoseShippingStreetChanged()))
        .Must().Throw<StateAssertionFailedException>()
        .Which.Message.Must().Contain(nameof(IShippingAddressTevent<>));

   static IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> HistoryOfAnOrderWhoseShippingStreetChanged()
   {
      var order = OrderTaggregate.Create();
      order.ShippingAddress.ChangeStreet("1 Harbour Road");
      IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> history = [];
      ((ITaggregate)order).Commit(committed => history = [..committed]);
      return history;
   }
}
