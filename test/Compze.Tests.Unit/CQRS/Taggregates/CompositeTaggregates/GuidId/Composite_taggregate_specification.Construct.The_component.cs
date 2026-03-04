using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class The_component : After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
      {
         readonly Component _component;
         readonly QueryModels.Component _qmComponent;

         public The_component()
         {
            _component = _taggregate.Component;
            _qmComponent = _queryModel.Component;
         }
         [XF] public void Component_Name_is_empty_string() => _component.Name.Must().Be("");
         [XF] public void QueryModel_Name_is_empty_string() => _qmComponent.Name.Must().Be("");

         public class After_calling_rename_with_string_newName : The_component
         {
            public After_calling_rename_with_string_newName() => _component.Rename("newName");

            [XF] public void Component_Name_is_newName() => _component.Name.Must().Be("newName");
            [XF] public void QueryModel_Name_is_newName() => _qmComponent.Name.Must().Be("newName");
         }
      }
   }
}
