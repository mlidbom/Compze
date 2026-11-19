using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
[UsedImplicitly] partial class Entity : RootQueryModel.Entity<Entity, Guid, ICompositeTaggregateTevent.Entity, ICompositeTaggregateTevent.Entity.Created, ICompositeTaggregateTevent.Entity.Removed, CompositeTaggregateTevent.Entity.IdGetterSetter>
#pragma warning restore CA1812
{
   static readonly TeventRouting<Entity, ICompositeTaggregateTevent.Entity> Appliers = DeclareAppliers(it => it
     .Entities(static entity => entity._entities)
     .For<ICompositeTaggregateTevent.Entity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

   protected override TeventRouting TeventAppliers => Appliers;

   public string Name { get; private set; } = string.Empty;

   public Entity() => _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);

   public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
   readonly RemovableNestedEntity.ICollectionManager _entities;
}
