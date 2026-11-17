using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using JetBrains.Annotations;
// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

[UsedImplicitly] partial class RemovableEntity
{
#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   // ReSharper disable once ClassNeverInstantiated.Global
   public class RemovableNestedEntity :
#pragma warning restore CA1812
        RemovableEntity.RemovableEntity<
            RemovableNestedEntity,
            Guid,
            ICompositeTaggregateTevent.Entity.NestedEntity,
            CompositeTaggregateTevent.Entity.NestedEntity,
            ICompositeTaggregateTevent.Entity.NestedEntity.Created,
            ICompositeTaggregateTevent.Entity.NestedEntity.Removed,
            CompositeTaggregateTevent.Entity.NestedEntity.IdGetterSetter>, ITeventRouted
    {
        static readonly TeventRouting<RemovableNestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity> NestedEntityAppliers = DeclareAppliers(it => it
           .For<ICompositeTaggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

        TeventRouting ITeventRouted.TeventAppliers => NestedEntityAppliers;

        public string Name { get; private set; } = string.Empty;

        public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity) {}

        public void Rename(string name) => Publish(new CompositeTaggregateTevent.Entity.NestedEntity.Renamed(name: name));
        public void Remove() => Publish(new CompositeTaggregateTevent.Entity.NestedEntity.Removed());

        internal NamedEntitySnapshot TakeSnapshot() => new(Id, Name);
        internal void RestoreFrom(NamedEntitySnapshot snapshot) => Name = snapshot.Name;
    }
}
