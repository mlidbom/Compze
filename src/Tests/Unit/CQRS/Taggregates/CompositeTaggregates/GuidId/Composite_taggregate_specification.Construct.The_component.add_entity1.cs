using System;
using System.Linq;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
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

            [XF] public void Added_entity_is_named_entity1() => _entity1.Name.Must().Be("entity1");
            [XF] public void Added_entity_has_the_supplied_id() => _entity1.Id.Must().Be(_entity1Id);
            [XF] public void Invoking_AddEntity_with_a_new_name_but_the_same_id_throws() => _component.Invoking(it => it.AddEntity("newEntityName", _entity1.Id)).Must().Throw<Exception>();
            [XF] public void QueryModel_has_a_single_entity_named_entity1() => _qmComponent.Entities.Single().Name.Must().Be("entity1");
            [XF] public void QueryModels_single_entity_has_the_same_Id_as_the_entity() => _qmComponent.Entities.Single().Id.Must().Be(_entity1.Id);

            public class The_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               [XF] public void Single_returns_the_entity() => _component.Entities.Single().Must().Be(_entity1);
               [XF] public void InCreationOrder_0_returns_the_entity() => _component.Entities.InCreationOrder[0].Must().Be(_entity1);
               [XF] public void InCreationOrder_Count_is_1() => _component.Entities.InCreationOrder.Count.Must().Be(1);

               public class Passing_the_entitys_id_to : The_Entities_collection
               {
                  [XF] public void Contains_returns_true() => _component.Entities.Contains(_entity1.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity() => _component.Entities.Get(_entity1.Id).Must().Be(_entity1);
                  [XF] public void Indexer_returns_the_entity() => _component.Entities[_entity1.Id].Must().Be(_entity1);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _component.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Must().BeTrue();
                     agEntity1Fetched.Must().Be(_entity1);
                  }
               }
            }

            public class The_QueryModels_Entities_collection : After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               [XF] public void Single_returns_the_entity_tuery_model() => _qmComponent.Entities.Single().Must().Be(_qmEntity1);
               [XF] public void InCreationOrder_0_returns_the_entity_tuery_model() => _qmComponent.Entities.InCreationOrder[0].Must().Be(_qmEntity1);
               [XF] public void InCreationOrder_Count_is_1() => _qmComponent.Entities.InCreationOrder.Count.Must().Be(1);

               public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
               {
                  [XF] public void Contains_returns_true() => _qmComponent.Entities.Contains(_entity1.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity_tuery_model() => _qmComponent.Entities.Get(_entity1.Id).Must().Be(_qmEntity1);
                  [XF] public void Indexer_returns_the_entity() => _qmComponent.Entities[_entity1.Id].Must().Be(_qmEntity1);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _qmComponent.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Must().BeTrue();
                     qmEntity1Fetched.Must().Be(_qmEntity1);
                  }
               }
            }
         }
      }
   }
}

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class The_component
      {}
   }
}
