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

      [XFact] public void Aggregate_Entity_Entity_tests()
      {
         var agRootEntity = Aggregate.AddEntity("RootEntityName");
         var qmRootEntity = QueryModel.Entities.InCreationOrder[0];

         var entity1Id = Guid.NewGuid();
         var agNestedEntity1 = agRootEntity.AddEntity("entity1", entity1Id);
         var qmNestedEntity1 = qmRootEntity.Entities.InCreationOrder[0];

         agRootEntity.Invoking(it => it.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

         agNestedEntity1.Id.Should().Be(entity1Id);
         qmNestedEntity1.Id.Should().Be(entity1Id);
         agNestedEntity1.Name.Should().Be("entity1");
         qmNestedEntity1.Name.Should().Be("entity1");
         agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
         qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
         agRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(true);
         qmRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(true);
         agRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(agNestedEntity1);
         qmRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(qmNestedEntity1);
         agRootEntity.Entities[agNestedEntity1.Id].Should().Be(agNestedEntity1);
         qmRootEntity.Entities[agNestedEntity1.Id].Should().Be(qmNestedEntity1);

         var entity2Id = Guid.NewGuid();
         var agNestedEntity2 = agRootEntity.AddEntity("entity2", entity2Id);
         var qmNestedEntity2 = qmRootEntity.Entities.InCreationOrder[1];
         agRootEntity.Invoking(it => it.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

         agNestedEntity2.Id.Should().Be(entity2Id);
         qmNestedEntity2.Id.Should().Be(entity2Id);
         agNestedEntity2.Name.Should().Be("entity2");
         qmNestedEntity2.Name.Should().Be("entity2");
         agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
         qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
         agRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(true);
         qmRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(true);
         agRootEntity.Entities[agNestedEntity2.Id].Should().Be(agNestedEntity2);
         qmRootEntity.Entities[agNestedEntity2.Id].Should().Be(qmNestedEntity2);

         agNestedEntity1.Rename("newName");
         agNestedEntity1.Name.Should().Be("newName");
         qmNestedEntity1.Name.Should().Be("newName");
         agNestedEntity2.Name.Should().Be("entity2");
         qmNestedEntity2.Name.Should().Be("entity2");

         agNestedEntity2.Rename("newName2");
         agNestedEntity2.Name.Should().Be("newName2");
         qmNestedEntity2.Name.Should().Be("newName2");
         agNestedEntity1.Name.Should().Be("newName");
         qmNestedEntity1.Name.Should().Be("newName");

         agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
         qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);

         agNestedEntity2.Remove();
         agRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(false);
         qmRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(false);
         agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
         qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
         agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
         qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
         agRootEntity.Invoking(_ =>
         {
            var __ = agRootEntity.Entities[agNestedEntity2.Id];
         }).Should().Throw<Exception>();
         qmRootEntity.Invoking(_ =>
         {
            var __ = agRootEntity.Entities[agNestedEntity2.Id];
         }).Should().Throw<Exception>();

         agNestedEntity1.Remove();
         agRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(false);
         qmRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(false);
         agRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
         qmRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
         agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
         qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
         agRootEntity.Invoking(_ =>
         {
            var __ = agRootEntity.Entities[agNestedEntity1.Id];
         }).Should().Throw<Exception>();
         qmRootEntity.Invoking(_ =>
         {
            var __ = agRootEntity.Entities[agNestedEntity1.Id];
         }).Should().Throw<Exception>();
      }
   }
}
