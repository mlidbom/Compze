using Compze.Abstractions;
using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses.Shared._private;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>A slot of a class, and one owner of that class: what the constructor of the shared tomponent in that slot of that owner takes.<br/>
/// A ref struct, so making one and handing it over allocates nothing, and it can't be kept.</summary>
public readonly ref struct SharedTomponentSlotInAnOwner<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   internal SharedTomponentSlotInAnOwner(object owner, ISharedTomponentSlotOfAClass<TTomponentTevent> slot)
   {
      Owner = owner;
      Slot = slot;
   }

   internal object Owner { get; }
   internal ISharedTomponentSlotOfAClass<TTomponentTevent> Slot { get; }
}

///<summary>A slot for a shared tomponent in the class <typeparamref name="TOwner"/>: declared once, in a static field of that class, and shared by every instance of it.<br/>
/// It holds what is the same for every owner of the class: the adopting wrapper tevent that tevents of the shared tomponent in this slot are published in,<br/>
/// and how to get that shared tomponent from an owner.</summary>
///<remarks>Beside <see cref="SharedTomponentSlot{TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>, which is an object<br/>
/// for each owner. With the slot in a static field, a shared tomponent keeps its owner and its slot, and its owner keeps nothing for the slot.<br/>
/// Built on the spike provisionally, from taggregate-experiments-fable-static-slots: see dev_docs/taggregate-improvements.</remarks>
///<remarks>The class's <see cref="TeventRouting"/> enters the slot with <see cref="TeventRouting{TInstance}.Declaration.SharedTomponent{TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>.<br/>
/// Declare the slots in a nested static class, such as <c>Slots</c>: the runtime initializes a class's static fields before the first<br/>
/// read of any of them, so the routing may be declared above or below the slots. Declared beside the routing in the owner's class<br/>
/// itself, a slot must be above it, since static fields are initialized in the order written.</remarks>
public sealed class SharedTomponentSlotOfAClass<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> : ISharedTomponentSlotOfAClass<TTomponentTevent>
   where TOwner : class
   where TOwnerTevent : class, ITaggregateTevent
   where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent
   where TTomponentTevent : class, ITevent
   where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>
{
   readonly Type _adoptingWrapperTeventImplementation;
   readonly Func<TOwner, ISharedTomponentInternals<TTomponentTevent>> _tomponentOf;

   ///<param name="adoptingWrapperTeventImplementation">The implementation class of <typeparamref name="TAdoptingWrapperTevent"/>. Its generic type definition is<br/>
   /// closed over each tevent's own type, as a taggregate's wrapper tevent implementation is.</param>
   ///<param name="tomponentOf">Gets the shared tomponent in this slot from an owner. It must hold no state: every owner of the class shares it.</param>
   public SharedTomponentSlotOfAClass(Type adoptingWrapperTeventImplementation, Func<TOwner, ISharedTomponentInternals<TTomponentTevent>> tomponentOf)
   {
      Argument.Assert(typeof(ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>).IsAssignableFrom(typeof(TOwner)),
                      () => $"A slot's owner publishes the tevents adopted through it, so {typeof(TOwner)} must be a taggregate or a component whose tevents are {typeof(TOwnerTevent)}.");
      _adoptingWrapperTeventImplementation = adoptingWrapperTeventImplementation;
      _tomponentOf = tomponentOf;
   }

   ///<summary>This slot in <paramref name="owner"/>: what the shared tomponent in it is constructed with.</summary>
   public SharedTomponentSlotInAnOwner<TTomponentTevent> In(TOwner owner) => new(owner, this);

   ///<summary>The owner's tevent that adopts <paramref name="tevent"/>: this slot's adopting wrapper tevent, around it. What the shared tomponent's publishing<br/>
   /// publishes, and what an owner puts in a snapshot for the shared tomponent in this slot.</summary>
   TOwnerTeventImplementation Adopt(TTomponentTevent tevent) => (TOwnerTeventImplementation)PublisherTevent.WrapIn(_adoptingWrapperTeventImplementation, tevent);

   internal ISharedTomponentInternals<TTomponentTevent> TomponentOf(TOwner owner) => _tomponentOf(owner);

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
   void ISharedTomponentSlotOfAClass<TTomponentTevent>.PublishInternal(object owner, TTomponentTevent tevent) =>
      ((ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>)owner).PublishInternal(Adopt(tevent));
#pragma warning restore CS0618
}
