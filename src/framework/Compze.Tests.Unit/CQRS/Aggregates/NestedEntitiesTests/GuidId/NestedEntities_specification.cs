using System;
using Compze.Persistence.EventStore;
using Compze.SystemCE.ReactiveCE;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;

// ReSharper disable InconsistentNaming
// ReSharper disable ImplicitlyCapturedClosure

// ReSharper disable MemberHidesStaticFromOuterClass

#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      readonly Root Aggregate;
      readonly RootQueryModel QueryModel;
      readonly Guid AggregateId;

      public After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events()
      {
         AggregateId = Guid.NewGuid();
         Aggregate = new Root("root", AggregateId);
         QueryModel = new RootQueryModel();
         IEventStored eventStored = Aggregate;
         eventStored.EventStream.Subscribe(@event => QueryModel.ApplyEvent((RootEvent.IRoot)@event));
         eventStored.Commit(QueryModel.LoadFromHistory);
      }

      [XFact] public void Aggregate_name_is_root() => Aggregate.Name.Should().Be("root");
      [XFact] public void Query_model_name_is_root() => QueryModel.Name.Should().Be("root");
      [XFact] public void Aggregate_id_is_the_supplied_id() => Aggregate.Id.Should().Be(AggregateId);
      [XFact] public void QueryModel_id_is_the_supplied_id() => QueryModel.Id.Should().Be(AggregateId);

      [XFact] public void Aggregate_Component_Component_tests()
      {
         Aggregate.Component.CComponent.Name.Should().BeNullOrEmpty();
         QueryModel.Component.CComponent.Name.Should().BeNullOrEmpty();
         Aggregate.Component.CComponent.Rename("newName");
         Aggregate.Component.CComponent.Name.Should().Be("newName");
         QueryModel.Component.CComponent.Name.Should().Be("newName");
      }
   }
}
