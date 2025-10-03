using Compze.GenericAbstractions.Time;
using Compze.Teventive.Aggregates;
using JetBrains.Annotations;
using System;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.IntegerId.Domain;

class Root : Aggregate<Root, RootEvent.IRoot, RootEvent.Implementation.Root>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.CollectionManager _entities;
   public Component Component { get; private set; }

   public Root(string name) : base(new DateTimeNowTimeSource())
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);

      RegisterEventAppliers()
        .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

      Publish(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, int> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new RootEvent.Entity.Implementation.Created(++_instances, name));
}

class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;

   public Component(Root parent): base(parent)
   {
       _entities = Entity.CreateSelfManagingCollection(this);

       RegisterEventAppliers()
          .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
   readonly Entity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootEvent.Component.Implementation.Renamed(name));
   public Entity AddEntity(string name) => _entities.AddByPublishing(new RootEvent.Component.Entity.Implementation.Created(++_instances, name));

   [UsedImplicitly]public class Entity : RemovableEntity<Entity,
      int,
      RootEvent.Component.Entity.IRoot,
      RootEvent.Component.Entity.Implementation.Root,
      RootEvent.Component.Entity.Created,
      RootEvent.Component.Entity.Removed,
      RootEvent.Component.Entity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterEventAppliers()
           .For<RootEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootEvent.Component.Entity.Implementation.Renamed(name));
      public void Remove() => Publish(new RootEvent.Component.Entity.Implementation.Removed());
   }
}

[UsedImplicitly]class RemovableEntity : Root.RemovableEntity<RemovableEntity,
   int,
   RootEvent.Entity.Implementation.Root,
   RootEvent.Entity.IRoot,
   RootEvent.Entity.Created,
   RootEvent.Entity.Removed,
   RootEvent.Entity.Implementation.Root.IdGetterSetter>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;
   public RemovableEntity(Root root) : base(root)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterEventAppliers()
        .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<RemovableNestedEntity, int> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootEvent.Entity.Implementation.Renamed(name));
   public void Remove() => Publish(new RootEvent.Entity.Implementation.Removed());

   public class RemovableNestedEntity : RemovableEntity<RemovableNestedEntity,
      int,
      RootEvent.Entity.NestedEntity.IRoot,
      RootEvent.Entity.NestedEntity.Implementation.Root,
      RootEvent.Entity.NestedEntity.Created,
      RootEvent.Entity.NestedEntity.Removed,
      RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
      {
         RegisterEventAppliers()
           .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
      public void Remove() => Publish(new RootEvent.Entity.NestedEntity.Implementation.Removed());

   }

   public RemovableNestedEntity AddEntity(string name)
      => _entities.AddByPublishing(new RootEvent.Entity.NestedEntity.Implementation.Created(nestedEntityId: ++_instances, name: name));
}