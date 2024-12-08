using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         readonly RemovableEntity _rootEntity;
         readonly Entity _qmRootEntity;

         public After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid()
         {
            _rootEntity = Aggregate.AddEntity("RootEntity");
            _qmRootEntity = QueryModel.Entities.InCreationOrder[0];
         }

         [XFact] public void RootEntity_Name_is_RootEntity() => _rootEntity.Name.Should().Be("RootEntity");
         [XFact] public void QueryModel_Name_is_RootEntity() => _qmRootEntity.Name.Should().Be("RootEntity");
      }
   }
}
