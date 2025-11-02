using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

class Root : Taggregate<Root, RootTevent.IRoot, RootTevent.Implementation.Root>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.CollectionManager _entities;
   public Component Component { get; private set; }

   public Root(string name)
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);

      RegisterTeventAppliers()
        .For<RootTevent.PropertyUpdated.Name>(e => Name = e.Name);

      Publish(new RootTevent.Implementation.Created(new TaggregateId(), name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, int> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new RootTevent.Entity.Implementation.Created(++_instances, name));
}

class Component : Root.Component<Component, RootTevent.Component.Implementation.Root, RootTevent.Component.IRoot>
{
   static int _instances;
   public string? Name { get; private set; } = string.Empty;

   public Component(Root parent): base(parent)
   {
       _entities = Entity.CreateSelfManagingCollection(this);

       RegisterTeventAppliers()
          .For<RootTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
   readonly Entity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootTevent.Component.Implementation.Renamed(name));
   public Entity AddEntity(string name) => _entities.AddByPublishing(new RootTevent.Component.Entity.Implementation.Created(++_instances, name));

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   [UsedImplicitly]public class Entity : RemovableEntity<Entity,
#pragma warning restore CA1812
      int,
      RootTevent.Component.Entity.IRoot,
      RootTevent.Component.Entity.Implementation.Root,
      RootTevent.Component.Entity.Created,
      RootTevent.Component.Entity.Removed,
      RootTevent.Component.Entity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<RootTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootTevent.Component.Entity.Implementation.Renamed(name));
      public void Remove() => Publish(new RootTevent.Component.Entity.Implementation.Removed());
   }
}

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
[UsedImplicitly]class RemovableEntity : Root.RemovableEntity<RemovableEntity,
#pragma warning restore CA1812
   int,
   RootTevent.Entity.Implementation.Root,
   RootTevent.Entity.IRoot,
   RootTevent.Entity.Created,
   RootTevent.Entity.Removed,
   RootTevent.Entity.Implementation.Root.IdGetterSetter>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;
   public RemovableEntity(Root root) : base(root)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterTeventAppliers()
        .For<RootTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<RemovableNestedEntity, int> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootTevent.Entity.Implementation.Renamed(name));
   public void Remove() => Publish(new RootTevent.Entity.Implementation.Removed());

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   public class RemovableNestedEntity : RemovableEntity<RemovableNestedEntity,
#pragma warning restore CA1812
      int,
      RootTevent.Entity.NestedEntity.IRoot,
      RootTevent.Entity.NestedEntity.Implementation.Root,
      RootTevent.Entity.NestedEntity.Created,
      RootTevent.Entity.NestedEntity.Removed,
      RootTevent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
      {
         RegisterTeventAppliers()
           .For<RootTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootTevent.Entity.NestedEntity.Implementation.Renamed(name: name));
      public void Remove() => Publish(new RootTevent.Entity.NestedEntity.Implementation.Removed());

   }

   public RemovableNestedEntity AddEntity(string name)
      => _entities.AddByPublishing(new RootTevent.Entity.NestedEntity.Implementation.Created(nestedEntityId: ++_instances, name: name));
}