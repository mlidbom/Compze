using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

class Root : Taggregate<Root, IRootTevent, RootTevent, IRootTevent<IRootTevent>, RootTevent<RootTevent>>
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
        .For<IRootTevent.PropertyUpdated.Name>(e => Name = e.Name);

      Publish(new RootTevent.Created(new TaggregateId(), name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, int> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new RootTevent.Entity.Created(++_instances, name));
}

class Component : Root.Component<Component, IRootTevent.Component, RootTevent.Component>
{
   static int _instances;
   public string? Name { get; private set; } = string.Empty;

   public Component(Root parent): base(parent)
   {
       _entities = Entity.CreateSelfManagingCollection(this);

       RegisterTeventAppliers()
          .For<IRootTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
   readonly Entity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootTevent.Component.Renamed(name));
   public Entity AddEntity(string name) => _entities.AddByPublishing(new RootTevent.Component.Entity.Created(++_instances, name));

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   [UsedImplicitly]public class Entity : RemovableEntity<Entity,
#pragma warning restore CA1812
      int,
      IRootTevent.Component.Entity,
      RootTevent.Component.Entity,
      IRootTevent.Component.Entity.Created,
      IRootTevent.Component.Entity.Removed,
      RootTevent.Component.Entity.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<IRootTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootTevent.Component.Entity.Renamed(name));
      public void Remove() => Publish(new RootTevent.Component.Entity.Removed());
   }
}

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
[UsedImplicitly]class RemovableEntity : Root.RemovableEntity<RemovableEntity,
#pragma warning restore CA1812
   int,
   RootTevent.Entity,
   IRootTevent.Entity,
   IRootTevent.Entity.Created,
   IRootTevent.Entity.Removed,
   RootTevent.Entity.IdGetterSetter>
{
   static int _instances;
   public string Name { get; private set; } = string.Empty;
   public RemovableEntity(Root root) : base(root)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterTeventAppliers()
        .For<IRootTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<RemovableNestedEntity, int> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;

   public void Rename(string name) => Publish(new RootTevent.Entity.Renamed(name));
   public void Remove() => Publish(new RootTevent.Entity.Removed());

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   public class RemovableNestedEntity : RemovableEntity<RemovableNestedEntity,
#pragma warning restore CA1812
      int,
      IRootTevent.Entity.NestedEntity,
      RootTevent.Entity.NestedEntity,
      IRootTevent.Entity.NestedEntity.Created,
      IRootTevent.Entity.NestedEntity.Removed,
      RootTevent.Entity.NestedEntity.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
      {
         RegisterTeventAppliers()
           .For<IRootTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootTevent.Entity.NestedEntity.Renamed(name: name));
      public void Remove() => Publish(new RootTevent.Entity.NestedEntity.Removed());

   }

   public RemovableNestedEntity AddEntity(string name)
      => _entities.AddByPublishing(new RootTevent.Entity.NestedEntity.Created(nestedEntityId: ++_instances, name: name));
}