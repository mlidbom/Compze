using Compze.Abstractions.Tessaging.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

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
   readonly IMutableTeventDispatcher<TTomponentTevent> _teventAppliersDispatcher;
   readonly ISharedTomponentSlot<TTomponentTevent> _slot;

   protected SharedTomponent(ISharedTomponentSlot<TTomponentTevent> slot, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)
   {
      _teventAppliersDispatcher = IMutableTeventDispatcher<TTomponentTevent>.New(teventAppliersDispatcherConfig);
      _slot = slot;
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      slot.AttachTomponentInternal(this);
#pragma warning restore CS0618
   }

   protected ITeventSubscriber<TTomponentTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

   ///<summary>Publishes <paramref name="tevent"/> through the tomponent's <see cref="ISharedTomponentSlot{TTomponentTevent}"/>: the slot wraps it in the<br/>
   /// owner's adopting wrapper tevent and publishes it as an owner tevent, and the owner's publishing routes it back through the slot to this<br/>
   /// tomponent's tevent appliers. State changes only in the appliers, exactly as for a taggregate.</summary>
   protected void Publish(TTomponentTevent tevent)
   {
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      _slot.PublishInternal(tevent);
#pragma warning restore CS0618
   }

#pragma warning disable CA1033 //These methods should NOT clutter the public interface of shared tomponents.
   void ISharedTomponentInternals<TTomponentTevent>.ApplyTeventInternal(TTomponentTevent tevent) => _teventAppliersDispatcher.Dispatch(tevent);
   void ISharedTomponentInternals<TTomponentTevent>.PublishInternal(TTomponentTevent tevent) => Publish(tevent);
#pragma warning restore CA1033
}
