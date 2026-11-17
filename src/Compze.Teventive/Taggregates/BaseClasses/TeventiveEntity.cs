using Compze.Tessaging.TessageTypes;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
using Compze.Teventive.Taggregates.BaseClasses._internal;

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

   internal class CollectionManager : RoutedEntityCollection<TTentity, TTentityId, TTentityTevent>, ICollectionManager
   {
      protected static readonly TTentityTeventIdGetterSetter IdGetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

      readonly TParent _parent;

      public CollectionManager(TParent parent)
      {
         _parent = parent;
         TeventRouting.AssertRoutesToAPart<TTentityTevent>((ITeventRouted)parent, this);
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

      internal override TTentityId IdOf(TTentityTevent tevent) => IdGetter.GetId(tevent);
      internal override bool Creates(TTentityTevent tevent) => tevent is TTentityCreatedTevent;

      ///<summary>A new entity of the parent. It applies its created tevent itself, which gives it its id.</summary>
      internal override TTentity CreateEntity() => Constructor.For<TTentity>.WithArguments<TParent>.Instance(_parent);

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
