using System;
using Compze.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

static partial class CompositeAggregateEvent
{
   public interface ICompositeAggregateEvent : IAggregateEvent;

   interface Created : IAggregateCreatedEvent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : CompositeAggregateEvent.ICompositeAggregateEvent
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : AggregateEvent, ICompositeAggregateEvent
      {
         protected Root() { }
         protected Root(Guid aggregateId) : base(aggregateId) { }
      }

      public class Created(Guid id, string name) : Root(id), CompositeAggregateEvent.Created
      {
         public string Name { get; } = name;
      }
   }
}