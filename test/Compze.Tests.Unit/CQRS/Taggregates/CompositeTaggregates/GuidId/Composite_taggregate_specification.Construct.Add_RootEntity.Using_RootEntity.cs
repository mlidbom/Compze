using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
      {
         public partial class Using_RootEntity : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
         {
            public class After_calling_rename_with_string_newName : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
            {
               public After_calling_rename_with_string_newName() => _rootEntity.Rename("newName");

               [XF] public void Component_Name_is_newName() => _rootEntity.Name.Must().Be("newName");
               [XF] public void QueryModel_Name_is_newName() => _qmRootEntity.Name.Must().Be("newName");
            }
         }
      }
   }
}
