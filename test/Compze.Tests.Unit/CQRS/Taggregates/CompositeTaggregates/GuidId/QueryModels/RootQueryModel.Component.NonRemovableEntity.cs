using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using JetBrains.Annotations;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class NonRemovableEntity : RootQueryModel.Entity<NonRemovableEntity, Guid, ICompositeTaggregateTevent.Component.NonRemovableEntity, ICompositeTaggregateTevent.Component.NonRemovableEntity.Created, CompositeTaggregateTevent.Component.NonRemovableEntity.IdGetterSetter>
#pragma warning restore CA1812
   {
      static readonly TeventRouting<NonRemovableEntity, ICompositeTaggregateTevent.Component.NonRemovableEntity> NonRemovableEntityAppliers = DeclareAppliers(it => it
        .For<ICompositeTaggregateTevent.Component.NonRemovableEntity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

      protected override TeventRouting TeventAppliers => NonRemovableEntityAppliers;

      public string Name { get; private set; } = string.Empty;
   }
}
