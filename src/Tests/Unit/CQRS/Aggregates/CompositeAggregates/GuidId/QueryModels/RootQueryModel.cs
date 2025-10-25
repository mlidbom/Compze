using System;
using Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

class RootQueryModel : SelfGeneratingQueryModel<RootQueryModel, CompositeAggregateTevent.ICompositeAggregateTevent>
{
   public string Name { get; private set; } = string.Empty;
   readonly Entity.CollectionManager _entities;
   public Component Component { get; private set; }

   public RootQueryModel()
   {
      Component = new Component(this);
      _entities = Entity.CreateSelfManagingCollection(this);

      RegisterTeventAppliers()
        .For<CompositeAggregateTevent.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
}