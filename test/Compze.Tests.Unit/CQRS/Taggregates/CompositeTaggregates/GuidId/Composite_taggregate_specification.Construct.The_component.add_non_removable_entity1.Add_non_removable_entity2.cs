using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class The_component
      {
         public partial class After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid
         {
            public partial class After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid : After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid
            {
               readonly Guid _nonRemovableEntity2Id;
               readonly Component.NonRemovableEntity _nonRemovableEntity2;
               readonly QueryModels.Component.NonRemovableEntity _qmNonRemovableEntity2;

               public After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid()
               {
                  _nonRemovableEntity2Id = Guid.NewGuid();
                  _nonRemovableEntity2 = _component.AddNonRemovableEntity("entity2", _nonRemovableEntity2Id);
                  _qmNonRemovableEntity2 = _qmComponent.NonRemovableEntities.InCreationOrder[1];
               }

               [XF] public void Added_entity_is_named_entity2() => _nonRemovableEntity2.Name.Must().Be("entity2");
               [XF] public void Added_entity_has_the_supplied_id_() => _nonRemovableEntity2.Id.Must().Be(_nonRemovableEntity2Id);
               [XF] public void Added_entity_QueryModel_is_named_entity2() => _qmNonRemovableEntity2.Name.Must().Be("entity2");
               [XF] public void Added_entity_QueryModel_has_the_same_id_as_the_entity() => _qmNonRemovableEntity2.Id.Must().Be(_nonRemovableEntity2.Id);
               [XF] public void Invoking_AddNonRemovableEntity_with_a_new_name_but_the_same_id_throws_() => _component.Invoking(it => it.AddNonRemovableEntity("newEntityName", _nonRemovableEntity2.Id)).Must().Throw<Exception>();

               public class The_NonRemovableEntities_collection_ : After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid
               {
                  [XF] public void Single_throws() => _component.NonRemovableEntities.Invoking(it => it.Single()).Must().Throw<Exception>();
                  [XF] public void InCreationOrder_1_returns_the_entity() => _component.NonRemovableEntities.InCreationOrder[1].Must().Be(_nonRemovableEntity2);
                  [XF] public void InCreationOrder_Count_is_2() => _component.NonRemovableEntities.InCreationOrder.Count.Must().Be(2);

                  public class Passing_the_entitys_id_to : The_NonRemovableEntities_collection_
                  {
                     [XF] public void Contains_returns_true() => _component.NonRemovableEntities.Contains(_nonRemovableEntity2.Id).Must().Be(true);
                     [XF] public void Get_returns_the_entity() => _component.NonRemovableEntities.Get(_nonRemovableEntity2.Id).Must().Be(_nonRemovableEntity2);
                     [XF] public void Indexer_returns_the_entity() => _component.NonRemovableEntities[_nonRemovableEntity2.Id].Must().Be(_nonRemovableEntity2);

                     [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _component.NonRemovableEntities.TryGet(_nonRemovableEntity2.Id, out var entityFetched).Must().BeTrue();
                        entityFetched.Must().Be(_nonRemovableEntity2);
                     }
                  }
               }

               public class The_QueryModels_NonRemovableEntities_collection_ : After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid
               {
                  [XF] public void Single_throws() => _qmComponent.NonRemovableEntities.Invoking(it => it.Single()).Must().Throw<Exception>();
                  [XF] public void InCreationOrder_1_returns_the_entity_tuery_model() => _qmComponent.NonRemovableEntities.InCreationOrder[1].Must().Be(_qmNonRemovableEntity2);
                  [XF] public void InCreationOrder_Count_is_2() => _qmComponent.NonRemovableEntities.InCreationOrder.Count.Must().Be(2);

                  public class Passing_the_entitys_id_to : The_QueryModels_NonRemovableEntities_collection_
                  {
                     [XF] public void Contains_returns_true() => _qmComponent.NonRemovableEntities.Contains(_nonRemovableEntity2.Id).Must().Be(true);
                     [XF] public void Get_returns_the_entity_tuery_model() => _qmComponent.NonRemovableEntities.Get(_nonRemovableEntity2.Id).Must().Be(_qmNonRemovableEntity2);
                     [XF] public void Indexer_returns_the_entity() => _qmComponent.NonRemovableEntities[_nonRemovableEntity2.Id].Must().Be(_qmNonRemovableEntity2);

                     [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                     {
                        _qmComponent.NonRemovableEntities.TryGet(_nonRemovableEntity2.Id, out var qmEntityFetched).Must().BeTrue();
                        qmEntityFetched.Must().Be(_qmNonRemovableEntity2);
                     }
                  }
               }
            }
         }
      }
   }
}
