using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
      {
         public partial class Using_RootEntity
         {
            public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public partial class After_calling_entity2_Remove
                  {
                     public class After_calling_entity1_Remove : After_calling_entity2_Remove
                     {
                        public After_calling_entity1_Remove() => _entity1.Remove();

                        public class The_Entities_collection___ : After_calling_entity1_Remove
                        {
                           [XFact] public void Single_throws() => FluentActions.Invoking(() => _rootEntity.Entities.Single()).Should().Throw<Exception>();
                           [XFact] public void InCreationOrder_0_throws() => FluentActions.Invoking(() => _rootEntity.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                           [XFact] public void InCreationOrder_Count_is_0() => _rootEntity.Entities.InCreationOrder.Count.Should().Be(0);

                           public class Passing_the_entity1_id_to : The_Entities_collection___
                           {
                              [XFact] public void Contains_returns_false() => _rootEntity.Entities.Contains(_entity1.Id).Should().Be(false);
                              [XFact] public void Get_throws() => FluentActions.Invoking(() => _rootEntity.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                              [XFact] public void Indexer_throws() => FluentActions.Invoking(() => _rootEntity.Entities[_entity1.Id]).Should().Throw<Exception>();

                              [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                              {
                                 _rootEntity.Entities.TryGet(_entity1.Id, out var entity1Fetched).Should().BeFalse();
                                 entity1Fetched.Should().Be(null);
                              }
                           }
                        }

                        public class The_QueryModel_Entities_collection_ : After_calling_entity1_Remove
                        {
                           [XFact] public void Single_throws() => FluentActions.Invoking(() => _qmRootEntity.Entities.Single()).Should().Throw<Exception>();
                           [XFact] public void InCreationOrder_0_throws() => FluentActions.Invoking(() => _qmRootEntity.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                           [XFact] public void InCreationOrder_Count_is_0() => _qmRootEntity.Entities.InCreationOrder.Count.Should().Be(0);

                           public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection_
                           {
                              [XFact] public void Contains_returns_false() => _qmRootEntity.Entities.Contains(_entity1.Id).Should().Be(false);
                              [XFact] public void Get_throws() => FluentActions.Invoking(() => _qmRootEntity.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                              [XFact] public void Indexer_throws() => FluentActions.Invoking(() => _qmRootEntity.Entities[_entity1.Id]).Should().Throw<Exception>();

                              [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                              {
                                 _qmRootEntity.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeFalse();
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
   }
}
