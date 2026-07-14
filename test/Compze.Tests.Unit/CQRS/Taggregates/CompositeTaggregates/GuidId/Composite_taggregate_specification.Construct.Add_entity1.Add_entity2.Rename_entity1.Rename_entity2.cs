using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2
         {
            public partial class After_calling_rename_on_entity1_with_string_newName
            {
               public class After_calling_rename_on_entity2_with_string_newName2 : After_calling_rename_on_entity1_with_string_newName
               {
                  public After_calling_rename_on_entity2_with_string_newName2() => _entity2.Rename("newName2");
                  [XF] public void entity2_name_is_newName2() => _entity2.Name.Must().Be("newName2");
                  [XF] public void QueryModel_entity2_name_is_newName2() => _qmEntity2.Name.Must().Be("newName2");
                  [XF] public void entity1_name_remains_newName() => _entity1.Name.Must().Be("newName");
                  [XF] public void QueryModel_entity1_name_remains_newName() => _qmEntity1.Name.Must().Be("newName");
               }
            }
         }
      }
   }
}
