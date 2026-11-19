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
   internal SharedTomponentSlotInAnOwner(object owner, ISharedTomponentSlot<TTomponentTevent> slot)
   {
      Owner = owner;
      Slot = slot;
   }

   internal object Owner { get; }
   internal ISharedTomponentSlot<TTomponentTevent> Slot { get; }
}

///<summary>A slot for a shared tomponent in the class <typeparamref name="TOwner"/>: declared once, in a static field of that class, and shared by every instance of it.<br/>
/// It holds what is the same for every owner of the class: the adopting wrapper tevent that tevents of the shared tomponent in this slot are published in,<br/>
/// and how to get that shared tomponent from an owner.</summary>
///<remarks>With the slot in a static field, a shared tomponent keeps its owner and its slot, and its owner keeps nothing for the slot.<br/>
/// See dev_docs/taggregate-improvements.</remarks>
///<remarks>The class's <see cref="TeventRouting"/> enters the slot with <see cref="TeventRouting{TInstance,TTevent}.Declaration.SharedTomponent{TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>.<br/>
/// Declare the slots in a nested static class, such as <c>Slots</c>: the runtime initializes a class's static fields before the first<br/>
/// read of any of them, so the routing may be declared above or below the slots. Declared beside the routing in the owner's class<br/>
/// itself, a slot must be above it, since static fields are initialized in the order written.</remarks>
public sealed class SharedTomponentSlot<TOwner, TOwnerTevent, TOwnerTeventImplementation, TTomponentTevent, TAdoptingWrapperTevent> : ISharedTomponentSlot<TTomponentTevent>
   where TOwner : class
   where TOwnerTevent : class, ITaggregateTevent
   where TOwnerTeventImplementation : TaggregateTevent, TOwnerTevent
   where TTomponentTevent : class, ITevent
   where TAdoptingWrapperTevent : class, TOwnerTevent, IPublisherTevent<TTomponentTevent>
{
   readonly Type _adoptingWrapperTeventImplementation;
   readonly Func<TOwner, SharedTomponent<TTomponentTevent>> _tomponentOf;

   ///<param name="adoptingWrapperTeventImplementation">The implementation class of <typeparamref name="TAdoptingWrapperTevent"/>, such as <c>typeof(SeeAlsoTevent&lt;&gt;)</c>.<br/>
   /// Its generic type definition is closed over each tevent's own type, as a taggregate's wrapper tevent implementation is.</param>
   ///<param name="tomponentOf">Gets the shared tomponent in this slot from an owner. It must hold no state: every owner of the class shares it.</param>
   ///<exception cref="ArgumentException"><paramref name="adoptingWrapperTeventImplementation"/> doesn't implement <typeparamref name="TAdoptingWrapperTevent"/>, such as another slot's:<br/>
   /// this slot's tevents would then be routed to the other slot's tomponent.</exception>
   public SharedTomponentSlot(Type adoptingWrapperTeventImplementation, Func<TOwner, SharedTomponent<TTomponentTevent>> tomponentOf)
   {
      Argument.Assert(typeof(ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>).IsAssignableFrom(typeof(TOwner)),
                      () => $"A slot's owner publishes the tevents adopted through it, so {typeof(TOwner)} must be a taggregate or a component whose tevents are {typeof(TOwnerTevent)}.");
      Argument.Assert(ImplementsTheAdoptingWrapperTevent(adoptingWrapperTeventImplementation),
                      () => $"The adopting wrapper tevent implementation {adoptingWrapperTeventImplementation} handed to a slot for {typeof(TAdoptingWrapperTevent)} does not implement it. " +
                            $"Another slot's wrapper would route this slot's tevents to that slot's tomponent.");
      _adoptingWrapperTeventImplementation = adoptingWrapperTeventImplementation;
      _tomponentOf = tomponentOf;
   }

   ///<summary>This slot in <paramref name="owner"/>: what the shared tomponent in it is constructed with.</summary>
   public SharedTomponentSlotInAnOwner<TTomponentTevent> In(TOwner owner) => new(owner, this);

   ///<summary>The owner's tevent that adopts <paramref name="tevent"/>: this slot's adopting wrapper tevent, around it. What the shared tomponent's publishing<br/>
   /// publishes, and what an owner puts in a snapshot for the shared tomponent in this slot.</summary>
   TOwnerTeventImplementation Adopt(TTomponentTevent tevent) => (TOwnerTeventImplementation)PublisherTevent.WrapIn(_adoptingWrapperTeventImplementation, tevent);

   ///<summary>The shared tomponent in this slot of <paramref name="owner"/>, asserted to have been constructed with this slot: a tomponent constructed with<br/>
   /// another slot of the class and kept in this slot's member would publish through the other slot, and be routed what this one adopts.</summary>
   internal ISharedTomponentInternals<TTomponentTevent> TomponentOf(TOwner owner)
   {
      var tomponent = _tomponentOf(owner);
      State.Assert(tomponent.IsIn(this),
                   () => $"The shared tomponent the slot for {typeof(TAdoptingWrapperTevent)} gets from a {typeof(TOwner)} was not constructed with that slot. Construct it with this slot's In(owner).");
      return tomponent;
   }

   ///<summary>Whether <paramref name="implementation"/>, closed over <typeparamref name="TTomponentTevent"/> if it is a generic type definition, implements <typeparamref name="TAdoptingWrapperTevent"/>.</summary>
   static bool ImplementsTheAdoptingWrapperTevent(Type implementation) =>
      (implementation.IsGenericTypeDefinition ? implementation.MakeGenericType(typeof(TTomponentTevent)) : implementation).IsAssignableTo(typeof(TAdoptingWrapperTevent));

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
   ///<summary>Publishes <paramref name="tevent"/> as the owner's tevent, in this slot's adopting wrapper tevent, once asserted to come from the shared tomponent<br/>
   /// the owner keeps in this slot's member: a tomponent constructed with this slot and kept in another slot's member would have its tevents applied<br/>
   /// to this slot's tomponent.</summary>
   void ISharedTomponentSlot<TTomponentTevent>.PublishInternal(object owner, object publisher, TTomponentTevent tevent)
   {
      State.Assert(ReferenceEquals(_tomponentOf((TOwner)owner), publisher),
                   () => $"A shared tomponent constructed with the slot for {typeof(TAdoptingWrapperTevent)} publishes, but its {typeof(TOwner)} keeps another tomponent in that slot's member. Construct each shared tomponent with the slot of the member it is kept in.");
      ((ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>)owner).PublishInternal(Adopt(tevent));
   }
#pragma warning restore CS0618
}
