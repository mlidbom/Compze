using Compze.Contracts;
using Compze.Teventive.Taggregates.BaseClasses._private;
using Compze.Teventive.Taggregates.BaseClasses.Shared._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>Base class for shared tomponents: teventive components that are NOT tied to one specific taggregate but can be a member of any<br/>
/// taggregate (or teventive component). A shared tomponent's tevents are bare <see cref="ITevent"/>s rooted in the tomponent's own tevent<br/>
/// interface hierarchy - they carry no taggregate identity of their own. The owner adopts each published tevent into its own tevent hierarchy<br/>
/// by wrapping it in an owner-declared adopting wrapper tevent; that wrapping, and routing adopted tevents back here for applying, is the job<br/>
/// of the <see cref="SharedTomponentSlotOfAClass{TOwner,TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/> the tomponent is in.</summary>
///<remarks>Contrast with <see cref="TeventiveComponent{TParent,TParentTevent,TParentTeventImplementation,TComponent,TComponentTevent,TComponentTeventImplementation}"/>,<br/>
/// whose tevents slot into exactly one taggregate's tevent hierarchy: an owned component's tevent types already identify their publisher completely,<br/>
/// so they need no wrapping. A shared tomponent's tevent types identify no owner at all - the adopting wrapper is what says<br/>
/// "this happened in THIS member of THIS taggregate".</remarks>
///<remarks>A shared tomponent keeps its owner and its slot, which belongs to the owner's class, and its owner keeps nothing for it.</remarks>
public abstract class SharedTomponent<TTomponentTevent> : ISharedTomponentInternals<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   readonly object _owner;
   readonly ISharedTomponentSlotOfAClass<TTomponentTevent> _slot;

   ///<summary>A shared tomponent in <paramref name="slot"/>, which is what <c>Slots.TheSlot.In(owner)</c> returns. Its class declares its tevent appliers<br/>
   /// with <see cref="DeclareAppliers{TTomponent}"/>.</summary>
   protected SharedTomponent(SharedTomponentSlotInAnOwner<TTomponentTevent> slot) : this(slot.Owner, slot.Slot)
   {
      // ReSharper disable once VirtualMemberCallInConstructor The routing is a table in a static field, and the assertion that the class declares its own must run at construction.
      TeventRouting.AssertDeclaresItsTeventAppliers(this, TeventAppliers);
   }

   ///<summary>For a shared tomponent that routes the tevents its slot delivers in code of its own, instead of through a <see cref="TeventRouting"/>.</summary>
   private protected SharedTomponent(object owner, ISharedTomponentSlotOfAClass<TTomponentTevent> slot)
   {
      _owner = owner;
      _slot = slot;
   }

   ///<summary>The routing of this tomponent's class to its tevent appliers: the table the class declares with <see cref="DeclareAppliers{TTomponent}"/>,<br/>
   /// or a subclass with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>, keeps in a static field, and returns from its override:<br/>
   /// <c>protected override TeventRouting TeventAppliers => Appliers;</c><br/>
   /// Every class overrides it with its own: one that returns none, or inherits its base class's, is refused when it is constructed. None only in<br/>
   /// Compze's own <see cref="SharedTentityCollection{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/>, which routes its tentities'<br/>
   /// tevents in code of its own. A shared tomponent has no tevent handlers.</summary>
   protected abstract TeventRouting? TeventAppliers { get; }

   ///<summary>Declares the tevent appliers of <typeparamref name="TTomponent"/>, the shared tomponent class calling it, for it to keep in a static field<br/>
   /// and return from its override of <see cref="TeventAppliers"/>. Every tevent the tomponent publishes must reach at least one of them.</summary>
   protected static TeventRouting<TTomponent, TTomponentTevent> DeclareAppliers<TTomponent>(Action<TeventRouting<TTomponent, TTomponentTevent>.Declaration> declare)
      where TTomponent : SharedTomponent<TTomponentTevent> =>
      TeventRouting<TTomponent, TTomponentTevent>.DeclareAppliers(declare);

   ///<summary>Whether the taggregate this tomponent is in is being restored from a snapshot, as its owner tells.</summary>
   private protected bool ItsTaggregateIsBeingRestored => ((ITeventiveInATaggregate)_owner).ItsTaggregateIsBeingRestored;

   ///<summary>Whether this tomponent was constructed with <paramref name="slot"/>: what a slot's route asserts of the tomponent it gets from an owner.</summary>
   internal bool IsIn(ISharedTomponentSlotOfAClass<TTomponentTevent> slot) => ReferenceEquals(_slot, slot);

   ///<summary>Publishes <paramref name="tevent"/> through the tomponent's slot: the slot wraps it in the owner's adopting wrapper tevent and publishes it<br/>
   /// as an owner tevent, and the owner's routing routes it back through the slot to this tomponent's tevent appliers. State changes only in the<br/>
   /// appliers, exactly as for a taggregate.</summary>
   protected void Publish(TTomponentTevent tevent) => _slot.PublishInternal(_owner, this, tevent);

   ///<summary>Applies a tevent its slot delivers, still in the slot's adopting wrapper tevent: through this class's <see cref="TeventRouting"/>.</summary>
   private protected virtual void ApplyAdopted(IPublisherTevent<TTomponentTevent> adopted) => TeventAppliers._assert().NotNull().Dispatch(this, adopted);

#pragma warning disable CA1033 //These methods should NOT clutter the public interface of shared tomponents.
   void ISharedTomponentInternals<TTomponentTevent>.ApplyAdoptedTeventInternal(IPublisherTevent<TTomponentTevent> adopted) => ApplyAdopted(adopted);
   void ISharedTomponentInternals<TTomponentTevent>.PublishInternal(TTomponentTevent tevent) => Publish(tevent);
#pragma warning restore CA1033
}
