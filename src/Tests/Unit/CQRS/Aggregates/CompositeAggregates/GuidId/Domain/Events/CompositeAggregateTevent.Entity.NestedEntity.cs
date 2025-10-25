using System;
using Compze.Tessaging.Teventive;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

static partial class CompositeAggregateTevent
{
   public static partial class Entity
   {
      public static class NestedEntity
      {
         public interface IRoot : CompositeAggregateTevent.Entity.IRoot
         {
            Guid NestedEntityId { get; }
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
            public abstract class Root : CompositeAggregateTevent.Entity.Implementation.Root, NestedEntity.IRoot
            {
               public Guid NestedEntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
               [UsedImplicitly] public new class IdGetterSetter : Root, IGetSetAggregateEntityTeventEntityId<Guid, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root @tevent, Guid id) => @tevent.NestedEntityId = id;
                  public Guid GetId(IRoot @tevent) => @tevent.NestedEntityId;
               }
            }

            public class Created : Root, NestedEntity.Created
            {
               public Created(Guid id, string name)
               {
                  NestedEntityId = id;
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