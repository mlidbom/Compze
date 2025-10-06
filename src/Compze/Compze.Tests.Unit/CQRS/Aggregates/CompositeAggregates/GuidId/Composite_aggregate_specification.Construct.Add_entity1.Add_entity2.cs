using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2 : After_adding_entity_named_entity1
         {
            readonly RemovableEntity _entity2;
            readonly Entity _qmEntity2;

            public After_adding_entity_named_entity2()
            {
               _entity2 = _aggregate.AddEntity("entity2");
               _qmEntity2 = _queryModel.Entities.InCreationOrder[1];
            }

            [XFact] public void The_name_of_the_added_entity_is_entity2() => _entity2.Name.Should().Be("entity2");
            [XFact] public void The_name_of_the_added_query_model_is_entity2() => _qmEntity2.Name.Should().Be("entity2");

            public class The_aggregates_Entities_collection_ : After_adding_entity_named_entity2
            {
               [XFact] public void Single_throws() => Invoking(() => _aggregate.Entities.Single()).Should().Throw<Exception>();
               [XFact] public void InCreationOrder_1_returns_the_entity() => _aggregate.Entities.InCreationOrder[1].Should().Be(_entity2);
               [XFact] public void InCreationOrder_Count_is_2() => _aggregate.Entities.InCreationOrder.Count.Should().Be(2);

               public class Passing_the_entitys_id_to : The_aggregates_Entities_collection_
               {
                  [XFact] public void Contains_returns_true() => _aggregate.Entities.Contains(_entity2.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity() => _aggregate.Entities.Get(_entity2.Id).Should().Be(_entity2);
                  [XFact] public void Indexer_returns_the_entity() => _aggregate.Entities[_entity2.Id].Should().Be(_entity2);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _aggregate.Entities.TryGet(_entity2.Id, out var agEntity2Fetched).Should().BeTrue();
                     agEntity2Fetched.Should().Be(_entity2);
                  }
               }
            }

            public class The_QueryModels_Entities_collection_ : After_adding_entity_named_entity2
            {
               [XFact] public void Single_throws() => Invoking(() => _queryModel.Entities.Single()).Should().Throw<Exception>();
               [XFact] public void InCreationOrder_1_returns_the_entity_query_model() => _queryModel.Entities.InCreationOrder[1].Should().Be(_qmEntity2);
               [XFact] public void InCreationOrder_Count_is_2() => _queryModel.Entities.InCreationOrder.Count.Should().Be(2);

               public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection_
               {
                  [XFact] public void Contains_returns_true() => _queryModel.Entities.Contains(_entity2.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity_query_model() => _queryModel.Entities.Get(_entity2.Id).Should().Be(_qmEntity2);
                  [XFact] public void Indexer_returns_the_entity() => _queryModel.Entities[_entity2.Id].Should().Be(_qmEntity2);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _queryModel.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeTrue();
                     qmEntity2Fetched.Should().Be(_qmEntity2);
                  }
               }
            }
         }
      }
   }
}
