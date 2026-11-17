using Compze.Tessaging.TessageTypes;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
using Compze.Teventive.Taggregates.BaseClasses._internal;
using Compze.Teventive.Taggregates.BaseClasses._private;

namespace Compze.Teventive.Taggregates.BaseClasses;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class Tentity<TParent,
                              TParentTevent,
                              TParentTeventImplementation,
                              TTentity,
                              TTentityId,
                              TTentityTeventImplementation,
                              TTentityTevent,
                              TTentityCreatedTevent,
                              TTentityTeventIdGetterSetter>
   : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityTevent, TTentityTeventImplementation>
   where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
   where TParentTevent : class, ITaggregateTevent
   where TTentityId : struct
   where TTentityTevent : class, TParentTevent
   where TParentTeventImplementation : TaggregateTevent, TParentTevent
   where TTentityTeventImplementation : TParentTeventImplementation, TTentityTevent
   where TTentityCreatedTevent : TTentityTevent
   where TTentity : Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>
   where TTentityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TTentityId, TTentityTeventImplementation, TTentityTevent>
{
   static Tentity() => TaggregateTypeValidator<TTentity, TTentityTeventImplementation, TTentityTevent>.AssertStaticStructureIsValid();

   static readonly TTentityTeventIdGetterSetter IdGetterSetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

   TTentityId _id;
   public TTentityId Id => _id._assert().NotDefault();

   protected Tentity(TParent taggregate) : base(taggregate) {}

   ///<summary>Declares the tevent appliers of <typeparamref name="TTentity"/>, for it to keep in a static field and return from its explicit<br/>
   /// implementation of <see cref="ITeventRouted.TeventAppliers"/>. Every tevent dispatched to the entity must reach at least one of them.<br/>
   /// The routing starts with the entity's own applier of its created tevent, which gives it its <see cref="Id"/>.</summary>
   protected new static TeventRouting<TTentity, TTentityTevent> DeclareAppliers(Action<TeventRouting<TTentity, TTentityTevent>.Declaration> declare) => DeclareTentityAppliers(declare);

   ///<summary>What <see cref="DeclareAppliers"/> declares, for the subclasses of <see cref="Tentity{TParent,TParentTevent,TParentTeventImplementation,TTentity,TTentityId,TTentityTeventImplementation,TTentityTevent,TTentityCreatedTevent,TTentityTeventIdGetterSetter}"/><br/>
   /// whose own <c>DeclareAppliers</c> adds routes of theirs after the entity's.</summary>
   private protected static TeventRouting<TTentity, TTentityTevent> DeclareTentityAppliers(Action<TeventRouting<TTentity, TTentityTevent>.Declaration> declare) =>
      TeventRouting<TTentity, TTentityTevent>.DeclareAppliers(it =>
      {
         it.For<TTentityCreatedTevent>(static (entity, created) => entity._id = IdGetterSetter.GetId(created));
         declare(it);
      });

   protected override void Publish(TTentityTeventImplementation tevent)
   {
      var id = IdGetterSetter.GetId(tevent);
      if(Equals(id, default(TTentityId)))
      {
         IdGetterSetter.SetEntityId(tevent, Id);
      } else if(!Equals(id, Id))
      {
         throw new Exception($"Attempted to raise tevent with EntityId: {id} from within entity with EntityId: {Id}");
      }

      base.Publish(tevent);
   }

   public interface ICollectionManager : IEntityCollectionManager<TTentity, TTentityId, TTentityTevent, TTentityTeventImplementation, TTentityCreatedTevent>, ITeventRoutedMember<TTentityTevent>;

   public static ICollectionManager CreateSelfManagingCollection(TParent parent) => new CollectionManager(parent);

   internal class CollectionManager : RoutedEntityCollection<TTentity, TTentityId, TTentityTevent>, ICollectionManager
   {
      static readonly TTentityTeventIdGetterSetter IdGetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

      readonly TParent _parent;

      public CollectionManager(TParent parent)
      {
         _parent = parent;
         TeventRouting.AssertRoutesToAPart<TTentityTevent>(parent, this);
      }

      protected override TTentityId IdOf(TTentityTevent tevent) => IdGetter.GetId(tevent);
      protected override bool Creates(TTentityTevent tevent) => tevent is TTentityCreatedTevent;

      ///<summary>A new entity of the parent. It applies its created tevent itself, which gives it its id.</summary>
      protected override TTentity CreateEntity() => Constructor.For<TTentity>.WithArguments<TParent>.Instance(_parent);

      public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => ManagedEntities;

      public TTentity AddRestored(TTentityId id)
      {
         State.Assert(((ITeventiveInATaggregate)_parent).ItsTaggregateIsBeingRestored,
                      () => $"A {typeof(TTentity).Name} is added without its created tevent only while its taggregate is being restored from a snapshot, by its RestoreFrom. Add it by publishing its created tevent, with {nameof(AddByPublishing)}.");
         var entity = CreateEntity();
         entity._id = id;
         ManagedEntities.Add(entity, id);
         return entity;
      }

      public TTentity AddByPublishing<TCreationTevent>(TCreationTevent creationTevent) where TCreationTevent : TTentityTeventImplementation, TTentityCreatedTevent
      {
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
         _parent.PublishInternal(creationTevent);
#pragma warning restore CS0618
         var result = ManagedEntities.InCreationOrder[^1];
         return result;
      }
   }
}
