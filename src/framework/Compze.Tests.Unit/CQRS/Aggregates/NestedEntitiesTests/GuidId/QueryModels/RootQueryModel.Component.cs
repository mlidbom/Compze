using System;
using Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;

partial class Component : RootQueryModel.Component<Component, RootEvent.Component.IRoot>
{
   public Component(RootQueryModel root) : base(root)
   {
      _entities = Component.Entity.CreateSelfManagingCollection(this);
      CComponent = new NestedComponent(this);
      RegisterEventAppliers()
        .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   readonly Component.Entity.CollectionManager _entities;
   // ReSharper disable once NotAccessedField.Local
   public NestedComponent CComponent { get; private set; }

   public string Name { get; private set; } = string.Empty;
   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
}