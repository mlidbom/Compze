using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component
      {
         public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid
         {
            public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid
            {
               public partial class After_calling_entity2_Remove : After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public After_calling_entity2_Remove() => _entity2.Remove();

                  public class The_Entities_collection__ : After_calling_entity2_Remove
                  {
                     [XFact] public void Single_return_entity1() => _component.Entities.Single().Should().Be(_entity1);
                     [XFact] public void InCreationOrder_1_throws() => FluentActions.Invoking(() => _component.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_1() => _component.Entities.InCreationOrder.Count.Should().Be(1);

                     public class Passing_the_entity2_id_to : The_Entities_collection__
                     {
                        [XFact] public void Contains_returns_false() => _component.Entities.Contains(_entity2.Id).Should().Be(false);
                        [XFact] public void Get_throws() => FluentActions.Invoking(() => _component.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => FluentActions.Invoking(() => _component.Entities[_entity2.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           _component.Entities.TryGet(_entity2.Id, out var entity2Fetched).Should().BeFalse();
                           entity2Fetched.Should().Be(null);
                        }
                     }
                  }

                  public class The_QueryModel_Entities_collection : After_calling_entity2_Remove
                  {
                     [XFact] public void Single_return_entity1() => _qmComponent.Entities.Single().Should().Be(_qmEntity1);
                     [XFact] public void InCreationOrder_1_throws() => FluentActions.Invoking(() => _qmComponent.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_1() => _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);

                     public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                     {
                        [XFact] public void Contains_returns_false() => _qmComponent.Entities.Contains(_entity2.Id).Should().Be(false);
                        [XFact] public void Get_throws() => FluentActions.Invoking(() => _qmComponent.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => FluentActions.Invoking(() => _qmComponent.Entities[_entity2.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           _qmComponent.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeFalse();
                           qmEntity2Fetched.Should().Be(null);
                        }
                     }
                  }
               }
            }
         }
      }
   }
}
