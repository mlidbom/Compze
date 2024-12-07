using Compze.Persistence.EventStore;
using Compze.SystemCE.ReactiveCE;
using Compze.Testing;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming
// ReSharper disable ImplicitlyCapturedClosure

// ReSharper disable MemberHidesStaticFromOuterClass

#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId;

using System;

public class NestedEntitiesTests : UniversalTestBase
{
   Root Ag;
   RootQueryModel Qm;
   Guid AggregateId;

   public NestedEntitiesTests()
   {
      AggregateId = Guid.NewGuid();
      Ag = new Root("root", AggregateId);
      Qm = new RootQueryModel();
      IEventStored eventStored = Ag;
      eventStored.EventStream.Subscribe(@event => Qm.ApplyEvent((RootEvent.IRoot)@event));
      eventStored.Commit(Qm.LoadFromHistory);
   }

   [Fact] public void ConstructorWorks()
   {
      Ag.Name.Should().Be("root");
      Qm.Name.Should().Be("root");
      Ag.Id.Should().Be(AggregateId);
      Qm.Id.Should().Be(AggregateId);
   }

   public class After_constructor_runs : NestedEntitiesTests
   {
      [Fact] public void AggregateName_is_root() => Ag.Name.Should().Be("root");
      public class After_constructor_runs_nested : After_constructor_runs
      {
         [Fact] public void AggregateName_is_root_again() => Ag.Name.Should().Be("root");
      }
      public class After_constructor_runs_nested2 : After_constructor_runs
      {
         [Fact] public void AggregateName_is_root_again2() => Ag.Name.Should().Be("root");
      }
   }

   [Fact] public void Aggregate_entity_tests()
   {
      var agEntity1 = Ag.AddEntity("entity1");
      var qmEntity1 = Qm.Entities.InCreationOrder[0];
      qmEntity1.Id.Should().Be(agEntity1.Id);
      agEntity1.Name.Should().Be("entity1");
      qmEntity1.Name.Should().Be("entity1");
   }

   [Fact] public void Aggregate_Component_tests()
   {
      Ag.Component.Name.Should().BeNullOrEmpty();
      Qm.Component.Name.Should().BeNullOrEmpty();

      Ag.Component.Rename("newName");
      Ag.Component.Name.Should().Be("newName");
      Qm.Component.Name.Should().Be("newName");
   }

   [Fact] public void Aggregate_Component_Component_tests()
   {
      Ag.Component.CComponent.Name.Should().BeNullOrEmpty();
      Qm.Component.CComponent.Name.Should().BeNullOrEmpty();
      Ag.Component.CComponent.Rename("newName");
      Ag.Component.CComponent.Name.Should().Be("newName");
      Qm.Component.CComponent.Name.Should().Be("newName");
   }

   [Fact] public void Aggregate_Component_Entity_tests()
   {
      var agComponent = Ag.Component;
      var qmComponent = Qm.Component;

      var entity1Id = Guid.NewGuid();
      var agComponentEntity1 = agComponent.AddEntity("entity1", entity1Id);
      agComponent.Invoking(it => it.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

      var qmComponentEntity1 = qmComponent.Entities.InCreationOrder[0];

   }

   [Fact] public void Aggregate_Entity_Entity_tests()
   {
      var agRootEntity = Ag.AddEntity("RootEntityName");
      var qmRootEntity = Qm.Entities.InCreationOrder[0];

      var entity1Id = Guid.NewGuid();
      var agNestedEntity1 = agRootEntity.AddEntity("entity1", entity1Id);
      var qmNestedEntity1 = qmRootEntity.Entities.InCreationOrder[0];

      agRootEntity.Invoking(it => it.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

   }
}
