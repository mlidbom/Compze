using System;
using System.Linq;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2 : After_adding_entity_named_entity1
         {
            readonly RemovableEntity _entity2;
            readonly Entity _qmEntity2;

            public After_adding_entity_named_entity2()
            {
               _entity2 = _taggregate.AddEntity("entity2");
               _qmEntity2 = _queryModel.Entities.InCreationOrder[1];
            }

            [XF] public void The_name_of_the_added_entity_is_entity2() => _entity2.Name.Must().Be("entity2");
            [XF] public void The_name_of_the_added_tuery_model_is_entity2() => _qmEntity2.Name.Must().Be("entity2");

            public class The_taggregates_Entities_collection_ : After_adding_entity_named_entity2
            {
               [XF] public void Single_throws() => Invoking(() => _taggregate.Entities.Single()).Must().Throw<Exception>();
               [XF] public void InCreationOrder_1_returns_the_entity() => _taggregate.Entities.InCreationOrder[1].Must().Be(_entity2);
               [XF] public void InCreationOrder_Count_is_2() => _taggregate.Entities.InCreationOrder.Count.Must().Be(2);

               public class Passing_the_entitys_id_to : The_taggregates_Entities_collection_
               {
                  [XF] public void Contains_returns_true() => _taggregate.Entities.Contains(_entity2.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity() => _taggregate.Entities.Get(_entity2.Id).Must().Be(_entity2);
                  [XF] public void Indexer_returns_the_entity() => _taggregate.Entities[_entity2.Id].Must().Be(_entity2);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _taggregate.Entities.TryGet(_entity2.Id, out var agEntity2Fetched).Must().BeTrue();
                     agEntity2Fetched.Must().Be(_entity2);
                  }
               }
            }

            public class The_QueryModels_Entities_collection_ : After_adding_entity_named_entity2
            {
               [XF] public void Single_throws() => Invoking(() => _queryModel.Entities.Single()).Must().Throw<Exception>();
               [XF] public void InCreationOrder_1_returns_the_entity_tuery_model() => _queryModel.Entities.InCreationOrder[1].Must().Be(_qmEntity2);
               [XF] public void InCreationOrder_Count_is_2() => _queryModel.Entities.InCreationOrder.Count.Must().Be(2);

               public class Passing_the_entitys_id_to : The_QueryModels_Entities_collection_
               {
                  [XF] public void Contains_returns_true() => _queryModel.Entities.Contains(_entity2.Id).Must().Be(true);
                  [XF] public void Get_returns_the_entity_tuery_model() => _queryModel.Entities.Get(_entity2.Id).Must().Be(_qmEntity2);
                  [XF] public void Indexer_returns_the_entity() => _queryModel.Entities[_entity2.Id].Must().Be(_qmEntity2);

                  [XF] public void TryGet_returns_true_and_the_out_parameter_is_the_entity()
                  {
                     _queryModel.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Must().BeTrue();
                     qmEntity2Fetched.Must().Be(_qmEntity2);
                  }
               }
            }
         }
      }
   }
}
