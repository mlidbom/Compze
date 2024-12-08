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
            public partial class After_calling_entity2_Remove
            {
               public class After_calling_entity1_Remove : After_calling_entity2_Remove
               {
                  public After_calling_entity1_Remove() => _entity1.Remove();

                  public new class The_aggregates_Entities_collection : After_calling_entity1_Remove
                  {
                     [XFact] public void Single_throws() => Invoking(()  => Aggregate.Entities.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_0_throws() => Invoking(() => Aggregate.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_0() => Aggregate.Entities.InCreationOrder.Count.Should().Be(0);

                     public class Passing_the_entity1_id_to : The_aggregates_Entities_collection
                     {
                        [XFact] public void Contains_returns_false() => Aggregate.Entities.Contains(_entity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => Aggregate.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => Aggregate.Entities[_entity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           Aggregate.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Should().BeFalse();
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
                        [XFact] public void Contains_returns_false() => QueryModel.Entities.Contains(_entity1.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => QueryModel.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => QueryModel.Entities[_entity1.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           QueryModel.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeFalse();
                           qmEntity1Fetched.Should().Be(null);
                        }
                     }
                  }
               }
            }
         }
      }
   }
}
