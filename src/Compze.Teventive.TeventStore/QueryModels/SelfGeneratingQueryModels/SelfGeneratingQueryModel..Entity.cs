using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses._internal;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>An entity of a query model, or of one of its components: one of a collection of parts with ids, which its parent keeps in the collection<br/>
   /// <see cref="CreateSelfManagingCollection"/> creates, and its parent's routing routes to with <c>Entities(...)</c>. A <typeparamref name="TEntityCreatedTevent"/><br/>
   /// adds an entity to the collection, and every tevent of <typeparamref name="TEntityTevent"/> reaches the entity whose id it carries.</summary>
   ///<remarks>The collection creates an entity with its parameterless constructor.</remarks>
   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
   public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter> : Component<TEntity, TEntityTevent>
      where TEntityId : struct
      where TEntityTevent : class, TTaggregateTevent
      where TEntityCreatedTevent : TEntityTevent
      where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityCreatedTevent, TTeventEntityIdGetter>
      where TTeventEntityIdGetter : IGetTaggregateEntityTeventEntityId<TEntityTevent, TEntityId>
   {
      static readonly TTeventEntityIdGetter IdGetter = Constructor.For<TTeventEntityIdGetter>.DefaultConstructor.Instance();

      TEntityId _id;
      public TEntityId Id => _id._assert().NotDefault();

      ///<summary>Declares the tevent appliers of <typeparamref name="TEntity"/>, for it to keep in a static field and return from its override of<br/>
      /// <see cref="TeventApplyingComponent{TComponent,TComponentTevent}.TeventAppliers"/>. Every tevent dispatched to the entity must reach at least one of them.<br/>
      /// The routing starts with the entity's own applier of its created tevent, which gives it its <see cref="Id"/>.</summary>
      protected new static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare) => DeclareEntityAppliers(declare);

      ///<summary>What <see cref="DeclareAppliers"/> declares, for the removable entity, whose own adds its routes after the entity's.</summary>
      private protected static TeventRouting<TEntity, TEntityTevent> DeclareEntityAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare) =>
         TeventRouting<TEntity, TEntityTevent>.DeclareAppliers(it =>
         {
            it.For<TEntityCreatedTevent>(static (entity, created) => entity._id = IdGetter.GetId(created));
            declare(it);
         });

      ///<summary>The collection that keeps a parent's entities, which the parent's routing routes to with <c>Entities(...)</c>.</summary>
      public interface ICollectionManager : ITeventRoutedPart<TEntityTevent>
      {
         ///<summary>The entities, by id and in the order they were created.</summary>
         IReadOnlyEntityCollection<TEntity, TEntityId> Entities { get; }
      }

      ///<summary>A collection of the entities of <paramref name="parent"/>, the query model or component whose routing routes their tevents to it.</summary>
      public static ICollectionManager CreateSelfManagingCollection(IParent parent) => new CollectionManager(parent);

      internal class CollectionManager : RoutedEntityCollection<TEntity, TEntityId, TEntityTevent>, ICollectionManager
      {
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
         internal CollectionManager(IParent parent) => parent.TeventAppliersInternal.AssertRoutesToAPart<TEntityTevent>(parent, this);
#pragma warning restore CS0618

         public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

         protected override TEntityId IdOf(TEntityTevent tevent) => IdGetter.GetId(tevent);
         protected override bool Creates(TEntityTevent tevent) => tevent is TEntityCreatedTevent;
         protected override TEntity CreateEntity() => Constructor.For<TEntity>.DefaultConstructor.Instance();
      }
   }
}
