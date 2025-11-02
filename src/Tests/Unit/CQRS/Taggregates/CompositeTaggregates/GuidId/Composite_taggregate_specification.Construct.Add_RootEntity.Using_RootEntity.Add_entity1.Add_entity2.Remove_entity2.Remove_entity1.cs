using System;
using System.Linq;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;

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
                  public partial class After_calling_entity2_Remove
                  {
                     public class After_calling_entity1_Remove : After_calling_entity2_Remove
                     {
                        public After_calling_entity1_Remove() => _entity1.Remove();

                        public class The_Entities_collection___ : After_calling_entity1_Remove
                        {
                           [XF] public void Single_throws() => Invoking(() => _rootEntity.Entities.Single()).Must().Throw<Exception>();
                           [XF] public void InCreationOrder_0_throws() => Invoking(() => _rootEntity.Entities.InCreationOrder[0]).Must().Throw<Exception>();
                           [XF] public void InCreationOrder_Count_is_0() => _rootEntity.Entities.InCreationOrder.Count.Must().Be(0);

                           public class Passing_the_entity1_id_to : The_Entities_collection___
                           {
                              [XF] public void Contains_returns_false() => _rootEntity.Entities.Contains(_entity1.Id).Must().Be(false);
                              [XF] public void Get_throws() => Invoking(() => _rootEntity.Entities.Get(_entity1.Id)).Must().Throw<Exception>();
                              [XF] public void Indexer_throws() => Invoking(() => _rootEntity.Entities[_entity1.Id]).Must().Throw<Exception>();

                              [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                              {
                                 _rootEntity.Entities.TryGet(_entity1.Id, out var entity1Fetched).Must().BeFalse();
                                 entity1Fetched.Must().BeNull();
                              }
                           }
                        }

                        public class The_QueryModel_Entities_collection_ : After_calling_entity1_Remove
                        {
                           [XF] public void Single_throws() => Invoking(() => _qmRootEntity.Entities.Single()).Must().Throw<Exception>();
                           [XF] public void InCreationOrder_0_throws() => Invoking(() => _qmRootEntity.Entities.InCreationOrder[0]).Must().Throw<Exception>();
                           [XF] public void InCreationOrder_Count_is_0() => _qmRootEntity.Entities.InCreationOrder.Count.Must().Be(0);

                           public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection_
                           {
                              [XF] public void Contains_returns_false() => _qmRootEntity.Entities.Contains(_entity1.Id).Must().Be(false);
                              [XF] public void Get_throws() => Invoking(() => _qmRootEntity.Entities.Get(_entity1.Id)).Must().Throw<Exception>();
                              [XF] public void Indexer_throws() => Invoking(() => _qmRootEntity.Entities[_entity1.Id]).Must().Throw<Exception>();

                              [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                              {
                                 _qmRootEntity.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Must().BeFalse();
                                 qmEntity1Fetched.Must().BeNull();
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
}
