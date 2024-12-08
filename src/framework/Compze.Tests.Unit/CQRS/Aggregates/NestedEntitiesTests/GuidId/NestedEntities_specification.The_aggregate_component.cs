using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class NestedEntities_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public class The_aggregate_component : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         [XFact] public void Component_Name_is_empty_string() => Aggregate.Component.Name.Should().Be("");
         [XFact] public void QueryModel_Name_is_empty_string() => QueryModel.Component.Name.Should().Be("");

         public class After_calling_rename_with_string_newName : The_aggregate_component
         {
            public After_calling_rename_with_string_newName() => Aggregate.Component.Rename("newName");

            [XFact] public void Component_Name_is_newName() => Aggregate.Component.Name.Should().Be("newName");
            [XFact] public void QueryModel_Name_is_newName() => QueryModel.Component.Name.Should().Be("newName");
         }
      }
   }
}
