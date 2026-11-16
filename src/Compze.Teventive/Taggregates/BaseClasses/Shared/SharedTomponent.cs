using Compze.Contracts;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>Base class for shared tomponents: teventive components that are NOT tied to one specific taggregate but can be a member of any<br/>
/// taggregate (or teventive component). A shared tomponent's tevents are bare <see cref="ITevent"/>s rooted in the tomponent's own tevent<br/>
/// interface hierarchy - they carry no taggregate identity of their own. The owner adopts each published tevent into its own tevent hierarchy<br/>
/// by wrapping it in an owner-declared adopting wrapper tevent; that wrapping, and routing adopted tevents back here for applying, is the job<br/>
/// of the <see cref="ISharedTomponentSlot{TTomponentTevent}"/> the tomponent is handed at construction.</summary>
///<remarks>Contrast with <see cref="TeventiveComponent{TParent,TParentTevent,TParentTeventImplementation,TComponent,TComponentTevent,TComponentTeventImplementation}"/>,<br/>
/// whose tevents slot into exactly one taggregate's tevent hierarchy: an owned component's tevent types already identify their publisher completely,<br/>
/// so they need no wrapping. A shared tomponent's tevent types identify no owner at all - the adopting wrapper is what says<br/>
/// "this happened in THIS member of THIS taggregate".</remarks>
public abstract class SharedTomponent<TTomponentTevent> : ISharedTomponentInternals<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   //wip: taggregate-experiments-opus keeps the per-instance dispatcher for the classes that declare no TeventRouting. Null for those that do.
   readonly IMutableTeventDispatcher<TTomponentTevent>? _teventAppliersDispatcher;
   readonly ISharedTomponentSlot<TTomponentTevent>? _slot;

   //wip: taggregate-experiments-fable-static-slots: a tomponent whose slot belongs to its owner's class keeps its owner and that slot, and has no slot object of its own.
   //   The final design has only these two fields: _slot and the dispatcher above are the per-instance style's.
   readonly object? _owner;
   readonly ISharedTomponentSlotOfAClass<TTomponentTevent>? _slotOfItsOwnersClass;

   ///<summary>For a shared tomponent in a slot that belongs to its owner's class: <paramref name="slot"/> is what <c>TheSlot.In(owner)</c> returns.<br/>
   /// Its class declares its tevent appliers in a <see cref="TeventRouting"/>.</summary>
   protected SharedTomponent(SharedTomponentSlotInAnOwner<TTomponentTevent> slot)
   {
      State.Assert(TeventAppliers is not null, () => $"{GetType()} is in a slot that belongs to its owner's class, so it declares its tevent appliers in a {nameof(TeventRouting)}.");
      _owner = slot.Owner;
      _slotOfItsOwnersClass = slot.Slot;
   }

   protected SharedTomponent(ISharedTomponentSlot<TTomponentTevent> slot, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)
   {
      if(TeventAppliers is null) _teventAppliersDispatcher = IMutableTeventDispatcher<TTomponentTevent>.New(teventAppliersDispatcherConfig);
      _slot = slot;
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      slot.AttachTomponentInternal(this);
#pragma warning restore CS0618
   }

   ///<summary>The routing of this tomponent's class to its tevent appliers, when the class declares one: a <see cref="TeventRouting"/> in a static field.<br/>
   /// Null for a class that registers its appliers per instance, through <see cref="RegisterTeventAppliers"/>.</summary>
   protected virtual TeventRouting? TeventAppliers => null;

   protected ITeventSubscriber<TTomponentTevent> RegisterTeventAppliers()
   {
      State.Assert(TeventAppliers is null, () => $"{GetType()} declares its tevent appliers in a {nameof(TeventRouting)}, so it cannot also register appliers per instance.");
      return _teventAppliersDispatcher._assert().NotNull().Register();
   }

   ///<summary>Publishes <paramref name="tevent"/> through the tomponent's <see cref="ISharedTomponentSlot{TTomponentTevent}"/>: the slot wraps it in the<br/>
   /// owner's adopting wrapper tevent and publishes it as an owner tevent, and the owner's publishing routes it back through the slot to this<br/>
   /// tomponent's tevent appliers. State changes only in the appliers, exactly as for a taggregate.</summary>
   protected void Publish(TTomponentTevent tevent)
   {
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      if(_slotOfItsOwnersClass is {} slotOfItsOwnersClass) slotOfItsOwnersClass.PublishInternal(_owner._assert().NotNull(), tevent);
      else _slot._assert().NotNull().PublishInternal(tevent);
#pragma warning restore CS0618
   }

#pragma warning disable CA1033 //These methods should NOT clutter the public interface of shared tomponents.
   void ISharedTomponentInternals<TTomponentTevent>.ApplyTeventInternal(TTomponentTevent tevent) => _teventAppliersDispatcher._assert().NotNull().Dispatch(tevent);

   void ISharedTomponentInternals<TTomponentTevent>.ApplyAdoptedTeventInternal(IPublisherTevent<TTomponentTevent> adopted)
   {
      if(TeventAppliers is {} appliers) appliers.Dispatch(this, adopted);
      else _teventAppliersDispatcher._assert().NotNull().Dispatch(adopted.Tevent);
   }
   void ISharedTomponentInternals<TTomponentTevent>.PublishInternal(TTomponentTevent tevent) => Publish(tevent);
#pragma warning restore CA1033
}
