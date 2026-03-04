using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;
using Compze.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid : After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
      {
         readonly RemovableEntity _rootEntity;
         readonly Entity _qmRootEntity;

         public After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid()
         {
            _rootEntity = _taggregate.AddEntity("RootEntity");
            _qmRootEntity = _queryModel.Entities.InCreationOrder[0];
         }

         [XF] public void RootEntity_Name_is_RootEntity() => _rootEntity.Name.Must().Be("RootEntity");
         [XF] public void QueryModel_Name_is_RootEntity() => _qmRootEntity.Name.Must().Be("RootEntity");
      }
   }
}
