using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using JetBrains.Annotations;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class Entity : RootQueryModel.Entity<Entity, Guid, ICompositeTaggregateTevent.Component.Entity, ICompositeTaggregateTevent.Component.Entity.Created, ICompositeTaggregateTevent.Component.Entity.Removed, CompositeTaggregateTevent.Component.Entity.IdGetterSetter>, ITeventRouted
#pragma warning restore CA1812
   {
      static readonly TeventRouting<Entity, ICompositeTaggregateTevent.Component.Entity> EntityAppliers = DeclareAppliers(it => it
        .For<ICompositeTaggregateTevent.Component.Entity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

      TeventRouting ITeventRouted.TeventAppliers => EntityAppliers;

      public string Name { get; private set; } = string.Empty;
   }
}
