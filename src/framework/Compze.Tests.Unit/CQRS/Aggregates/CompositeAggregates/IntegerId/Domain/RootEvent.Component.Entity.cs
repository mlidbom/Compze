using Compze.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.IntegerId.Domain;

static partial class RootEvent
{
   public static partial class Component
   {
      public static class Entity
      {
         public interface IRoot : RootEvent.Component.IRoot
         {
            int EntityId { get; }
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
            public abstract class Root : RootEvent.Component.Implementation.Root, Entity.IRoot
            {
               public int EntityId { get; protected set; }

               [UsedImplicitly] public class IdGetterSetter : IGetSetAggregateEntityEventEntityId<int, Root, IRoot>
               {
                  public void SetEntityId(Root @event, int id) => @event.EntityId = id;
                  public int GetId(IRoot @event) => @event.EntityId;
               }
            }

            public class Created : Root, Entity.Created
            {
               public Created(int entityId, string name)
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
}