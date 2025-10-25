using System;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

static partial class CompositeAggregateEvent
{
   public interface ICompositeAggregateTevent : IAggregateTevent;

   interface Created : IAggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : CompositeAggregateEvent.ICompositeAggregateTevent
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : AggregateTevent, ICompositeAggregateTevent
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