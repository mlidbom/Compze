using Compze.Tessaging.Teventive;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.XUnit.CQRS.Aggregates.CompositeAggregates.IntegerId.Domain;

static partial class RootEvent
{
   public static partial class Entity
   {
      public static class NestedEntity
      {
         public interface IRoot : RootEvent.Entity.IRoot
         {
            int NestedEntityId { get; }
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
            public abstract class Root : RootEvent.Entity.Implementation.Root, NestedEntity.IRoot
            {
               public int NestedEntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
               [UsedImplicitly] public new class IdGetterSetter : Root, IGetSetAggregateEntityEventEntityId<int, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root @event, int id) => @event.NestedEntityId = id;
                  public int GetId(IRoot @event) => @event.NestedEntityId;
               }
            }

            public class Created : Root, NestedEntity.Created
            {
               public Created(int nestedEntityId, string name)
               {
                  NestedEntityId = nestedEntityId;
                  Name = name;
               }
               public string Name { get; }
            }

            public class Renamed(string name) : Root, NestedEntity.Renamed
            {
               public string Name { get; } = name;
            }

            public class Removed : Root, NestedEntity.Removed;
         }
      }
   }
}