using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

[UsedImplicitly] partial class RemovableEntity
{
#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
    public class RemovableNestedEntity :
#pragma warning restore CA1812
        RemovableEntity.RemovableEntity<
            RemovableNestedEntity,
            Guid,
            CompositeAggregateTevent.Entity.NestedEntity.IRoot,
            CompositeAggregateTevent.Entity.NestedEntity.Implementation.Root,
            CompositeAggregateTevent.Entity.NestedEntity.Created,
            CompositeAggregateTevent.Entity.NestedEntity.Removed,
            CompositeAggregateTevent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
        {
            RegisterTeventAppliers()
               .For<CompositeAggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeAggregateTevent.Entity.NestedEntity.Implementation.Renamed(name: name));
        public void Remove() => Publish(new CompositeAggregateTevent.Entity.NestedEntity.Implementation.Removed());
    }
}
