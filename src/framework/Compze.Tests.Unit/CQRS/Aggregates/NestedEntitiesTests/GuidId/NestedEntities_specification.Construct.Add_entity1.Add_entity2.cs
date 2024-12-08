using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2 : After_adding_entity_named_entity1
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
               [XFact] public void Single_throws() => FluentActions.Invoking(() => Aggregate.Entities.Single()).Should().Throw<Exception>();
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
               [XFact] public void Single_throws() => FluentActions.Invoking(() => QueryModel.Entities.Single()).Should().Throw<Exception>();
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
         }
      }
   }
}
