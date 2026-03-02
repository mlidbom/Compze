using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
    [UsedImplicitly] public class Entity :
#pragma warning restore CA1812
        Component.RemovableEntity<
            Entity,
            Guid,
            ICompositeTaggregateTevent.Component.Entity,
            CompositeTaggregateTevent.Component.Entity,
            ICompositeTaggregateTevent.Component.Entity.Created,
            ICompositeTaggregateTevent.Component.Entity.Removed,
            CompositeTaggregateTevent.Component.Entity.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public Entity(Component parent) : base(parent)
        {
            RegisterTeventAppliers()
               .For<ICompositeTaggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.Entity.Renamed(name));
        public void Remove() => Publish(new CompositeTaggregateTevent.Component.Entity.Removed());
    }
}
