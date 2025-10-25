using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.IntegerId.Domain;

static partial class RootTevent
{
   public interface IRoot : IAggregateTevent;

   interface Created : IAggregateCreatedTevent, PropertyUpdated.Name;

   public static class PropertyUpdated
   {
      public interface Name : RootTevent.IRoot
      {
         string Name { get; }
      }
   }

   internal static class Implementation
   {
      public abstract class Root : AggregateTevent, IRoot
      {
         protected Root() { }
         protected Root(Guid aggregateId) : base(aggregateId) { }
      }

      public class Created(Guid id, string name) : Root(id), RootTevent.Created
      {
         public string Name { get; } = name;
      }
   }
}