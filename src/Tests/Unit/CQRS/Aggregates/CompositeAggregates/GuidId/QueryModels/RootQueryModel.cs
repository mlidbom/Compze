using System;
using Compze.Tessaging.Teventive.EventStore.Tuery.Models.SelfGeneratingQueryModels;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

class RootQueryModel : SelfGeneratingQueryModel<RootQueryModel, CompositeAggregateEvent.ICompositeAggregateTevent>
{
   public string Name { get; private set; } = string.Empty;
   readonly Entity.CollectionManager _entities;
   public Component Component { get; private set; }

   public RootQueryModel()
   {
      Component = new Component(this);
      _entities = Entity.CreateSelfManagingCollection(this);

      RegisterEventAppliers()
        .For<CompositeAggregateEvent.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
}