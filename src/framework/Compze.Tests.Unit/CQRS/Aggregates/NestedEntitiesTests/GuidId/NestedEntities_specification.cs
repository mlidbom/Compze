using System;
using System.Linq;
using Compze.Persistence.EventStore;
using Compze.SystemCE.ReactiveCE;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;
using static FluentAssertions.FluentActions;

// ReSharper disable InconsistentNaming
// ReSharper disable ImplicitlyCapturedClosure

// ReSharper disable MemberHidesStaticFromOuterClass

#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static class NestedEntities_specification
{
   public class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
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

      public class After_adding_entity_named_entity1 : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         readonly RemovableEntity agEntity1;
         readonly Entity qmEntity1;

         public After_adding_entity_named_entity1()
         {
            agEntity1 = Aggregate.AddEntity("entity1");
            qmEntity1 = QueryModel.Entities.Single();
         }

         [XFact] public void Added_entity_is_named_entity1() => agEntity1.Name.Should().Be("entity1");
         [XFact] public void QueryModel_has_a_single_entity_named_entity1() => qmEntity1.Name.Should().Be("entity1");
         [XFact] public void QueryModels_single_entity_has_the_correct_id() => qmEntity1.Id.Should().Be(agEntity1.Id);

         public class The_aggregates_Entities_collection : After_adding_entity_named_entity1
         {
            [XFact] public void Single_returns_the_entity() => Aggregate.Entities.Single().Should().Be(agEntity1);
            [XFact] public void InCreationOrder_0_returns_the_entity() => Aggregate.Entities.InCreationOrder[0].Should().Be(agEntity1);
            [XFact] public void InCreationOrder_Count_is_1() => Aggregate.Entities.InCreationOrder.Count.Should().Be(1);

            public class Passing_the_entitys_id_to : The_aggregates_Entities_collection
            {
               [XFact] public void Contains_returns_true() => Aggregate.Entities.Contains(agEntity1.Id).Should().Be(true);
               [XFact] public void Get_returns_the_entity() => Aggregate.Entities.Get(agEntity1.Id).Should().Be(agEntity1);
               [XFact] public void Indexer_returns_the_entity() => Aggregate.Entities[agEntity1.Id].Should().Be(agEntity1);

               [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  Aggregate.Entities.TryGet(agEntity1.Id, out var agEntity1Fetched).Should().BeTrue();
                  agEntity1Fetched.Should().Be(agEntity1);
               }
            }
         }

         public class The_QueryModels_Entities_collection : After_adding_entity_named_entity1
         {
            [XFact] public void Single_returns_the_entity_query_model() => QueryModel.Entities.Single().Should().Be(qmEntity1);
            [XFact] public void InCreationOrder_0_returns_the_entity_query_model() => QueryModel.Entities.InCreationOrder[0].Should().Be(qmEntity1);
            [XFact] public void InCreationOrder_Count_is_1() => QueryModel.Entities.InCreationOrder.Count.Should().Be(1);

            public class Passing_the_entitys_id_to : The_aggregates_Entities_collection
            {
               [XFact] public void Contains_returns_true() => QueryModel.Entities.Contains(agEntity1.Id).Should().Be(true);
               [XFact] public void Get_returns_the_entity_query_model() => QueryModel.Entities.Get(agEntity1.Id).Should().Be(qmEntity1);
               [XFact] public void Indexer_returns_the_entity() => QueryModel.Entities[agEntity1.Id].Should().Be(qmEntity1);

               [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  QueryModel.Entities.TryGet(agEntity1.Id, out var qmEntity1Fetched).Should().BeTrue();
                  qmEntity1Fetched.Should().Be(qmEntity1);
               }
            }
         }

         public class After_adding_entity_named_entity2 : After_adding_entity_named_entity1
         {
            readonly RemovableEntity agEntity2;
            readonly Entity qmEntity2;

            public After_adding_entity_named_entity2()
            {
               agEntity2 = Aggregate.AddEntity("entity2");
               qmEntity2 = QueryModel.Entities.InCreationOrder[1];
            }

            [XFact] public void The_name_of_the_added_entity_is_entity2() => agEntity2.Name.Should().Be("entity2");
            [XFact] public void The_name_of_the_added_query_model_is_entity2() => qmEntity2.Name.Should().Be("entity2");

            public new class The_aggregates_Entities_collection : After_adding_entity_named_entity2
            {
               [XFact] public void Single_throws() => Invoking(() => Aggregate.Entities.Single()).Should().Throw<Exception>();
               [XFact] public void InCreationOrder_1_returns_the_entity() => Aggregate.Entities.InCreationOrder[1].Should().Be(agEntity2);
               [XFact] public void InCreationOrder_Count_is_2() => Aggregate.Entities.InCreationOrder.Count.Should().Be(2);

               public class Passing_the_entitys_id_to : The_aggregates_Entities_collection
               {
                  [XFact] public void Contains_returns_true() => Aggregate.Entities.Contains(agEntity2.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity() => Aggregate.Entities.Get(agEntity2.Id).Should().Be(agEntity2);
                  [XFact] public void Indexer_returns_the_entity() => Aggregate.Entities[agEntity2.Id].Should().Be(agEntity2);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     Aggregate.Entities.TryGet(agEntity2.Id, out var agEntity2Fetched).Should().BeTrue();
                     agEntity2Fetched.Should().Be(agEntity2);
                  }
               }
            }

            public new class The_QueryModels_Entities_collection : After_adding_entity_named_entity2
            {
               [XFact] public void Single_throws() => Invoking(() => QueryModel.Entities.Single()).Should().Throw<Exception>();
               [XFact] public void InCreationOrder_1_returns_the_entity_query_model() => QueryModel.Entities.InCreationOrder[1].Should().Be(qmEntity2);
               [XFact] public void InCreationOrder_Count_is_2() => QueryModel.Entities.InCreationOrder.Count.Should().Be(2);

               public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
               {
                  [XFact] public void Contains_returns_true() => QueryModel.Entities.Contains(agEntity2.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity_query_model() => QueryModel.Entities.Get(agEntity2.Id).Should().Be(qmEntity2);
                  [XFact] public void Indexer_returns_the_entity() => QueryModel.Entities[agEntity2.Id].Should().Be(qmEntity2);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     QueryModel.Entities.TryGet(agEntity2.Id, out var qmEntity2Fetched).Should().BeTrue();
                     qmEntity2Fetched.Should().Be(qmEntity2);
                  }
               }
            }

            public class After_calling_rename_on_entity1_with_string_newName : After_adding_entity_named_entity2
            {
               public After_calling_rename_on_entity1_with_string_newName() => agEntity1.Rename("newName");
               [XFact] public void entity1_name_is_newName() => agEntity1.Name.Should().Be("newName");
               [XFact] public void QueryModel_entity1_name_is_newName() => qmEntity1.Name.Should().Be("newName");
               [XFact] public void entity2_name_remains_entity2() => agEntity2.Name.Should().Be("entity2");
               [XFact] public void QueryModel_entity2_name_remains_entity2() => qmEntity2.Name.Should().Be("entity2");

               public class After_calling_rename_on_entity2_with_string_newName2 : After_calling_rename_on_entity1_with_string_newName
               {
                  public After_calling_rename_on_entity2_with_string_newName2() => agEntity2.Rename("newName2");
                  [XFact] public void entity2_name_is_newName2() => agEntity2.Name.Should().Be("newName2");
                  [XFact] public void QueryModel_entity2_name_is_newName2() => qmEntity2.Name.Should().Be("newName2");
                  [XFact] public void entity1_name_remains_newName() => agEntity1.Name.Should().Be("newName");
                  [XFact] public void QueryModel_entity1_name_remains_newName() => qmEntity1.Name.Should().Be("newName");
               }
            }

            public class After_calling_entity2_Remove : After_adding_entity_named_entity2
            {
               public After_calling_entity2_Remove() => agEntity2.Remove();

               public new class The_aggregates_Entities_collection : After_calling_entity2_Remove
               {
                  [XFact] public void Single_return_entity1() => Aggregate.Entities.Single().Should().Be(agEntity1);
                  [XFact] public void InCreationOrder_1_throws() => Invoking(() => Aggregate.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => Aggregate.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_aggregates_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => Aggregate.Entities.Contains(agEntity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => Invoking(() => Aggregate.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => Invoking(() => Aggregate.Entities[agEntity2.Id]).Should().Throw<Exception>();

                     [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                     {
                        Aggregate.Entities.TryGet(agEntity2.Id, out var agEntity2Fetched).Should().BeFalse();
                        agEntity2Fetched.Should().Be(null);
                     }
                  }
               }

               public class The_QueryModel_Entities_collection : After_calling_entity2_Remove
               {
                  [XFact] public void Single_return_entity1() => QueryModel.Entities.Single().Should().Be(qmEntity1);
                  [XFact] public void InCreationOrder_1_throws() => Invoking(() => QueryModel.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => QueryModel.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => QueryModel.Entities.Contains(agEntity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => Invoking(() => QueryModel.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => Invoking(() => QueryModel.Entities[agEntity2.Id]).Should().Throw<Exception>();

                     [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                     {
                        QueryModel.Entities.TryGet(agEntity2.Id, out var qmEntity2Fetched).Should().BeFalse();
                        qmEntity2Fetched.Should().Be(null);
                     }
                  }
               }

               public class After_calling_entity1_Remove : After_calling_entity2_Remove
               {
                  public After_calling_entity1_Remove() => agEntity1.Remove();

                  public new class The_aggregates_Entities_collection : After_calling_entity1_Remove
                  {
                     [XFact] public void Single_throws() => Invoking(()  => Aggregate.Entities.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_0_throws() => Invoking(() => Aggregate.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_0() => Aggregate.Entities.InCreationOrder.Count.Should().Be(0);

                     public class Passing_the_entity1_id_to : The_aggregates_Entities_collection
                     {
                        [XFact] public void Contains_returns_false() => Aggregate.Entities.Contains(agEntity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => Aggregate.Entities.Get(agEntity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => Aggregate.Entities[agEntity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           Aggregate.Entities.TryGet(agEntity1.Id, out var agEntity1Fetched).Should().BeFalse();
                           agEntity1Fetched.Should().Be(null);
                        }
                     }
                  }

                  public new class The_QueryModel_Entities_collection : After_calling_entity1_Remove
                  {
                     [XFact] public void Single_throws() => Invoking(() => QueryModel.Entities.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_0_throws() => Invoking(() => QueryModel.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_0() => QueryModel.Entities.InCreationOrder.Count.Should().Be(0);

                     public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection
                     {
                        [XFact] public void Contains_returns_false() => QueryModel.Entities.Contains(agEntity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => QueryModel.Entities.Get(agEntity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => QueryModel.Entities[agEntity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           QueryModel.Entities.TryGet(agEntity1.Id, out var qmEntity1Fetched).Should().BeFalse();
                           qmEntity1Fetched.Should().Be(null);
                        }
                     }
                  }
               }
            }
         }
      }

      [XFact] public void Aggregate_Component_tests()
      {
         Aggregate.Component.Name.Should().BeNullOrEmpty();
         QueryModel.Component.Name.Should().BeNullOrEmpty();

         Aggregate.Component.Rename("newName");
         Aggregate.Component.Name.Should().Be("newName");
         QueryModel.Component.Name.Should().Be("newName");
      }

      [XFact] public void Aggregate_Component_Component_tests()
      {
         Aggregate.Component.CComponent.Name.Should().BeNullOrEmpty();
         QueryModel.Component.CComponent.Name.Should().BeNullOrEmpty();
         Aggregate.Component.CComponent.Rename("newName");
         Aggregate.Component.CComponent.Name.Should().Be("newName");
         QueryModel.Component.CComponent.Name.Should().Be("newName");
      }

      [XFact] public void Aggregate_Component_Entity_tests()
      {
         var agComponent = Aggregate.Component;
         var qmComponent = QueryModel.Component;

         var entity1Id = Guid.NewGuid();
         var agComponentEntity1 = agComponent.AddEntity("entity1", entity1Id);
         agComponent.Invoking(it => it.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

         var qmComponentEntity1 = qmComponent.Entities.InCreationOrder[0];

         qmComponentEntity1.Id.Should().Be(agComponentEntity1.Id).And.Be(entity1Id);
         agComponentEntity1.Name.Should().Be("entity1");
         qmComponentEntity1.Name.Should().Be("entity1");
         agComponent.Entities.InCreationOrder.Count.Should().Be(1);
         qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
         agComponent.Entities.Contains(agComponentEntity1.Id).Should().Be(true);
         qmComponent.Entities.Contains(agComponentEntity1.Id).Should().Be(true);
         agComponent.Entities.Get(agComponentEntity1.Id).Should().Be(agComponentEntity1);
         qmComponent.Entities.Get(agComponentEntity1.Id).Should().Be(qmComponentEntity1);
         agComponent.Entities[agComponentEntity1.Id].Should().Be(agComponentEntity1);
         qmComponent.Entities[agComponentEntity1.Id].Should().Be(qmComponentEntity1);

         var entity2Id = Guid.NewGuid();
         var agEntity2 = agComponent.AddEntity("entity2", entity2Id);
         agComponent.Invoking(it => it.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

         var qmEntity2 = qmComponent.Entities.InCreationOrder[1];
         agEntity2.Name.Should().Be("entity2");
         qmEntity2.Name.Should().Be("entity2");
         agComponent.Entities.InCreationOrder.Count.Should().Be(2);
         qmComponent.Entities.InCreationOrder.Count.Should().Be(2);
         agComponent.Entities.Contains(agEntity2.Id).Should().Be(true);
         qmComponent.Entities.Contains(agEntity2.Id).Should().Be(true);
         agComponent.Entities[agEntity2.Id].Should().Be(agEntity2);
         qmComponent.Entities[agEntity2.Id].Should().Be(qmEntity2);

         agComponentEntity1.Rename("newName");
         agComponentEntity1.Name.Should().Be("newName");
         qmComponentEntity1.Name.Should().Be("newName");
         agEntity2.Name.Should().Be("entity2");
         qmEntity2.Name.Should().Be("entity2");

         agEntity2.Rename("newName2");
         agEntity2.Name.Should().Be("newName2");
         qmEntity2.Name.Should().Be("newName2");
         agComponentEntity1.Name.Should().Be("newName");
         qmComponentEntity1.Name.Should().Be("newName");

         agComponent.Entities.InCreationOrder.Count.Should().Be(2);
         qmComponent.Entities.InCreationOrder.Count.Should().Be(2);

         agEntity2.Remove();
         agComponent.Entities.Contains(agEntity2.Id).Should().Be(false);
         qmComponent.Entities.Contains(agEntity2.Id).Should().Be(false);
         agComponent.Entities.InCreationOrder.Count.Should().Be(1);
         qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
         agComponent.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
         qmComponent.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
         agComponent.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();
         qmComponent.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();

         agComponentEntity1.Remove();
         agComponent.Entities.Contains(agComponentEntity1.Id).Should().Be(false);
         qmComponent.Entities.Contains(agComponentEntity1.Id).Should().Be(false);
         agComponent.Entities.InCreationOrder.Count.Should().Be(0);
         qmComponent.Entities.InCreationOrder.Count.Should().Be(0);
         agComponent.Invoking(it => it.Entities.Get(agComponentEntity1.Id)).Should().Throw<Exception>();
         qmComponent.Invoking(it => it.Entities.Get(agComponentEntity1.Id)).Should().Throw<Exception>();
         agComponent.Invoking(it => { _ = it.Entities[agComponentEntity1.Id]; }).Should().Throw<Exception>();
         qmComponent.Invoking(it => { _ = it.Entities[agComponentEntity1.Id]; }).Should().Throw<Exception>();
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
