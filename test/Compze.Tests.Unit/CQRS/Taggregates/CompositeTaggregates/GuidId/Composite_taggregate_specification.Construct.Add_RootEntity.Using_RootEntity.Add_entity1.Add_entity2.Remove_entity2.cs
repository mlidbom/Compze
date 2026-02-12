using System;
using System.Linq;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
      {
         public partial class Using_RootEntity
         {
            public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public partial class After_calling_entity2_Remove : After_calling_AddEntity_with_name_entity2_and_a_newGuid
                  {
                     public After_calling_entity2_Remove() => _entity2.Remove();

                     public class The_Entities_collection__ : After_calling_entity2_Remove
                     {
                        [XF] public void Single_return_entity1() => _rootEntity.Entities.Single().Must().Be(_entity1);
                        [XF] public void InCreationOrder_1_throws() => Invoking(() => _rootEntity.Entities.InCreationOrder[1]).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_Count_is_1() => _rootEntity.Entities.InCreationOrder.Count.Must().Be(1);

                        public class Passing_the_entity2_id_to : The_Entities_collection__
                        {
                           [XF] public void Contains_returns_false() => _rootEntity.Entities.Contains(_entity2.Id).Must().Be(false);
                           [XF] public void Get_throws() => Invoking(() => _rootEntity.Entities.Get(_entity2.Id)).Must().Throw<Exception>();
                           [XF] public void Indexer_throws() => Invoking(() => _rootEntity.Entities[_entity2.Id]).Must().Throw<Exception>();

                           [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _rootEntity.Entities.TryGet(_entity2.Id, out var entity2Fetched).Must().BeFalse();
                              entity2Fetched.Must().BeNull();
                           }
                        }
                     }

                     public class The_QueryModel_Entities_collection : After_calling_entity2_Remove
                     {
                        [XF] public void Single_return_entity1() => _qmRootEntity.Entities.Single().Must().Be(_qmEntity1);
                        [XF] public void InCreationOrder_1_throws() => Invoking(() => _qmRootEntity.Entities.InCreationOrder[1]).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_Count_is_1() => _qmRootEntity.Entities.InCreationOrder.Count.Must().Be(1);

                        public class Passing_the_entity2_id_to : The_QueryModel_Entities_collection
                        {
                           [XF] public void Contains_returns_false() => _qmRootEntity.Entities.Contains(_entity2.Id).Must().Be(false);
                           [XF] public void Get_throws() => Invoking(() => _qmRootEntity.Entities.Get(_entity2.Id)).Must().Throw<Exception>();
                           [XF] public void Indexer_throws() => Invoking(() => _qmRootEntity.Entities[_entity2.Id]).Must().Throw<Exception>();

                           [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _qmRootEntity.Entities.TryGet(_entity2.Id, out var qmEntity2Fetched).Must().BeFalse();
                              qmEntity2Fetched.Must().BeNull();
                           }
                        }
                     }
                  }
               }
            }
         }
      }
   }
}
