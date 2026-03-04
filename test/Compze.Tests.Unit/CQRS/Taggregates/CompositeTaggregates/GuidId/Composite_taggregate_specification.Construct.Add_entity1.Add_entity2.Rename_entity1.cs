using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_adding_entity_named_entity1
      {
         public partial class After_adding_entity_named_entity2
         {
            public partial class After_calling_rename_on_entity1_with_string_newName : After_adding_entity_named_entity2
            {
               public After_calling_rename_on_entity1_with_string_newName() => _entity1.Rename("newName");
               [XF] public void entity1_name_is_newName() => _entity1.Name.Must().Be("newName");
               [XF] public void QueryModel_entity1_name_is_newName() => _qmEntity1.Name.Must().Be("newName");
               [XF] public void entity2_name_remains_entity2() => _entity2.Name.Must().Be("entity2");
               [XF] public void QueryModel_entity2_name_remains_entity2() => _qmEntity2.Name.Must().Be("entity2");
            }
         }
      }
   }
}
