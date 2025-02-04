﻿using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1 : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         readonly RemovableEntity _entity1;
         readonly Entity _qmEntity1;

         public After_adding_entity_named_entity1()
         {
            _entity1 = _aggregate.AddEntity("entity1");
            _qmEntity1 = _queryModel.Entities.Single();
         }

         [XFact] public void Added_entity_is_named_entity1() => _entity1.Name.Should().Be("entity1");
         [XFact] public void QueryModel_has_a_single_entity_named_entity1() => _qmEntity1.Name.Should().Be("entity1");
         [XFact] public void QueryModels_single_entity_has_the_correct_id() => _qmEntity1.Id.Should().Be(_entity1.Id);

         public class The_aggregates_Entities_collection : After_adding_entity_named_entity1
         {
            [XFact] public void Single_returns_the_entity() => _aggregate.Entities.Single().Should().Be(_entity1);
            [XFact] public void InCreationOrder_0_returns_the_entity() => _aggregate.Entities.InCreationOrder[0].Should().Be(_entity1);
            [XFact] public void InCreationOrder_Count_is_1() => _aggregate.Entities.InCreationOrder.Count.Should().Be(1);

            public class Passing_the_entitys_id_to : The_aggregates_Entities_collection
            {
               [XFact] public void Contains_returns_true() => _aggregate.Entities.Contains(_entity1.Id).Should().Be(true);
               [XFact] public void Get_returns_the_entity() => _aggregate.Entities.Get(_entity1.Id).Should().Be(_entity1);
               [XFact] public void Indexer_returns_the_entity() => _aggregate.Entities[_entity1.Id].Should().Be(_entity1);

               [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  _aggregate.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Should().BeTrue();
                  agEntity1Fetched.Should().Be(_entity1);
               }
            }
         }

         public class The_QueryModels_Entities_collection : After_adding_entity_named_entity1
         {
            [XFact] public void Single_returns_the_entity_query_model() => _queryModel.Entities.Single().Should().Be(_qmEntity1);
            [XFact] public void InCreationOrder_0_returns_the_entity_query_model() => _queryModel.Entities.InCreationOrder[0].Should().Be(_qmEntity1);
            [XFact] public void InCreationOrder_Count_is_1() => _queryModel.Entities.InCreationOrder.Count.Should().Be(1);

            public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
            {
               [XFact] public void Contains_returns_true() => _queryModel.Entities.Contains(_entity1.Id).Should().Be(true);
               [XFact] public void Get_returns_the_entity_query_model() => _queryModel.Entities.Get(_entity1.Id).Should().Be(_qmEntity1);
               [XFact] public void Indexer_returns_the_entity() => _queryModel.Entities[_entity1.Id].Should().Be(_qmEntity1);

               [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  _queryModel.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeTrue();
                  qmEntity1Fetched.Should().Be(_qmEntity1);
               }
            }
         }
      }
   }
}
