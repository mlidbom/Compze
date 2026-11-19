using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using JetBrains.Annotations;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
    [UsedImplicitly] public class NonRemovableEntity :
#pragma warning restore CA1812
        Component.Entity<
            NonRemovableEntity,
            Guid,
            ICompositeTaggregateTevent.Component.NonRemovableEntity,
            CompositeTaggregateTevent.Component.NonRemovableEntity,
            ICompositeTaggregateTevent.Component.NonRemovableEntity.Created,
            CompositeTaggregateTevent.Component.NonRemovableEntity.IdGetterSetter>
    {
        static readonly TeventRouting<NonRemovableEntity, ICompositeTaggregateTevent.Component.NonRemovableEntity> NonRemovableEntityAppliers = DeclareAppliers(it => it
           .For<ICompositeTaggregateTevent.Component.NonRemovableEntity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

        protected override TeventRouting TeventAppliers => NonRemovableEntityAppliers;

        public string Name { get; private set; } = string.Empty;

        public NonRemovableEntity(Component parent) : base(parent) {}

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.NonRemovableEntity.Renamed(name));

        internal NamedEntitySnapshot TakeSnapshot() => new(Id, Name);
        internal void RestoreFrom(NamedEntitySnapshot snapshot) => Name = snapshot.Name;
    }
}
