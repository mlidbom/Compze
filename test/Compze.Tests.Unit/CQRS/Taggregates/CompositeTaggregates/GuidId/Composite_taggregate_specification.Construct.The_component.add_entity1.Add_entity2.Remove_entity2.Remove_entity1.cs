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
      public partial class The_component
      {
         public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid
         {
            public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid
            {
               public partial class After_calling_entity2_Remove
               {
                  public class After_calling_entity1_Remove : After_calling_entity2_Remove
                  {
                     protected After_calling_entity1_Remove() => _entity1.Remove();

                     public class The_Entities_collection___ : After_calling_entity1_Remove
                     {
                        [XF] public void Single_throws() => Invoking(() => _component.Entities.Single()).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_0_throws() => Invoking(() => _component.Entities.InCreationOrder[0]).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_Count_is_0() => _component.Entities.InCreationOrder.Count.Must().Be(0);

                        public class Passing_the_entity1_id_to : The_Entities_collection___
                        {
                           [XF] public void Contains_returns_false() => _component.Entities.Contains(_entity1.Id).Must().BeFalse();
                           [XF] public void Get_throws() => Invoking(() => _component.Entities.Get(_entity1.Id)).Must().Throw<Exception>();
                           [XF] public void Indexer_throws() => Invoking(() => _component.Entities[_entity1.Id]).Must().Throw<Exception>();

                           [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _component.Entities.TryGet(_entity1.Id, out var entity1Fetched).Must().BeFalse();
                              entity1Fetched.Must().BeNull();
                           }
                        }
                     }

                     public class The_QueryModel_Entities_collection_ : After_calling_entity1_Remove
                     {
                        [XF] public void Single_throws() => Invoking(() => _qmComponent.Entities.Single()).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_0_throws() => Invoking(() => _qmComponent.Entities.InCreationOrder[0]).Must().Throw<Exception>();
                        [XF] public void InCreationOrder_Count_is_0() => _qmComponent.Entities.InCreationOrder.Count.Must().Be(0);

                        public class Passing_the_entity1_id_to : The_QueryModel_Entities_collection_
                        {
                           [XF] public void Contains_returns_false() => _qmComponent.Entities.Contains(_entity1.Id).Must().BeFalse();
                           [XF] public void Get_throws() => Invoking(() => _qmComponent.Entities.Get(_entity1.Id)).Must().Throw<Exception>();
                           [XF] public void Indexer_throws() => Invoking(() => _qmComponent.Entities[_entity1.Id]).Must().Throw<Exception>();

                           [XF] public void TryGet_returns_false_and_the_out_parameter_is_null()
                           {
                              _qmComponent.Entities.TryGet(_entity1.Id, out var qmEntity1Fetched).Must().BeFalse();
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
