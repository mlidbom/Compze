using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

// ReSharper disable PossibleInterfaceMemberAmbiguity

#pragma warning disable CA1715 //Interfaces without I prefix
#pragma warning disable CS0108 //Hides inherited member.
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

interface ICompositeTaggregateTevent<out T> : ITaggregateIdentifyingTevent<T> where T : ICompositeTaggregateTevent;

interface ICompositeTaggregateTevent : ITaggregateTevent
{
   interface Created : ICompositeTaggregateTevent, ITaggregateCreatedTevent, PropertyUpdated.Name;

   public interface PropertyUpdated : ICompositeTaggregateTevent
   {
      public interface Name : PropertyUpdated
      {
         string Name { get; }
      }
   }

   public interface Component : ICompositeTaggregateTevent
   {
      interface Renamed : Component, PropertyUpdated.Name;

      public interface PropertyUpdated : Component
      {
         public interface Name : PropertyUpdated
         {
            string Name { get; }
         }
      }

      internal interface NestedComponent : Component
      {
         internal interface Renamed : NestedComponent, PropertyUpdated.Name;

         internal interface PropertyUpdated : NestedComponent
         {
            public interface Name : PropertyUpdated
            {
               string Name { get; }
            }
         }
      }

      public interface Entity : Component
      {
         Guid EntityId { get; }

         public interface Created : Entity, PropertyUpdated.Name;

         interface Renamed : Entity, PropertyUpdated.Name;

         public interface Removed : Entity;

         public interface PropertyUpdated : Entity
         {
            public interface Name : PropertyUpdated
            {
               string Name { get; }
            }
         }
      }

      public interface NonRemovableEntity : Component
      {
         Guid NonRemovableEntityId { get; }

         public interface Created : NonRemovableEntity, PropertyUpdated.Name;

         interface Renamed : NonRemovableEntity, PropertyUpdated.Name;

         public interface PropertyUpdated : NonRemovableEntity
         {
            public interface Name : PropertyUpdated
            {
               string Name { get; }
            }
         }
      }
   }

   public interface Entity : ICompositeTaggregateTevent
   {
      Guid EntityId { get; }

      public interface Created : Entity, PropertyUpdated.Name;

      interface Renamed : Entity, PropertyUpdated.Name;

      public interface Removed : Entity;

      public interface PropertyUpdated : Entity
      {
         public interface Name : PropertyUpdated
         {
            string Name { get; }
         }
      }

      public interface NestedEntity : Entity
      {
         Guid NestedEntityId { get; }

         public interface Created : NestedEntity, PropertyUpdated.Name;

         interface Renamed : NestedEntity, PropertyUpdated.Name;
         public interface Removed : NestedEntity;

         public interface PropertyUpdated : NestedEntity
         {
            public interface Name : PropertyUpdated
            {
               string Name { get; }
            }
         }
      }
   }
}
