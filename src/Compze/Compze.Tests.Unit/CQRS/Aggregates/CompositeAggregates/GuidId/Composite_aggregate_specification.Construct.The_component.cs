using Compze.Testing.TestFrameworkExtensions.XUnit;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class The_component : After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
      {
         readonly Component _component;
         readonly QueryModels.Component _qmComponent;

         public The_component()
         {
            _component = _aggregate.Component;
            _qmComponent = _queryModel.Component;
         }
         [XFact] public void Component_Name_is_empty_string() => _component.Name.Should().Be("");
         [XFact] public void QueryModel_Name_is_empty_string() => _qmComponent.Name.Should().Be("");

         public class After_calling_rename_with_string_newName : The_component
         {
            public After_calling_rename_with_string_newName() => _component.Rename("newName");

            [XFact] public void Component_Name_is_newName() => _component.Name.Should().Be("newName");
            [XFact] public void QueryModel_Name_is_newName() => _qmComponent.Name.Should().Be("newName");
         }
      }
   }
}
