using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

static partial class RootTevent
{
   public interface IRoot : ITaggregateTevent;

   interface Created : ITaggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : RootTevent.IRoot
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : TaggregateTevent, IRoot
      {
         protected Root() { }
         protected Root(TaggregateId taggregateId) : base(taggregateId) { }
      }

      public class Created(TaggregateId id, string name) : Root(id), RootTevent.Created
      {
         public string Name { get; } = name;
      }
   }

   public static partial class Component
   {
      public interface IRoot : RootTevent.IRoot;

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
         public abstract class Root : RootTevent.Implementation.Root, Component.IRoot;

         public class Renamed(string name) : Root, Component.Renamed
         {
            public string Name { get; } = name;
         }
      }

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

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
               [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<int, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root tevent, int id) => tevent.EntityId = id;
                  public int GetId(IRoot tevent) => tevent.EntityId;
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

   public static partial class Entity
   {
      public interface IRoot : RootTevent.IRoot
      {
         int EntityId { get; }
      }

      internal interface Created : PropertyUpdated.Name;

      interface Renamed : PropertyUpdated.Name;

      internal interface Removed : IRoot;

      internal static class PropertyUpdated
      {
         public interface Name : IRoot
         {
            string Name { get; }
         }
      }

      internal static class Implementation
      {
         public abstract class Root : RootTevent.Implementation.Root, Entity.IRoot
         {
            public int EntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
            [UsedImplicitly] public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<int, Root, IRoot>
#pragma warning restore CA1812
            {
               public void SetEntityId(Root tevent, int id) => tevent.EntityId = id;
               public int GetId(IRoot tevent) => tevent.EntityId;
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

      public static class NestedEntity
      {
         public interface IRoot : RootTevent.Entity.IRoot
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
            public abstract class Root : RootTevent.Entity.Implementation.Root, NestedEntity.IRoot
            {
               public int NestedEntityId { get; protected set; }

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
               [UsedImplicitly] public new class IdGetterSetter : Root, IGetSetTaggregateEntityTeventEntityId<int, Root, IRoot>
#pragma warning restore CA1812
               {
                  public void SetEntityId(Root tevent, int id) => tevent.NestedEntityId = id;
                  public int GetId(IRoot tevent) => tevent.NestedEntityId;
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