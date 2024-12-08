using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

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
               public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid : After_calling_AddEntity_with_name_entity1_and_a_newGuid
               {
                  readonly Guid _entity2Id;
                  readonly RemovableEntity.RemovableNestedEntity _entity2;
                  readonly Entity.RemovableNestedEntity _qmEntity2;

                  public After_calling_AddEntity_with_name_entity2_and_a_newGuid()
                  {
                     _entity2Id = Guid.NewGuid();
                     _entity2 = _rootEntity.AddEntity("entity2", _entity2Id);
                     _qmEntity2 = _qmRootEntity.Entities.InCreationOrder[1];
                  }

                  [XFact] public void Added_entity_is_named_entity2() => _entity2.Name.Should().Be("entity2");
                  [XFact] public void Added_entity_has_the_supplied_id_() => _entity2.Id.Should().Be(_entity2Id);
                  [XFact] public void Added_entity_QueryModel_is_named_entity2() => _qmEntity2.Name.Should().Be("entity2");
                  [XFact] public void Added_entity_QueryModel_has_the_same_id_as_the_entity() => _qmEntity2.Id.Should().Be(_entity2.Id);
                  [XFact] public void Invoking_AddEntity_with_a_new_name_but_the_same_id_throws_() => _rootEntity.Invoking(it => it.AddEntity("newEntityName", _entity2.Id)).Should().Throw<Exception>();

                  public class The_Entities_collection_ : After_calling_AddEntity_with_name_entity2_and_a_newGuid
                  {
                     [XFact] public void Single_throws() => _rootEntity.Entities.Invoking(it => it.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_1_returns_the_entity() => _rootEntity.Entities.InCreationOrder[1].Should().Be(_entity2);
                     [XFact] public void InCreationOrder_Count_is_2() => _rootEntity.Entities.InCreationOrder.Count.Should().Be(2);

                     public class Passing_the_entitys_id_to : The_Entities_collection_
                     {
                        [XFact] public void Contains_returns_true() => _rootEntity.Entities.Contains(_entity2.Id).Should().Be(true);
                        [XFact] public void Get_returns_the_entity() => _rootEntity.Entities.Get(_entity2.Id).Should().Be(_entity2);
                        [XFact] public void Indexer_returns_the_entity() => _rootEntity.Entities[_entity2.Id].Should().Be(_entity2);

                        [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                        {
                           _rootEntity.Entities.TryGet(_entity2.Id, out var agEntity2Fetched).Should().BeTrue();
                           agEntity2Fetched.Should().Be(_entity2);
                        }
                     }
                  }

                  public class The_QueryModels_Entities_collection_ : After_calling_AddEntity_with_name_entity2_and_a_newGuid
                  {
                     [XFact] public void Single_throws() => _qmRootEntity.Entities.Invoking(it => it.Single()).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_1_returns_the_entity_query_model() => _qmRootEntity.Entities.InCreationOrder[1].Should().Be(_qmEntity2);
                     [XFact] public void InCreationOrder_Count_is_2() => _qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);

                     public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection_
                     {
                        [XFact] public void Contains_returns_true() => _qmRootEntity.Entities.Contains(_entity2.Id).Should().Be(true);
                        [XFact] public void Get_returns_the_entity_query_model() => _qmRootEntity.Entities.Get(_entity2.Id).Should().Be(_qmEntity2);
                        [XFact] public void Indexer_returns_the_entity() => _qmRootEntity.Entities[_entity2.Id].Should().Be(_qmEntity2);

                        [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                        {
                           _qmRootEntity.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeTrue();
                           qmEntity2Fetched.Should().Be(_qmEntity2);
                        }
                     }
                  }
               }
            }
         }
      }
   }
}
