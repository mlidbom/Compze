using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;
using System;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

static class CompositeTaggregateTevent
{
   public interface ICompositeTaggregateTevent : ITaggregateTevent;

   interface Created : ITaggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : CompositeTaggregateTevent.ICompositeTaggregateTevent
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : TaggregateTevent, ICompositeTaggregateTevent
      {
         protected Root() {}
         protected Root(TaggregateId taggregateId) : base(taggregateId) {}
      }

      public class Created(TaggregateId id, string name) : Root(id), CompositeTaggregateTevent.Created
      {
         public string Name { get; } = name;
      }
   }

   public static class Component
   {
      public interface IRoot : CompositeTaggregateTevent.ICompositeTaggregateTevent;

      interface Renamed : PropertyUpdated.Name;

      public static class PropertyUpdated
      {
         public interface Name : IRoot
         {
            string Name { get; }
         }
      }

      internal static class Implementation
      {
         public abstract class Root : CompositeTaggregateTevent.Implementation.Root, Component.IRoot;

         public class Renamed(string name) : Root, Component.Renamed
         {
            public string Name { get; } = name;
         }
      }

      internal static class NestedComponent
      {
         internal interface IRoot : Component.IRoot;
         internal interface Renamed : PropertyUpdated.Name;

         internal static class PropertyUpdated
         {
            public interface Name : NestedComponent.IRoot
            {
               string Name { get; }
            }
         }

         internal static class Implementation
         {
            public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot;

            public class Renamed(string name) : Root, NestedComponent.Renamed
            {
               public string Name { get; } = name;
            }
         }
      }

      public static class Entity
      {
         public interface IRoot : CompositeTaggregateTevent.Component.IRoot
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
            public abstract class Root : CompositeTaggregateTevent.Component.Implementation.Root, Entity.IRoot
            {
               public Guid EntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
               [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root tevent, Guid id) => tevent.EntityId = id;
                  public Guid GetId(IRoot tevent) => tevent.EntityId;
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

   public static class Entity
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
               public void SetEntityId(Root tevent, Guid id) => tevent.EntityId = id;
               public Guid GetId(IRoot tevent) => tevent.EntityId;
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

      public static class NestedEntity
      {
         public interface IRoot : CompositeTaggregateTevent.Entity.IRoot
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
            public abstract class Root : CompositeTaggregateTevent.Entity.Implementation.Root, NestedEntity.IRoot
            {
               public Guid NestedEntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
               [UsedImplicitly] public new class IdGetterSetter : Root, IGetSetTaggregateEntityTeventEntityId<Guid, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root tevent, Guid id) => tevent.NestedEntityId = id;
                  public Guid GetId(IRoot tevent) => tevent.NestedEntityId;
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
