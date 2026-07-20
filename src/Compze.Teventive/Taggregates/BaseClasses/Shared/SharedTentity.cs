using Compze.Contracts;
using Compze.Tessaging;
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
/// which occupies one <see cref="ISharedTomponentSlot{TTomponentTevent}"/> on the owner: the whole collection publishes through one adopting wrapper<br/>
/// tevent type, and individual tentities are told apart by the <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> their tevents carry.</summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentityId : struct
   where TTentityTevent : class, ISharedTentityTevent<TTentityId>
   where TTentityCreatedTevent : class, TTentityTevent
{
   readonly IMutableTeventDispatcher<TTentityTevent> _teventAppliersDispatcher = IMutableTeventDispatcher<TTentityTevent>.New();
   readonly ISharedTomponentInternals<TTentityTevent> _collection;

   TTentityId _id;
   public TTentityId Id => _id._assert().NotDefault();

   protected SharedTentity(SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> collection)
   {
      _collection = collection;
      RegisterTeventAppliers().For<TTentityCreatedTevent>(tevent => _id = tevent.EntityId);
   }

   protected ITeventSubscriber<TTentityTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

   ///<summary>Publishes <paramref name="tevent"/> through the collection's slot. The tevent must carry this tentity's <see cref="Id"/> -<br/>
   /// a shared tentity's tevents state their <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> explicitly at creation; nothing stamps it in later.</summary>
   protected void Publish(TTentityTevent tevent)
   {
      State.Assert(Equals(tevent.EntityId, Id), () => $"Attempted to publish a tevent with {nameof(ISharedTentityTevent<>.EntityId)}: {tevent.EntityId} from within the tentity with {nameof(Id)}: {Id}");
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      _collection.PublishInternal(tevent);
#pragma warning restore CS0618
   }

   internal void ApplyTevent(TTentityTevent tevent) => _teventAppliersDispatcher.Dispatch(tevent);
}
