using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;
using System;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

class CompositeTaggregateTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), ICompositeTaggregateTevent<T> where T : ICompositeTaggregateTevent;

abstract class CompositeTaggregateTevent : TaggregateTevent, ICompositeTaggregateTevent
{
   protected CompositeTaggregateTevent() {}
   CompositeTaggregateTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal class Created(TaggregateId id, string name) : CompositeTaggregateTevent(id), ICompositeTaggregateTevent.Created
   {
      public string Name { get; } = name;
   }

   public abstract class Component : CompositeTaggregateTevent, ICompositeTaggregateTevent.Component
   {
      internal class Renamed(string name) : Component, ICompositeTaggregateTevent.Component.Renamed
      {
         public string Name { get; } = name;
      }

      internal abstract class NestedComponent : Component, ICompositeTaggregateTevent.Component.NestedComponent
      {
         public new class Renamed(string name) : NestedComponent, ICompositeTaggregateTevent.Component.NestedComponent.Renamed
         {
            public string Name { get; } = name;
         }
      }

      public new abstract class Entity : Component, ICompositeTaggregateTevent.Component.Entity
      {
         public Guid EntityId { get; private set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
         [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Entity, ICompositeTaggregateTevent.Component.Entity>
#pragma warning restore CA1812
         {
            public void SetEntityId(Entity tevent, Guid id) => tevent.EntityId = id;
            public Guid GetId(ICompositeTaggregateTevent.Component.Entity tevent) => tevent.EntityId;
         }

         public new class Created : Entity, ICompositeTaggregateTevent.Component.Entity.Created
         {
            public Created(Guid entityId, string name)
            {
               EntityId = entityId;
               Name = name;
            }

            public string Name { get; }
         }

         public new class Renamed(string name) : Entity, ICompositeTaggregateTevent.Component.Entity.Renamed
         {
            public string Name { get; } = name;
         }

         internal class Removed : Entity, ICompositeTaggregateTevent.Component.Entity.Removed;
      }
   }

   public abstract class Entity : CompositeTaggregateTevent, ICompositeTaggregateTevent.Entity
   {
      public Guid EntityId { get; private set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
      [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Entity, ICompositeTaggregateTevent.Entity>
#pragma warning restore CA1812
      {
         public void SetEntityId(Entity tevent, Guid id) => tevent.EntityId = id;
         public Guid GetId(ICompositeTaggregateTevent.Entity tevent) => tevent.EntityId;
      }

      public new class Created : Entity, ICompositeTaggregateTevent.Entity.Created
      {
         public Created(Guid entityId, string name)
         {
            EntityId = entityId;
            Name = name;
         }

         public string Name { get; }
      }

      internal class Renamed(string name) : Entity, ICompositeTaggregateTevent.Entity.Renamed
      {
         public string Name { get; } = name;
      }

      internal class Removed : Entity, ICompositeTaggregateTevent.Entity.Removed;

      public abstract class NestedEntity : Entity, ICompositeTaggregateTevent.Entity.NestedEntity
      {
         public Guid NestedEntityId { get; private set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
         [UsedImplicitly] public new class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, NestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity>
#pragma warning restore CA1812
         {
            public void SetEntityId(NestedEntity tevent, Guid id) => tevent.NestedEntityId = id;
            public Guid GetId(ICompositeTaggregateTevent.Entity.NestedEntity tevent) => tevent.NestedEntityId;
         }

         public new class Created : NestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity.Created
         {
            public Created(Guid id, string name)
            {
               NestedEntityId = id;
               Name = name;
            }

            public string Name { get; }
         }

         public new class Renamed(string name) : NestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity.Renamed
         {
            public string Name { get; } = name;
         }

         public new class Removed : NestedEntity, ICompositeTaggregateTevent.Entity.NestedEntity.Removed;
      }
   }
}
