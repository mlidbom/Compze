using Compze.Contracts;
using Compze.Tessaging;
using Compze.Tessaging.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>The tevent contract of a <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/>: every tevent carries the<br/>
/// <see cref="EntityId"/> of the tentity it belongs to, as ordinary domain data. That id is how a<br/>
/// <see cref="SharedTentityCollection{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/> routes each tevent to the right tentity instance -<br/>
/// it is entity identity ("which tentity"), a different thing entirely from a tessage's <see cref="TessageId"/> ("which publication"),<br/>
/// which a shared teventive's tevents do not carry at all.</summary>
public interface ISharedTentityTevent<out TTentityId> : ITevent
   where TTentityId : struct
{
   TTentityId EntityId { get; }
}

///<summary>Base class for shared tentities: tevent-sourced entities-with-identity that, like a <see cref="SharedTomponent{TTomponentTevent}"/>, are NOT<br/>
/// tied to one specific taggregate. Shared tentities live in a <see cref="SharedTentityCollection{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/>,<br/>
/// which occupies one slot of its owner's class: the whole collection publishes through one adopting wrapper tevent type, and individual tentities<br/>
/// are told apart by the <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> their tevents carry.</summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentityId : struct
   where TTentityTevent : class, ISharedTentityTevent<TTentityId>
   where TTentityCreatedTevent : class, TTentityTevent
{
   readonly ISharedTomponentInternals<TTentityTevent> _collection;

   TTentityId _id;
   public TTentityId Id => _id._assert().NotDefault();

   protected SharedTentity(SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> collection)
   {
      // ReSharper disable once VirtualMemberCallInConstructor The routing is a table in a static field, and the assertion that the class declares its own must run at construction.
      TeventRouting.AssertDeclaresItsTeventAppliers(this, TeventAppliers);
      _collection = collection;
   }

   ///<summary>The routing of this tentity's class to its tevent appliers: the table the class declares with <see cref="DeclareAppliers"/>, or a<br/>
   /// subclass with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>, keeps in a static field, and returns from its override:<br/>
   /// <c>protected override TeventRouting TeventAppliers => Appliers;</c><br/>
   /// Every class overrides it with its own: one that inherits its base class's is refused when it is constructed. A shared tentity has no<br/>
   /// tevent handlers.</summary>
   protected abstract TeventRouting TeventAppliers { get; }

   ///<summary>Declares the tevent appliers of <typeparamref name="TTentity"/>, for it to keep in a static field and return from its override of<br/>
   /// <see cref="TeventAppliers"/>. Every tevent the tentity publishes must reach at least one of them.<br/>
   /// The routing starts with the tentity's own applier of its created tevent, which gives it its <see cref="Id"/>.</summary>
   protected static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<TeventRouting<TTentity, TTentityTevent>.Declaration> declare) =>
      TeventRouting<TTentity, TTentityTevent>.DeclareAppliers(it =>
      {
         it.For<TTentityCreatedTevent>(static (tentity, created) => tentity._id = created.EntityId);
         declare(it);
      });

   ///<summary>Publishes <paramref name="tevent"/> through the collection's slot. The tevent must carry this tentity's <see cref="Id"/> -<br/>
   /// a shared tentity's tevents state their <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> explicitly at creation; nothing stamps it in later.</summary>
   protected void Publish(TTentityTevent tevent)
   {
      State.Assert(Equals(tevent.EntityId, Id), () => $"Attempted to publish a tevent with {nameof(ISharedTentityTevent<>.EntityId)}: {tevent.EntityId} from within the tentity with {nameof(Id)}: {Id}");
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      _collection.PublishInternal(tevent);
#pragma warning restore CS0618
   }

   ///<summary>Gives this new tentity, being restored with its owner from a snapshot, its id, as its created tevent would.</summary>
   internal void RestoreId(TTentityId id) => _id = id;

   ///<summary>Applies a tevent the collection routes to this tentity, still in its slot's adopting wrapper tevent.</summary>
   internal void ApplyAdopted(IPublisherTevent<TTentityTevent> adopted) => TeventAppliers.Dispatch(this, adopted);
}
