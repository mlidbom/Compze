using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Entity
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class RemovableNestedEntity : RootQueryModel.Entity<RemovableNestedEntity, Guid, ICompositeTaggregateTevent.Entity.NestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity.Created, ICompositeTaggregateTevent.Entity.NestedEntity.Removed, CompositeTaggregateTevent.Entity.NestedEntity.IdGetterSetter>, ITeventRouted
#pragma warning restore CA1812
   {
      static readonly TeventRouting<RemovableNestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity> NestedEntityAppliers = DeclareAppliers(it => it
        .For<ICompositeTaggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

      TeventRouting ITeventRouted.TeventAppliers => NestedEntityAppliers;

      public string Name { get; private set; } = string.Empty;
   }
}
