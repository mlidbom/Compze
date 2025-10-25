using Compze.Tessaging.Teventive;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.IntegerId.Domain;

static partial class RootTevent
{
   public static partial class Component
   {
      public static class Entity
      {
         public interface IRoot : RootTevent.Component.IRoot
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
            public abstract class Root : RootTevent.Component.Implementation.Root, Entity.IRoot
            {
               public int EntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
               [UsedImplicitly] public class IdGetterSetter : IGetSetAggregateEntityTeventEntityId<int, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root @tevent, int id) => @tevent.EntityId = id;
                  public int GetId(IRoot @tevent) => @tevent.EntityId;
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