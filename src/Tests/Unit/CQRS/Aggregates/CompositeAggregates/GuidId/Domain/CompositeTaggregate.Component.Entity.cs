using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
    [UsedImplicitly] public class Entity :
#pragma warning restore CA1812
        Component.RemovableEntity<
            Entity,
            Guid,
            CompositeTaggregateTevent.Component.Entity.IRoot,
            CompositeTaggregateTevent.Component.Entity.Implementation.Root,
            CompositeTaggregateTevent.Component.Entity.Created,
            CompositeTaggregateTevent.Component.Entity.Removed,
            CompositeTaggregateTevent.Component.Entity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public Entity(Component parent) : base(parent)
        {
            RegisterTeventAppliers()
               .For<CompositeTaggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.Entity.Implementation.Renamed(name));
        public void Remove() => Publish(new CompositeTaggregateTevent.Component.Entity.Implementation.Removed());
    }
}
