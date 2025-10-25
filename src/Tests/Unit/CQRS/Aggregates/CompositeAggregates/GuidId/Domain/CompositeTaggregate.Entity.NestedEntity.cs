using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

[UsedImplicitly] partial class RemovableEntity
{
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
    public class RemovableNestedEntity :
#pragma warning restore CA1812
        RemovableEntity.RemovableEntity<
            RemovableNestedEntity,
            Guid,
            CompositeTaggregateTevent.Entity.NestedEntity.IRoot,
            CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Root,
            CompositeTaggregateTevent.Entity.NestedEntity.Created,
            CompositeTaggregateTevent.Entity.NestedEntity.Removed,
            CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
    {
        public string Name { get; private set; } = string.Empty;

        public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
        {
            RegisterTeventAppliers()
               .For<CompositeTaggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Renamed(name: name));
        public void Remove() => Publish(new CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Removed());
    }
}
