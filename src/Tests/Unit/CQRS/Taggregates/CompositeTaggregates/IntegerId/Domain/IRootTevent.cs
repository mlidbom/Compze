using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;

#pragma warning disable CS0108 //Hides inherited member.

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.IntegerId.Domain;

interface IRootTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IRootTevent;
interface IRootTevent : ITaggregateTevent
{
   interface Created : IRootTevent, ITaggregateCreatedTevent, PropertyUpdated.Name;

   public interface PropertyUpdated : IRootTevent
   {
      public interface Name : PropertyUpdated
      {
         string Name { get; }
      }
   }

   public interface Component : IRootTevent
   {
      interface Renamed : Component, PropertyUpdated.Name;

      public interface PropertyUpdated : Component
      {
         public interface Name : PropertyUpdated
         {
            string Name { get; }
         }
      }

      public interface Entity : Component
      {
         int EntityId { get; }

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
   }

   public interface Entity : IRootTevent
   {
      int EntityId { get; }

      internal interface Created : Entity, PropertyUpdated.Name;

      interface Renamed : Entity, PropertyUpdated.Name;

      internal interface Removed : Entity;

      internal interface PropertyUpdated : Entity
      {
         public interface Name : PropertyUpdated
         {
            string Name { get; }
         }
      }

      public interface NestedEntity : Entity
      {
         int NestedEntityId { get; }

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
