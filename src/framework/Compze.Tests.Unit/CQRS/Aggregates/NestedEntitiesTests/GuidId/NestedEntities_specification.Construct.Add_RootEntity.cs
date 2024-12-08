using System;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;
using System.Linq;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         readonly Component _component;
         readonly QueryModels.Component _qmComponent;

         public After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid()
         {
            _component = Aggregate.Component;
            _qmComponent = QueryModel.Component;
         }

         [XFact] public void Component_Name_is_empty_string() => _component.Name.Should().Be("");
         [XFact] public void QueryModel_Name_is_empty_string() => _qmComponent.Name.Should().Be("");

         public class After_calling_rename_with_string_newName : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
         {
            public After_calling_rename_with_string_newName() => _component.Rename("newName");

            [XFact] public void Component_Name_is_newName() => _component.Name.Should().Be("newName");
            [XFact] public void QueryModel_Name_is_newName() => _qmComponent.Name.Should().Be("newName");
         }

         public class After_calling_AddEntity_with_name_entity1_and_a_newGuid : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
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

               public class After_calling_entity2_Remove : After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public After_calling_entity2_Remove() => _entity2.Remove();

                  public class The_Entities_collection__ : After_calling_entity2_Remove
                  {
                     [XFact] public void Single_return_entity1() => _component.Entities.Single().Should().Be(_entity1);
                     [XFact] public void InCreationOrder_1_throws() => Invoking(() => _component.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_1() => _component.Entities.InCreationOrder.Count.Should().Be(1);

                     public class Passing_the_entity2_id_to : The_Entities_collection__
                     {
                        [XFact] public void Contains_returns_false() => _component.Entities.Contains(_entity2.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => _component.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => _component.Entities[_entity2.Id]).Should().Throw<Exception>();

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
                     [XFact] public void InCreationOrder_1_throws() => Invoking(() => _qmComponent.Entities.InCreationOrder[1]).Should().Throw<Exception>();
                     [XFact] public void InCreationOrder_Count_is_1() => _qmComponent.Entities.InCreationOrder.Count.Should().Be(1);

                     public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                     {
                        [XFact] public void Contains_returns_false() => _qmComponent.Entities.Contains(_entity2.Id).Should().Be(false);
                        [XFact] public void Get_throws() => Invoking(() => _qmComponent.Entities.Get(_entity2.Id)).Should().Throw<Exception>();
                        [XFact] public void Indexer_throws() => Invoking(() => _qmComponent.Entities[_entity2.Id]).Should().Throw<Exception>();

                        [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                        {
                           _qmComponent.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Should().BeFalse();
                           qmEntity2Fetched.Should().Be(null);
                        }
                     }
                  }

                  public class After_calling_entity1_Remove : After_calling_entity2_Remove
                  {
                     public After_calling_entity1_Remove() => _entity1.Remove();

                     public class The_Entities_collection___ : After_calling_entity1_Remove
                     {
                        [XFact] public void Single_throws() => Invoking(() => _component.Entities.Single()).Should().Throw<Exception>();
                        [XFact] public void InCreationOrder_0_throws() => Invoking(() => _component.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                        [XFact] public void InCreationOrder_Count_is_0() => _component.Entities.InCreationOrder.Count.Should().Be(0);

                        public class Passing_the_entity1_id_to : The_Entities_collection___
                        {
                           [XFact] public void Contains_returns_false() => _component.Entities.Contains(_entity1.Id).Should().Be(false);
                           [XFact] public void Get_throws() => Invoking(() => _component.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                           [XFact] public void Indexer_throws() => Invoking(() => _component.Entities[_entity1.Id]).Should().Throw<Exception>();

                           [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _component.Entities.TryGet(_entity1.Id, out var entity1Fetched).Should().BeFalse();
                              entity1Fetched.Should().Be(null);
                           }
                        }
                     }

                     public class The_QueryModel_Entities_collection_ : After_calling_entity1_Remove
                     {
                        [XFact] public void Single_throws() => Invoking(() => _qmComponent.Entities.Single()).Should().Throw<Exception>();
                        [XFact] public void InCreationOrder_0_throws() => Invoking(() => _qmComponent.Entities.InCreationOrder[0]).Should().Throw<Exception>();
                        [XFact] public void InCreationOrder_Count_is_0() => _qmComponent.Entities.InCreationOrder.Count.Should().Be(0);

                        public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection_
                        {
                           [XFact] public void Contains_returns_false() => _qmComponent.Entities.Contains(_entity1.Id).Should().Be(false);
                           [XFact] public void Get_throws() => Invoking(() => _qmComponent.Entities.Get(_entity1.Id)).Should().Throw<Exception>();
                           [XFact] public void Indexer_throws() => Invoking(() => _qmComponent.Entities[_entity1.Id]).Should().Throw<Exception>();

                           [XFact] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _qmComponent.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Should().BeFalse();
                              qmEntity1Fetched.Should().Be(null);
                           }
                        }
                     }
                  }
               }
            }
         }
      }
      
      [XFact] public void Aggregate_Entity_Entity_tests()
         {
            var agRootEntity = Aggregate.AddEntity("RootEntity");
            var qmRootEntity = QueryModel.Entities.InCreationOrder[0];

            var entity1Id = Guid.NewGuid();
            var agNestedEntity1 = agRootEntity.AddEntity("entity1", entity1Id);
            var qmNestedEntity1 = qmRootEntity.Entities.InCreationOrder[0];

            agRootEntity.Invoking(it => it.AddEntity("entity2", entity1Id)).Should().Throw<Exception>();

            agNestedEntity1.Id.Should().Be(entity1Id);
            qmNestedEntity1.Id.Should().Be(entity1Id);
            agNestedEntity1.Name.Should().Be("entity1");
            qmNestedEntity1.Name.Should().Be("entity1");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(true);
            qmRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(true);
            agRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(agNestedEntity1);
            qmRootEntity.Entities.Get(agNestedEntity1.Id).Should().Be(qmNestedEntity1);
            agRootEntity.Entities[agNestedEntity1.Id].Should().Be(agNestedEntity1);
            qmRootEntity.Entities[agNestedEntity1.Id].Should().Be(qmNestedEntity1);

            var entity2Id = Guid.NewGuid();
            var agNestedEntity2 = agRootEntity.AddEntity("entity2", entity2Id);
            var qmNestedEntity2 = qmRootEntity.Entities.InCreationOrder[1];
            agRootEntity.Invoking(it => it.AddEntity("entity3", entity2Id)).Should().Throw<Exception>();

            agNestedEntity2.Id.Should().Be(entity2Id);
            qmNestedEntity2.Id.Should().Be(entity2Id);
            agNestedEntity2.Name.Should().Be("entity2");
            qmNestedEntity2.Name.Should().Be("entity2");
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            agRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(true);
            qmRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(true);
            agRootEntity.Entities[agNestedEntity2.Id].Should().Be(agNestedEntity2);
            qmRootEntity.Entities[agNestedEntity2.Id].Should().Be(qmNestedEntity2);

            agNestedEntity1.Rename("newName");
            agNestedEntity1.Name.Should().Be("newName");
            qmNestedEntity1.Name.Should().Be("newName");
            agNestedEntity2.Name.Should().Be("entity2");
            qmNestedEntity2.Name.Should().Be("entity2");

            agNestedEntity2.Rename("newName2");
            agNestedEntity2.Name.Should().Be("newName2");
            qmNestedEntity2.Name.Should().Be("newName2");
            agNestedEntity1.Name.Should().Be("newName");
            qmNestedEntity1.Name.Should().Be("newName");

            agRootEntity.Entities.InCreationOrder.Count.Should().Be(2);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(2);

            agNestedEntity2.Remove();
            agRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(false);
            qmRootEntity.Entities.Contains(agNestedEntity2.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(1);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity2.Id)).Should().Throw<Exception>();
            agRootEntity.Invoking(_ =>
            {
               var __ = agRootEntity.Entities[agNestedEntity2.Id];
            }).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ =>
            {
               var __ = agRootEntity.Entities[agNestedEntity2.Id];
            }).Should().Throw<Exception>();

            agNestedEntity1.Remove();
            agRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(false);
            qmRootEntity.Entities.Contains(agNestedEntity1.Id).Should().Be(false);
            agRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
            qmRootEntity.Entities.InCreationOrder.Count.Should().Be(0);
            agRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ => agRootEntity.Entities.Get(agNestedEntity1.Id)).Should().Throw<Exception>();
            agRootEntity.Invoking(_ =>
            {
               var __ = agRootEntity.Entities[agNestedEntity1.Id];
            }).Should().Throw<Exception>();
            qmRootEntity.Invoking(_ =>
            {
               var __ = agRootEntity.Entities[agNestedEntity1.Id];
            }).Should().Throw<Exception>();
         }
   }
}
