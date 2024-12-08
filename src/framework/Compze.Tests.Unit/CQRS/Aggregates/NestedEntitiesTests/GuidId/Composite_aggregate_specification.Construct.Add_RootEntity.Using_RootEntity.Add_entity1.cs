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
            public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
            {
               readonly Guid _entity1Id;
               readonly RemovableEntity.RemovableNestedEntity _entity1;
               readonly Entity.RemovableNestedEntity _qmEntity1;

               public After_calling_AddEntity_with_name_entity1_and_a_newGuid()
               {
                  _entity1Id = Guid.NewGuid();
                  _entity1 = _rootEntity.AddEntity("entity1", _entity1Id);
                  _qmEntity1 = _qmRootEntity.Entities.InCreationOrder[0];
               }

               [XFact] public void Added_entity_is_named_entity1() => _entity1.Name.Should().Be("entity1");
               [XFact] public void Added_entity_has_the_supplied_id() => _entity1.Id.Should().Be(_entity1Id);
               [XFact] public void Invoking_AddEntity_with_a_new_name_but_the_same_id_throws() => _rootEntity.Invoking(it => it.AddEntity("newEntityName", _entity1.Id)).Should().Throw<Exception>();
               [XFact] public void QueryModel_has_a_single_entity_named_entity1() => _qmRootEntity.Entities.Single().Name.Should().Be("entity1");
               [XFact] public void QueryModels_single_entity_has_the_same_Id_as_the_entity() => _qmRootEntity.Entities.Single().Id.Should().Be(_entity1.Id);

               public class The_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
               {
                  [XFact] public void Single_returns_the_entity() => _rootEntity.Entities.Single().Should().Be(_entity1);
                  [XFact] public void InCreationOrder_0_returns_the_entity() => _rootEntity.Entities.InCreationOrder[0].Should().Be(_entity1);
                  [XFact] public void InCreationOrder_Count_is_1() => _rootEntity.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entitys_id_to : The_Entities_collection
                  {
                     [XFact] public void Contains_returns_true() => _rootEntity.Entities.Contains(_entity1.Id).Should().Be(true);
                     [XFact] public void Get_returns_the_entity() => _rootEntity.Entities.Get(_entity1.Id).Should().Be(_entity1);
                     [XFact] public void Indexer_returns_the_entity() => _rootEntity.Entities[_entity1.Id].Should().Be(_entity1);

                     [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _rootEntity.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Should().BeTrue();
                        agEntity1Fetched.Should().Be(_entity1);
                     }
                  }
               }

               public class The_QueryModels_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
               {
                  [XFact] public void Single_returns_the_entity_query_model() => _qmRootEntity.Entities.Single().Should().Be(_qmEntity1);
                  [XFact] public void InCreationOrder_0_returns_the_entity_query_model() => _qmRootEntity.Entities.InCreationOrder[0].Should().Be(_qmEntity1);
                  [XFact] public void InCreationOrder_Count_is_1() => _qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);

                  public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
                  {
                     [XFact] public void Contains_returns_true() => _qmRootEntity.Entities.Contains(_entity1.Id).Should().Be(true);
                     [XFact] public void Get_returns_the_entity_query_model() => _qmRootEntity.Entities.Get(_entity1.Id).Should().Be(_qmEntity1);
                     [XFact] public void Indexer_returns_the_entity() => _qmRootEntity.Entities[_entity1.Id].Should().Be(_qmEntity1);

                     [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _qmRootEntity.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeTrue();
                        qmEntity1Fetched.Should().Be(_qmEntity1);
                     }
                  }
               }
            }
         }
      }
   }
}
