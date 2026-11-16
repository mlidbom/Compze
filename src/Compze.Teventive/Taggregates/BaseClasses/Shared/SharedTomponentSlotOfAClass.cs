using Compze.Abstractions;
using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>What a <see cref="SharedTomponent{TTomponentTevent}"/> publishes through when its slot belongs to its owner's class instead of to the one owner:<br/>
/// the slot is told which owner the tomponent is in, since it is the same slot for every owner of the class.</summary>
public interface ISharedTomponentSlotOfAClass<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(object owner, TTomponentTevent tevent);
}

///<summary>A slot of a class, and one owner of that class: what the constructor of the shared tomponent in that slot of that owner takes.<br/>
/// A struct, so making one and handing it over allocates nothing.</summary>
public readonly struct SharedTomponentSlotInAnOwner<TTomponentTevent>
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
///<remarks>An experiment (taggregate-experiments-fable-static-slots), beside <see cref="SharedTomponentSlot{TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>,<br/>
/// which is an object for each owner. With the slot in a static field, a shared tomponent keeps its owner and its slot, and its owner keeps nothing for the slot.<br/>
/// See dev_docs/taggregate-improvements on the taggregate-improvements branch.</remarks>
///<remarks>The class's <see cref="TeventRouting"/> enters the slot with <see cref="TeventRouting{TInstance}.Declaration.SharedTomponent{TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>,<br/>
/// so the slot is declared above the routing: static fields are initialized in the order written.</remarks>
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
   public TOwnerTeventImplementation Adopt(TTomponentTevent tevent) => (TOwnerTeventImplementation)PublisherTevent.WrapIn(_adoptingWrapperTeventImplementation, tevent);

   internal ISharedTomponentInternals<TTomponentTevent> TomponentOf(TOwner owner) => _tomponentOf(owner);

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
   void ISharedTomponentSlotOfAClass<TTomponentTevent>.PublishInternal(object owner, TTomponentTevent tevent) =>
      ((ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>)owner).PublishInternal(Adopt(tevent));
#pragma warning restore CS0618
}
