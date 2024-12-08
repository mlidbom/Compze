﻿using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2
         {
            public partial class After_calling_entity2_Remove : After_adding_entity_named_entity2
            {
               public After_calling_entity2_Remove() => agEntity2.Remove();

               public new class The_aggregates_Entities_collection : After_calling_entity2_Remove
               {
                  [XFact] public void Single_return_entity1() => Aggregate.Entities.Single().Should().Be(agEntity1);
                  [XFact] public void InCreationOrder_1_throws() => FluentActions.Invoking(() => Aggregate.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => Aggregate.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_aggregates_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => Aggregate.Entities.Contains(agEntity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => FluentActions.Invoking(() => Aggregate.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => FluentActions.Invoking(() => Aggregate.Entities[agEntity2.Id]).Should().Throw<Exception>();

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
                  [XFact] public void InCreationOrder_1_throws() => FluentActions.Invoking(() => QueryModel.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => QueryModel.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => QueryModel.Entities.Contains(agEntity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => FluentActions.Invoking(() => QueryModel.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => FluentActions.Invoking(() => QueryModel.Entities[agEntity2.Id]).Should().Throw<Exception>();

                     [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                     {
                        QueryModel.Entities.TryGet(agEntity2.Id, out var qmEntity2Fetched).Should().BeFalse();
                        qmEntity2Fetched.Should().Be(null);
                     }
                  }
               }
            }
         }
      }
   }
}
