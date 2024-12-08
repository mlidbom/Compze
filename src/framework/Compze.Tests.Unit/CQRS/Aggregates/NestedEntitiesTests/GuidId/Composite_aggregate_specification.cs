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

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      readonly CompositeAggregate _aggregate;
      readonly RootQueryModel _queryModel;
      readonly Guid _aggregateId;

      public After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events()
      {
         _aggregateId = Guid.NewGuid();
         _aggregate = new CompositeAggregate("root", _aggregateId);
         _queryModel = new RootQueryModel();
         IEventStored<CompositeAggregateEvent.ICompositeAggregateEvent> eventStored = _aggregate;
         eventStored.EventStream.Subscribe(_queryModel.ApplyEvent);
         eventStored.Commit(_queryModel.LoadFromHistory);
      }

      [XFact] public void Aggregate_name_is_root() => _aggregate.Name.Should().Be("root");
      [XFact] public void Query_model_name_is_root() => _queryModel.Name.Should().Be("root");
      [XFact] public void Aggregate_id_is_the_supplied_id() => _aggregate.Id.Should().Be(_aggregateId);
      [XFact] public void QueryModel_id_is_the_supplied_id() => _queryModel.Id.Should().Be(_aggregateId);

      [XFact] public void Aggregate_Component_Component_tests()
      {
         _aggregate.Component.CComponent.Name.Should().BeNullOrEmpty();
         _queryModel.Component.CComponent.Name.Should().BeNullOrEmpty();
         _aggregate.Component.CComponent.Rename("newName");
         _aggregate.Component.CComponent.Name.Should().Be("newName");
         _queryModel.Component.CComponent.Name.Should().Be("newName");
      }
   }
}
