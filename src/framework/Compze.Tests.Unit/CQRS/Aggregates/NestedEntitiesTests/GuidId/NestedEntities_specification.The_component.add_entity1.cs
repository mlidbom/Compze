using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component
      {
         public class After_calling_AddEntity_with_name_entity1_and_a_newGuid : The_component
         {
            readonly Guid _entity1Id;
            readonly Component.Entity _entity1;
            readonly QueryModels.Component.Entity _qmEntity1;

            public After_calling_AddEntity_with_name_entity1_and_a_newGuid()
            {
               _entity1Id = Guid.NewGuid();
               _entity1 = _component.AddEntity("entity1", _entity1Id);
               _qmEntity1 = _qmComponent.Entities.InCreationOrder[0];
            }

            [XFact] public void Added_entity_is_named_entity1() => _entity1.Name.Should().Be("entity1");
            [XFact] public void Added_entity_has_the_supplied_id() => _entity1.Id.Should().Be(_entity1Id);
            [XFact] public void Invoking_AddEntity_with_a_new_name_but_the_same_id_throws() => _component.Invoking(it => it.AddEntity("newEntityName", _entity1.Id)).Should().Throw<Exception>();
            [XFact] public void QueryModel_has_a_single_entity_named_entity1() => _qmComponent.Entities.Single().Name.Should().Be("entity1");
            [XFact] public void QueryModels_single_entity_has_the_same_Id_as_the_entity() => _qmComponent.Entities.Single().Id.Should().Be(_entity1.Id);

            public class The_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               [XFact] public void Single_returns_the_entity() => _component.Entities.Single().Should().Be(_entity1);
               [XFact] public void InCreationOrder_0_returns_the_entity() => _component.Entities.InCreationOrder[0].Should().Be(_entity1);
               [XFact] public void InCreationOrder_Count_is_1() => _component.Entities.InCreationOrder.Count.Should().Be(1);

               public class Passing_the_entitys_id_to : The_Entities_collection
               {
                  [XFact] public void Contains_returns_true() => _component.Entities.Contains(_entity1.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity() => _component.Entities.Get(_entity1.Id).Should().Be(_entity1);
                  [XFact] public void Indexer_returns_the_entity() => _component.Entities[_entity1.Id].Should().Be(_entity1);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _component.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Should().BeTrue();
                     agEntity1Fetched.Should().Be(_entity1);
                  }
               }
            }

            public class The_QueryModels_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               [XFact] public void Single_returns_the_entity_query_model() => _qmComponent.Entities.Single().Should().Be(_qmEntity1);
               [XFact] public void InCreationOrder_0_returns_the_entity_query_model() => _qmComponent.Entities.InCreationOrder[0].Should().Be(_qmEntity1);
               [XFact] public void InCreationOrder_Count_is_1() => _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);

               public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
               {
                  [XFact] public void Contains_returns_true() => _qmComponent.Entities.Contains(_entity1.Id).Should().Be(true);
                  [XFact] public void Get_returns_the_entity_query_model() => _qmComponent.Entities.Get(_entity1.Id).Should().Be(_qmEntity1);
                  [XFact] public void Indexer_returns_the_entity() => _qmComponent.Entities[_entity1.Id].Should().Be(_qmEntity1);

                  [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _qmComponent.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeTrue();
                     qmEntity1Fetched.Should().Be(_qmEntity1);
                  }
               }
            }

            public class After_calling_AddEntity_with_name_entity2_and_a_newGuid : After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               readonly Guid _entity2Id;
               readonly Component.Entity _entity2;
               readonly QueryModels.Component.Entity _qmEntity2;

               public After_calling_AddEntity_with_name_entity2_and_a_newGuid()
               {
                  _entity2Id = Guid.NewGuid();
                  _entity2 = _component.AddEntity("entity2", _entity2Id);
                  _qmEntity2 = _qmComponent.Entities.InCreationOrder[1];
               }

               [XFact] public void Added_entity_is_named_entity2() => _entity2.Name.Should().Be("entity2");
               [XFact] public void Added_entity_has_the_supplied_id_() => _entity2.Id.Should().Be(_entity2Id);
               [XFact] public void Added_entity_QueryModel_is_named_entity2() => _qmEntity2.Name.Should().Be("entity2");
               [XFact] public void Added_entity_QueryModel_has_the_same_id_as_the_entity() => _qmEntity2.Id.Should().Be(_entity2.Id);
               [XFact] public void Invoking_AddEntity_with_a_new_name_but_the_same_id_throws_() => _component.Invoking(it => it.AddEntity("newEntityName", _entity2.Id)).Should().Throw<Exception>();

               public class The_Entities_collection_ : After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  [XFact] public void Single_throws() => _component.Entities.Invoking(it => it.Single()).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_1_returns_the_entity() => _component.Entities.InCreationOrder[1].Should().Be(_entity2);
                  [XFact] public void InCreationOrder_Count_is_2() => _component.Entities.InCreationOrder.Count.Should().Be(2);

                  public class Passing_the_entitys_id_to : The_Entities_collection_
                  {
                     [XFact] public void Contains_returns_true() => _component.Entities.Contains(_entity2.Id).Should().Be(true);
                     [XFact] public void Get_returns_the_entity() => _component.Entities.Get(_entity2.Id).Should().Be(_entity2);
                     [XFact] public void Indexer_returns_the_entity() => _component.Entities[_entity2.Id].Should().Be(_entity2);

                     [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _component.Entities.TryGet(_entity2.Id, out var agEntity2Fetched).Should().BeTrue();
                        agEntity2Fetched.Should().Be(_entity2);
                     }
                  }
               }

               public class The_QueryModels_Entities_collection_ : After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  [XFact] public void Single_throws() => _qmComponent.Entities.Invoking(it => it.Single()).Should().Throw<Exception>();
                  [XFact] public void InCreationOrder_1_returns_the_entity_query_model() => _qmComponent.Entities.InCreationOrder[1].Should().Be(_qmEntity2);
                  [XFact] public void InCreationOrder_Count_is_2() => _qmComponent.Entities.InCreationOrder.Count.Should().Be(2);

                  public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection_
                  {
                     [XFact] public void Contains_returns_true() => _qmComponent.Entities.Contains(_entity2.Id).Should().Be(true);
                     [XFact] public void Get_returns_the_entity_query_model() => _qmComponent.Entities.Get(_entity2.Id).Should().Be(_qmEntity2);
                     [XFact] public void Indexer_returns_the_entity() => _qmComponent.Entities[_entity2.Id].Should().Be(_qmEntity2);

                     [XFact] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _qmComponent.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeTrue();
                        qmEntity2Fetched.Should().Be(_qmEntity2);
                     }
                  }
               }

               public class After_calling_rename_on_entity1_with_string_newName : After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public After_calling_rename_on_entity1_with_string_newName() => _entity1.Rename("newName");
                  [XFact] public void entity1_name_is_newName() => _entity1.Name.Should().Be("newName");
                  [XFact] public void QueryModel_entity1_name_is_newName() => _qmEntity1.Name.Should().Be("newName");
                  [XFact] public void entity2_name_remains_entity2() => _entity2.Name.Should().Be("entity2");
                  [XFact] public void QueryModel_entity2_name_remains_entity2() => _qmEntity2.Name.Should().Be("entity2");

                  public class After_calling_rename_on_entity2_with_string_newName2 : After_calling_rename_on_entity1_with_string_newName
                  {
                     public After_calling_rename_on_entity2_with_string_newName2() => _entity2.Rename("newName2");
                     [XFact] public void entity2_name_is_newName2() => _entity2.Name.Should().Be("newName2");
                     [XFact] public void QueryModel_entity2_name_is_newName2() => _qmEntity2.Name.Should().Be("newName2");
                     [XFact] public void entity1_name_remains_newName() => _entity1.Name.Should().Be("newName");
                     [XFact] public void QueryModel_entity1_name_remains_newName() => _qmEntity1.Name.Should().Be("newName");
                  }
               }
            }
         }

         [XFact] public void Aggregate_Component_Entity_tests()
         {
            var _component = Aggregate.Component;
            var _qmComponent = QueryModel.Component;

            var _entity1Id = Guid.NewGuid();
            var _entity1 = _component.AddEntity("entity1", _entity1Id);

            var entity2Id = Guid.NewGuid();
            var agEntity2 = _component.AddEntity("entity2", entity2Id);
            _component.Invoking(it => it.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

            agEntity2.Remove();
            _component.Entities.Contains(agEntity2.Id).Should().Be(false);
            _qmComponent.Entities.Contains(agEntity2.Id).Should().Be(false);
            _component.Entities.InCreationOrder.Count.Should().Be(1);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);
            _component.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            _qmComponent.Invoking(it => it.Entities.Get(agEntity2.Id)).Should().Throw<Exception>();
            _component.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();
            _qmComponent.Invoking(it => { _ = it.Entities[agEntity2.Id]; }).Should().Throw<Exception>();

            _entity1.Remove();
            _component.Entities.Contains(_entity1.Id).Should().Be(false);
            _qmComponent.Entities.Contains(_entity1.Id).Should().Be(false);
            _component.Entities.InCreationOrder.Count.Should().Be(0);
            _qmComponent.Entities.InCreationOrder.Count.Should().Be(0);
            _component.Invoking(it => it.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
            _qmComponent.Invoking(it => it.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
            _component.Invoking(it => { _ = it.Entities[_entity1.Id]; }).Should().Throw<Exception>();
            _qmComponent.Invoking(it => { _ = it.Entities[_entity1.Id]; }).Should().Throw<Exception>();
         }
      }
   }
}
