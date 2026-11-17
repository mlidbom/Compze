using Compze.Tessaging.TessageTypes;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
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

   protected Tentity(TParent taggregate, TeventDispatcherConfig? teventAppliersDispatcherConfig = null) : base(taggregate, false, teventAppliersDispatcherConfig)
   {
      if(TeventAppliers is null)
         RegisterTeventAppliers()
           .For<TTentityCreatedTevent>(e => _id = IdGetterSetter.GetId(e));
   }

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

   internal class CollectionManager : ICollectionManager
   {
      protected static readonly TTentityTeventIdGetterSetter IdGetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

      protected EntityCollection<TTentity, TTentityId> ManagedEntities { get; }

      TParent _parent;

      public CollectionManager(TParent parent)
      {
         ManagedEntities = [];
         _parent = parent;
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
         if(parent.DeclaresTeventAppliersInternal) return;

         parent.RegisterTeventAppliersInternal()
               .For<TTentityCreatedTevent>(e =>
                {
                   var entity = Constructor.For<TTentity>.WithArguments<TParent>.Instance(parent);
                   ManagedEntities.Add(entity, IdGetter.GetId(e));
                })
               .For<TTentityTevent>(e => GetEntityAsTeventiveInternals(e).ApplyTeventInternal(e));
#pragma warning restore CS0618
      }

      ITeventiveInternals<TTentityTevent, TTentityTeventImplementation> GetEntityAsTeventiveInternals(TTentityTevent e) => ManagedEntities[IdGetter.GetId(e)];

      ///<summary>What the routing of a parent that declares it delivers: a created tevent adds the entity, every tevent reaches the entity whose id<br/>
      /// it carries, and then <see cref="AfterTheEntityApplied"/> runs. The entity applies its created tevent itself, which gives it its id.</summary>
      void ITeventRoutedMember<TTentityTevent>.DispatchInternal(IPublisherTevent<ITevent> wrapped)
      {
         var tevent = (TTentityTevent)wrapped.Tevent;
         var id = IdGetter.GetId(tevent);
         if(tevent is TTentityCreatedTevent) ManagedEntities.Add(Constructor.For<TTentity>.WithArguments<TParent>.Instance(_parent), id);

#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
         ((ITeventRoutedMember<TTentityTevent>)ManagedEntities[id]).DispatchInternal(wrapped);
#pragma warning restore CS0618
         AfterTheEntityApplied(tevent, id);
      }

      ///<summary>Called after the entity whose id <paramref name="tevent"/> carries has applied it: a removable entity's collection removes the entity here.</summary>
      private protected virtual void AfterTheEntityApplied(TTentityTevent tevent, TTentityId id) {}

      public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => ManagedEntities;

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
