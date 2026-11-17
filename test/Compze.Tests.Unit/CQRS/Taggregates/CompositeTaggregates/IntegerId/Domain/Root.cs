using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

class Root : Taggregate<Root, IRootTevent, RootTevent, IRootTevent<IRootTevent>, RootTevent<RootTevent>>, ITeventRouted
{
   static readonly TeventRouting<Root, IRootTevent> Appliers = DeclareAppliers(it => it
     .Component(static root => root.Component)
     .Entities(static root => root._entities)
     .For<IRootTevent.PropertyUpdated.Name>(static (root, tevent) => root.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   static int _instances;
   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.ICollectionManager _entities;
   public Component Component { get; private set; }

   public Root(string name)
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);

      Publish(new RootTevent.Created(new TaggregateId(), name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, int> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new RootTevent.Entity.Created(++_instances, name));
}

class Component : Root.Component<Component, IRootTevent.Component, RootTevent.Component>, ITeventRouted
{
   static readonly TeventRouting<Component, IRootTevent.Component> Appliers = DeclareAppliers(it => it
     .Entities(static component => component._entities)
     .For<IRootTevent.Component.PropertyUpdated.Name>(static (component, tevent) => component.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   static int _instances;
   public string? Name { get; private set; } = string.Empty;

   public Component(Root parent): base(parent) => _entities = Entity.CreateSelfManagingCollection(this);

   public IReadOnlyEntityCollection<Entity, int> Entities => _entities.Entities;
   readonly Entity.ICollectionManager _entities;

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
      RootTevent.Component.Entity.IdGetterSetter>, ITeventRouted
   {
      static readonly TeventRouting<Entity, IRootTevent.Component.Entity> EntityAppliers = DeclareAppliers(it => it
        .For<IRootTevent.Component.Entity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

      TeventRouting ITeventRouted.TeventAppliers => EntityAppliers;

      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent) {}

      public void Rename(string name) => Publish(new RootTevent.Component.Entity.Renamed(name));
      public void Remove() => Publish(new RootTevent.Component.Entity.Removed());
   }
}

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
[UsedImplicitly]class RemovableEntity : Root.RemovableEntity<RemovableEntity,
#pragma warning restore CA1812
   int,
   IRootTevent.Entity,
   RootTevent.Entity,
   IRootTevent.Entity.Created,
   IRootTevent.Entity.Removed,
   RootTevent.Entity.IdGetterSetter>, ITeventRouted
{
   static readonly TeventRouting<RemovableEntity, IRootTevent.Entity> Appliers = DeclareAppliers(it => it
     .Entities(static entity => entity._entities)
     .For<IRootTevent.Entity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   static int _instances;
   public string Name { get; private set; } = string.Empty;
   public RemovableEntity(Root root) : base(root) => _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);

   public IReadOnlyEntityCollection<RemovableNestedEntity, int> Entities => _entities.Entities;
   readonly RemovableNestedEntity.ICollectionManager _entities;

   public void Rename(string name) => Publish(new RootTevent.Entity.Renamed(name));
   public void Remove() => Publish(new RootTevent.Entity.Removed());

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
   // ReSharper disable once ClassNeverInstantiated.Global
   public class RemovableNestedEntity : RemovableEntity<RemovableNestedEntity,
#pragma warning restore CA1812
      int,
      IRootTevent.Entity.NestedEntity,
      RootTevent.Entity.NestedEntity,
      IRootTevent.Entity.NestedEntity.Created,
      IRootTevent.Entity.NestedEntity.Removed,
      RootTevent.Entity.NestedEntity.IdGetterSetter>, ITeventRouted
   {
      static readonly TeventRouting<RemovableNestedEntity, IRootTevent.Entity.NestedEntity> NestedEntityAppliers = DeclareAppliers(it => it
        .For<IRootTevent.Entity.NestedEntity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

      TeventRouting ITeventRouted.TeventAppliers => NestedEntityAppliers;

      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity) {}

      public void Rename(string name) => Publish(new RootTevent.Entity.NestedEntity.Renamed(name: name));
      public void Remove() => Publish(new RootTevent.Entity.NestedEntity.Removed());

   }

   public RemovableNestedEntity AddEntity(string name)
      => _entities.AddByPublishing(new RootTevent.Entity.NestedEntity.Created(nestedEntityId: ++_instances, name: name));
}