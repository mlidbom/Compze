using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class The_component
      {
         public partial class After_calling_AddNonRemovableEntity_with_name_entity1_and_a_newGuid
         {
            public partial class After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid
            {
               public class After_calling_rename_on_non_removable_entity1_with_string_newName : After_calling_AddNonRemovableEntity_with_name_entity2_and_a_newGuid
               {
                  public After_calling_rename_on_non_removable_entity1_with_string_newName() => _nonRemovableEntity1.Rename("newName");
                  [XF] public void entity1_name_is_newName() => _nonRemovableEntity1.Name.Must().Be("newName");
                  [XF] public void QueryModel_entity1_name_is_newName() => _qmNonRemovableEntity1.Name.Must().Be("newName");
                  [XF] public void entity2_name_remains_entity2() => _nonRemovableEntity2.Name.Must().Be("entity2");
                  [XF] public void QueryModel_entity2_name_remains_entity2() => _qmNonRemovableEntity2.Name.Must().Be("entity2");

                  public class After_calling_rename_on_non_removable_entity2_with_string_newName2 : After_calling_rename_on_non_removable_entity1_with_string_newName
                  {
                     public After_calling_rename_on_non_removable_entity2_with_string_newName2() => _nonRemovableEntity2.Rename("newName2");
                     [XF] public void entity2_name_is_newName2() => _nonRemovableEntity2.Name.Must().Be("newName2");
                     [XF] public void QueryModel_entity2_name_is_newName2() => _qmNonRemovableEntity2.Name.Must().Be("newName2");
                     [XF] public void entity1_name_remains_newName() => _nonRemovableEntity1.Name.Must().Be("newName");
                     [XF] public void QueryModel_entity1_name_remains_newName() => _qmNonRemovableEntity1.Name.Must().Be("newName");
                  }
               }
            }
         }
      }
   }
}
