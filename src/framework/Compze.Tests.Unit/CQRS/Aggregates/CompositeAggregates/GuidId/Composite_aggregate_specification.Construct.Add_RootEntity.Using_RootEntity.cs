using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
      {
         public partial class Using_RootEntity : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
         {
            public class After_calling_rename_with_string_newName : After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
            {
               public After_calling_rename_with_string_newName() => _rootEntity.Rename("newName");

               [XFact] public void Component_Name_is_newName() => _rootEntity.Name.Should().Be("newName");
               [XFact] public void QueryModel_Name_is_newName() => _qmRootEntity.Name.Should().Be("newName");
            }
         }
      }
   }
}
