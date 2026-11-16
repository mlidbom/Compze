# Checks against Compze.Teventive 0.6.0-alpha

Run on 2026-11-16 by jastudio-rep-opus. Each file is a program of its own.

## `TypedParentAndReentrancy.cs`

It checks two things:

- **A component typed on the subclass that declares it:** a `FormsComponent` of `VocabNote`, which derives from
  `JPNote`. It derives from the top-level `TeventiveComponent<VocabNote, …>` instead of the nested `JPNote.Component<…>`,
  and its applier reads a member only `VocabNote` has, through a typed field. See "Components typed on the class that
  declares them" in [the per-class routing](../2-tevent-routing-decided-once-per-class.md).
- **The reentrancy bug in `Taggregate.Publish`:** a `TeventStream` observer that publishes on the same taggregate from
  inside `OnNext`. See [told once per publish cascade](../3-told-once-per-publish-cascade.md).

Its output:

```
Typed-parent component: Forms.Value = '食べる / question of taberu'
Reentrant publish from an observer threw InvalidOperationException: Collection was modified; enumeration operation may not execute.
Tevents the observer saw, in order: Renamed, Renamed, Updated
```

## `SharedTomponentSubscriptions.cs`

It checks how a subscriber across every owner can get a shared tomponent's tevents: an order with a shipping and a
billing address slot, and an unrelated warehouse with one address slot, all dispatched into one
`IMutableTeventDispatcher<ITevent>`. See [later](../4-later.md). Its output:

```
ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent>>>: ShippingAddressTevent: Shipping street | BillingAddressTevent: Billing street | WarehouseAddressTevent: Warehouse street
ForWrappedGeneric<IPublisherTevent<IPublisherTevent<IPostalAddressTevent.Changed>>>: Shipping street | Billing street | Warehouse street
ForGenericTevent<IAddressAdopted>, a marker on each owner's adopting wrappers: ShippingAddressTevent: Shipping street | BillingAddressTevent: Billing street | WarehouseAddressTevent: Warehouse street
```

## Running them

To run one, copy it as `Program.cs` to a folder outside this repository, with the project file shown in
[the benchmark's README](../benchmark/README.md), and `dotnet run -c Release`. Once the per-cascade hook exists, a
specification in Compze replaces the reentrancy check.
