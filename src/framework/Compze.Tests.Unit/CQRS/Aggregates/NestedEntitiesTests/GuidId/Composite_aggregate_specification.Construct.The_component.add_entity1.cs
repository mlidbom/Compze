using System;
using System.Linq;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component
      {
         public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid : The_component
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
         }
      }
   }
}

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component
      {}
   }
}
