using Compze.Abstractions.Public;
using Compze.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CA1812 //Uninstantiated class (used via reflection)
#pragma warning disable CS0108 //Hides inherited member.
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

class RootTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IRootTevent<T> where T : IRootTevent;
abstract class RootTevent : TaggregateTevent, IRootTevent
{
   protected RootTevent() {}
   RootTevent(TaggregateId taggregateId) : base(taggregateId) {}

   public class Created(TaggregateId id, string name) : RootTevent(id), IRootTevent.Created
   {
      public string Name { get; } = name;
   }

   public class Component : RootTevent, IRootTevent.Component
   {
      public class Renamed(string name) : Component, IRootTevent.Component.Renamed
      {
         public string Name { get; } = name;
      }

      public abstract class Entity : Component, IRootTevent.Component.Entity
      {
         public int EntityId { get; private set; }

         [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<int, Entity, IRootTevent.Component.Entity>
         {
            public void SetEntityId(Entity tevent, int id) => tevent.EntityId = id;
            public int GetId(IRootTevent.Component.Entity tevent) => tevent.EntityId;
         }

         public class Created : Entity, IRootTevent.Component.Entity.Created
         {
            public Created(int entityId, string name)
            {
               EntityId = entityId;
               Name = name;
            }

            public string Name { get; }
         }

         public class Renamed(string name) : Entity, IRootTevent.Component.Entity.Renamed
         {
            public string Name { get; } = name;
         }

         public class Removed : Entity, IRootTevent.Component.Entity.Removed;
      }
   }

   public abstract class Entity : RootTevent, IRootTevent.Entity
   {
      public int EntityId { get; private set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
      [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<int, Entity, IRootTevent.Entity>
#pragma warning restore CA1812
      {
         public void SetEntityId(Entity tevent, int id) => tevent.EntityId = id;
         public int GetId(IRootTevent.Entity tevent) => tevent.EntityId;
      }

      public class Created : Entity, IRootTevent.Entity.Created
      {
         public Created(int entityId, string name)
         {
            EntityId = entityId;
            Name = name;
         }

         public string Name { get; }
      }

      public class Renamed(string name) : Entity, IRootTevent.Entity.Renamed
      {
         public string Name { get; } = name;
      }

      public class Removed : Entity, IRootTevent.Entity.Removed;

      public abstract class NestedEntity : Entity, IRootTevent.Entity.NestedEntity
      {
         public int NestedEntityId { get; private set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
         [UsedImplicitly] public new class IdGetterSetter : NestedEntity, IGetSetTaggregateEntityTeventEntityId<int, NestedEntity, IRootTevent.Entity.NestedEntity>
#pragma warning restore CA1812
         {
            public void SetEntityId(NestedEntity tevent, int id) => tevent.NestedEntityId = id;
            public int GetId(IRootTevent.Entity.NestedEntity tevent) => tevent.NestedEntityId;
         }

         public class Created : NestedEntity, IRootTevent.Entity.NestedEntity.Created
         {
            public Created(int nestedEntityId, string name)
            {
               NestedEntityId = nestedEntityId;
               Name = name;
            }

            public string Name { get; }
         }

         public class Renamed(string name) : NestedEntity, IRootTevent.Entity.NestedEntity.Renamed
         {
            public string Name { get; } = name;
         }

         public class Removed : NestedEntity, IRootTevent.Entity.NestedEntity.Removed;
      }
   }
}
