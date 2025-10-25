using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;
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
            CompositeAggregateTevent.Component.Entity.IRoot,
            CompositeAggregateTevent.Component.Entity.Implementation.Root,
            CompositeAggregateTevent.Component.Entity.Created,
            CompositeAggregateTevent.Component.Entity.Removed,
            CompositeAggregateTevent.Component.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public Entity(Component parent) : base(parent)
        {
            RegisterTeventAppliers()
               .For<CompositeAggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeAggregateTevent.Component.Entity.Implementation.Renamed(name));
        public void Remove() => Publish(new CompositeAggregateTevent.Component.Entity.Implementation.Removed());
    }
}
