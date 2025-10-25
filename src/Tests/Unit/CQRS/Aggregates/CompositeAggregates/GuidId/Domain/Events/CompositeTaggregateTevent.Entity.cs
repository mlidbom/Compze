using System;
using Compze.Tessaging.Teventive;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

static partial class CompositeTaggregateTevent
{
   public static partial class Entity
   {
      public interface IRoot : CompositeTaggregateTevent.ICompositeTaggregateTevent
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
         public abstract class Root : CompositeTaggregateTevent.Implementation.Root, Entity.IRoot
         {
            public Guid EntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
            [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Root, IRoot>
#pragma warning restore CA1812
            {
               public void SetEntityId(Root @tevent, Guid id) => @tevent.EntityId = id;
               public Guid GetId(IRoot @tevent) => @tevent.EntityId;
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