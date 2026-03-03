using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_adding_entity_named_entity1 : After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
      {
         readonly RemovableEntity _entity1;
         readonly Entity _qmEntity1;

         public After_adding_entity_named_entity1()
         {
            _entity1 = _taggregate.AddEntity("entity1");
            _qmEntity1 = _queryModel.Entities.Single();
         }

         [XF] public void Added_entity_is_named_entity1() => _entity1.Name.Must().Be("entity1");
         [XF] public void QueryModel_has_a_single_entity_named_entity1() => _qmEntity1.Name.Must().Be("entity1");
         [XF] public void QueryModels_single_entity_has_the_correct_id() => _qmEntity1.Id.Must().Be(_entity1.Id);

         public class The_taggregates_Entities_collection : After_adding_entity_named_entity1
         {
            [XF] public void Single_returns_the_entity() => _taggregate.Entities.Single().Must().Be(_entity1);
            [XF] public void InCreationOrder_0_returns_the_entity() => _taggregate.Entities.InCreationOrder[0].Must().Be(_entity1);
            [XF] public void InCreationOrder_Count_is_1() => _taggregate.Entities.InCreationOrder.Count.Must().Be(1);

            public class Passing_the_entitys_id_to : The_taggregates_Entities_collection
            {
               [XF] public void Contains_returns_true() => _taggregate.Entities.Contains(_entity1.Id).Must().Be(true);
               [XF] public void Get_returns_the_entity() => _taggregate.Entities.Get(_entity1.Id).Must().Be(_entity1);
               [XF] public void Indexer_returns_the_entity() => _taggregate.Entities[_entity1.Id].Must().Be(_entity1);

               [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  _taggregate.Entities.TryGet(_entity1.Id, out var agEntity1Fetched).Must().BeTrue();
                  agEntity1Fetched.Must().Be(_entity1);
               }
            }
         }

         public class The_QueryModels_Entities_collection : After_adding_entity_named_entity1
         {
            [XF] public void Single_returns_the_entity_tuery_model() => _queryModel.Entities.Single().Must().Be(_qmEntity1);
            [XF] public void InCreationOrder_0_returns_the_entity_tuery_model() => _queryModel.Entities.InCreationOrder[0].Must().Be(_qmEntity1);
            [XF] public void InCreationOrder_Count_is_1() => _queryModel.Entities.InCreationOrder.Count.Must().Be(1);

            public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection
            {
               [XF] public void Contains_returns_true() => _queryModel.Entities.Contains(_entity1.Id).Must().Be(true);
               [XF] public void Get_returns_the_entity_tuery_model() => _queryModel.Entities.Get(_entity1.Id).Must().Be(_qmEntity1);
               [XF] public void Indexer_returns_the_entity() => _queryModel.Entities[_entity1.Id].Must().Be(_qmEntity1);

               [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
               {
                  _queryModel.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Must().BeTrue();
                  qmEntity1Fetched.Must().Be(_qmEntity1);
               }
            }
         }
      }
   }
}
