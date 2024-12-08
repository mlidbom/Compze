using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2
         {
            public partial class After_calling_entity2_Remove : After_adding_entity_named_entity2
            {
               public After_calling_entity2_Remove() => _entity2.Remove();

               public new class The_aggregates_Entities_collection : After_calling_entity2_Remove
               {
                  [XFact] public void Single_return_entity1() => _aggregate.Entities.Single().Should().Be(_entity1);
                  [XFact] public void InCreationOrder_1_throws() => Invoking(() => _aggregate.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => _aggregate.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_aggregates_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => _aggregate.Entities.Contains(_entity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => Invoking(() => _aggregate.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => Invoking(() => _aggregate.Entities[_entity2.Id]).Should().Throw<Exception>();

                     [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                     {
                        _aggregate.Entities.TryGet(_entity2.Id, out var agEntity2Fetched).Should().BeFalse();
                        agEntity2Fetched.Should().Be(null);
                     }
                  }
               }

               public class The_QueryModel_Entities_collection : After_calling_entity2_Remove
               {
                  [XFact] public void Single_return_entity1() => _queryModel.Entities.Single().Should().Be(_qmEntity1);
                  [XFact] public void InCreationOrder_1_throws() => Invoking(() => _queryModel.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_Count_is_1() => _queryModel.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                  {
                     [XFact] public void Contains_returns_false() => _queryModel.Entities.Contains(_entity2.Id).Should().Be(false);
                     [XFact] public void Get_throws() => Invoking(() => _queryModel.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                     [XFact] public void Indexer_throws() => Invoking(() => _queryModel.Entities[_entity2.Id]).Should().Throw<Exception>();

                     [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                     {
                        _queryModel.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeFalse();
                        qmEntity2Fetched.Should().Be(null);
                     }
                  }
               }
            }
         }
      }
   }
}
