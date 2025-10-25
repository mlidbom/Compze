using System;
using Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component : RootQueryModel.Component<Component, CompositeTaggregateTevent.Component.IRoot>
{
   public Component(RootQueryModel root) : base(root)
   {
      _entities = Component.Entity.CreateSelfManagingCollection(this);
      CComponent = new NestedComponent(this);
      RegisterTeventAppliers()
        .For<CompositeTaggregateTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   readonly Component.Entity.CollectionManager _entities;
   // ReSharper disable once NotAccessedField.Local
   public NestedComponent CComponent { get; private set; }

   public string Name { get; private set; } = string.Empty;
   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
}