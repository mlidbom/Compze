using System;
using Compze.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

static partial class RootEvent
{
   public static partial class Entity
   {
      public interface IRoot : RootEvent.IRoot
      {
         Guid EntityId { get; }
      }

      public interface Created : PropertyUpdated.Name;

      interface Renamed : PropertyUpdated.Name;

      public interface Removed : IRoot;

      public static class PropertyUpdated
      {
         public interface Name : IRoot
         {
            string Name { get; }
         }
      }

      internal static class Implementation
      {
         public abstract class Root : RootEvent.Implementation.Root, Entity.IRoot
         {
            public Guid EntityId { get; protected set; }

            [UsedImplicitly] public class IdGetterSetter : IGetSetAggregateEntityEventEntityId<Guid, Root, IRoot>
            {
               public void SetEntityId(Root @event, Guid id) => @event.EntityId = id;
               public Guid GetId(IRoot @event) => @event.EntityId;
            }
         }

         public class Created : Root, Entity.Created
         {
            public Created(Guid entityId, string name)
            {
               EntityId = entityId;
               Name = name;
            }
            public string Name { get; }
         }

         public class Renamed(string name) : Root, Entity.Renamed
         {
            public string Name { get; } = name;
         }

         public class Removed : Root, Entity.Removed;
      }
   }
}