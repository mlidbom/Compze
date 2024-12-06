using System;
using Compze.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

static partial class RootEvent
{
   public interface IRoot : IAggregateEvent;

   interface Created : IAggregateCreatedEvent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : RootEvent.IRoot
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : AggregateEvent, IRoot
      {
         protected Root() { }
         protected Root(Guid aggregateId) : base(aggregateId) { }
      }

      public class Created(Guid id, string name) : Root(id), RootEvent.Created
      {
         public string Name { get; } = name;
      }
   }
}