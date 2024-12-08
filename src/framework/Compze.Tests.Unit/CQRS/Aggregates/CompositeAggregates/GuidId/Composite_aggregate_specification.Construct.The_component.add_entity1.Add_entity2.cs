using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;
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
            public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid : After_calling_AddEntity_with_name_entity1_and_a_newGuid
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
            }
         }
      }
   }
}
