using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
    [UsedImplicitly] public class Entity :
#pragma warning restore CA1812
        Component.RemovableEntity<
            Entity,
            Guid,
            CompositeAggregateEvent.Component.Entity.IRoot,
            CompositeAggregateEvent.Component.Entity.Implementation.Root,
            CompositeAggregateEvent.Component.Entity.Created,
            CompositeAggregateEvent.Component.Entity.Removed,
            CompositeAggregateEvent.Component.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public Entity(Component parent) : base(parent)
        {
            RegisterEventAppliers()
               .For<CompositeAggregateEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeAggregateEvent.Component.Entity.Implementation.Renamed(name));
        public void Remove() => Publish(new CompositeAggregateEvent.Component.Entity.Implementation.Removed());
    }
}
