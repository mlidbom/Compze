using Compze.Abstractions;
using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>A shared tomponent's connection to its owner - the only thing a <see cref="SharedTomponent{TTomponentTevent}"/> knows about the<br/>
/// taggregate it is a member of. The owner creates one slot per shared-tomponent member and hands it to the tomponent at construction.</summary>
public interface ISharedTomponentSlot<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTomponentTevent tevent);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void AttachTomponentInternal(ISharedTomponentInternals<TTomponentTevent> tomponent);
}

///<summary>The owner-side wiring of one shared-tomponent member: wraps every tevent the tomponent publishes in the slot's adopting wrapper tevent<br/>
/// and publishes the wrapper as an owner tevent, and routes adopted tevents back to the tomponent's appliers - both when the owner publishes live<br/>
/// and when it replays history.</summary>
///<remarks>The adopting wrapper tevent is what makes a shared tomponent's tevents part of the owner's tevent hierarchy: an owner-declared wrapper<br/>
/// type that is BOTH an owner tevent and an <see cref="IPublisherTevent{TTevent}"/> of the tomponent's tevents, e.g.<br/>
/// <c>interface IShippingAddressTevent&lt;out T&gt; : IOrderTevent, IPublisherTevent&lt;T&gt; where T : IPostalAddressTevent</c>.<br/>
/// Each member gets its own adopting wrapper type - that type is what distinguishes two same-typed tomponents (a shipping and a billing address)<br/>
/// for routing back and for subscribers, and it is why <typeparamref name="TAdoptingWrapperTevent"/> exists per slot.</remarks>
///<remarks>Route-back subscribes to <see cref="IPublisherTevent{TTevent}"/> of <typeparamref name="TAdoptingWrapperTevent"/>: by the time an<br/>
/// adopted tevent reaches the owner's appliers it is wrapped once more, in the owner's own publisher-identifying wrapper, and routing matches the<br/>
/// outermost type only. The slot unwraps both layers by hand - the routing model auto-unwraps exactly one.</remarks>
public sealed class SharedTomponentSlot<TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> : ISharedTomponentSlot<TTomponentTevent>
   where TOwnerTevent : class, ITaggregateTevent
   where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent
   where TTomponentTevent : class, ITevent
   where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>
{
   readonly ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation> _owner;
   readonly Type _adoptingWrapperTeventImplementation;
   ISharedTomponentInternals<TTomponentTevent>? _tomponent;

   ///<param name="owner">The taggregate or tomponent this slot is a member of.</param>
   ///<param name="adoptingWrapperTeventImplementation">The implementation class of <typeparamref name="TAdoptingWrapperTevent"/>. Its generic type<br/>
   /// definition is closed over each published tevent's runtime type, exactly like a taggregate's declared wrapper implementation.</param>
   public SharedTomponentSlot(ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation> owner, Type adoptingWrapperTeventImplementation)
   {
      _owner = owner;
      _adoptingWrapperTeventImplementation = adoptingWrapperTeventImplementation;
#pragma warning disable CS0618 // This is just the type of infrastructure code the members are for
      owner.RegisterTeventAppliersInternal()
           .ForWrapped<IPublisherTevent<TAdoptingWrapperTevent>>(
               ownerWrappedTevent => _tomponent._assert().NotNull().ApplyTeventInternal(ownerWrappedTevent.Tevent.Tevent));
#pragma warning restore CS0618
   }

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
   void ISharedTomponentSlot<TTomponentTevent>.PublishInternal(TTomponentTevent tevent) =>
      _owner.PublishInternal((TOwnerTeventImplementation)PublisherTevent.WrapIn(_adoptingWrapperTeventImplementation, tevent));
#pragma warning restore CS0618

   void ISharedTomponentSlot<TTomponentTevent>.AttachTomponentInternal(ISharedTomponentInternals<TTomponentTevent> tomponent)
   {
      State.Assert(_tomponent is null, () => "A slot connects its owner to ONE shared tomponent; this slot already has one attached.");
      _tomponent = tomponent;
   }
}
