using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Must;
using Compze.xUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class The_component
      {
         public partial class After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid : The_component
         {
            readonly Guid _nonRemovableEntity1Id;
            readonly Component.NonRemovableEntity _nonRemovableEntity1;
            readonly QueryModels.Component.NonRemovableEntity _qmNonRemovableEntity1;

            public After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid()
            {
               _nonRemovableEntity1Id = Guid.NewGuid();
               _nonRemovableEntity1 = _component.AddNonRemovableEntity("entity1", _nonRemovableEntity1Id);
               _qmNonRemovableEntity1 = _qmComponent.NonRemovableEntities.InCreationOrder[0];
            }

            [XF] public void Added_entity_is_named_entity1() => _nonRemovableEntity1.Name.Must().Be("entity1");
            [XF] public void Added_entity_has_the_supplied_id() => _nonRemovableEntity1.Id.Must().Be(_nonRemovableEntity1Id);
            [XF] public void Invoking_AddNonRemovableEntity_with_a_new_name_but_the_same_id_throws() => _component.Invoking(it => it.AddNonRemovableEntity("newEntityName", _nonRemovableEntity1.Id)).Must().Throw<Exception>();
            [XF] public void QueryModel_has_a_single_entity_named_entity1() => _qmComponent.NonRemovableEntities.Single().Name.Must().Be("entity1");
            [XF] public void QueryModels_single_entity_has_the_same_Id_as_the_entity() => _qmComponent.NonRemovableEntities.Single().Id.Must().Be(_nonRemovableEntity1.Id);

            public class The_NonRemovableEntities_collection : After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid
            {
               [XF] public void Single_returns_the_entity() => _component.NonRemovableEntities.Single().Must().Be(_nonRemovableEntity1);
               [XF] public void InCreationOrder_0_returns_the_entity() => _component.NonRemovableEntities.InCreationOrder[0].Must().Be(_nonRemovableEntity1);
               [XF] public void InCreationOrder_Count_is_1() => _component.NonRemovableEntities.InCreationOrder.Count.Must().Be(1);

               public class Passing_the_entitys_id_to : The_NonRemovableEntities_collection
               {
                  [XF] public void Contains_returns_true() => _component.NonRemovableEntities.Contains(_nonRemovableEntity1.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity() => _component.NonRemovableEntities.Get(_nonRemovableEntity1.Id).Must().Be(_nonRemovableEntity1);
                  [XF] public void Indexer_returns_the_entity() => _component.NonRemovableEntities[_nonRemovableEntity1.Id].Must().Be(_nonRemovableEntity1);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _component.NonRemovableEntities.TryGet(_nonRemovableEntity1.Id, out var entityFetched).Must().BeTrue();
                     entityFetched.Must().Be(_nonRemovableEntity1);
                  }
               }
            }

            public class The_QueryModels_NonRemovableEntities_collection : After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid
            {
               [XF] public void Single_returns_the_entity_tuery_model() => _qmComponent.NonRemovableEntities.Single().Must().Be(_qmNonRemovableEntity1);
               [XF] public void InCreationOrder_0_returns_the_entity_tuery_model() => _qmComponent.NonRemovableEntities.InCreationOrder[0].Must().Be(_qmNonRemovableEntity1);
               [XF] public void InCreationOrder_Count_is_1() => _qmComponent.NonRemovableEntities.InCreationOrder.Count.Must().Be(1);

               public class Passing_the_entitys_id_to : The_QueryModels_NonRemovableEntities_collection
               {
                  [XF] public void Contains_returns_true() => _qmComponent.NonRemovableEntities.Contains(_nonRemovableEntity1.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity_tuery_model() => _qmComponent.NonRemovableEntities.Get(_nonRemovableEntity1.Id).Must().Be(_qmNonRemovableEntity1);
                  [XF] public void Indexer_returns_the_entity() => _qmComponent.NonRemovableEntities[_nonRemovableEntity1.Id].Must().Be(_qmNonRemovableEntity1);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _qmComponent.NonRemovableEntities.TryGet(_nonRemovableEntity1.Id, out var qmEntityFetched).Must().BeTrue();
                     qmEntityFetched.Must().Be(_qmNonRemovableEntity1);
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
      public partial class The_component;
   }
}
