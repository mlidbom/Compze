﻿using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId;

public static partial class Composite_aggregate_specification
{
   public partial class After_constructing_root_aggregate_with_name_root_and_slaving_a_query_model_to_the_aggregates_events
   {
      public partial class After_calling_AddEntity_with_name_RootEntity_and_a_new_Guid
      {
         public partial class Using_RootEntity
         {
            public partial class After_calling_AddEntity_with_name_entity1_and_a_newGuid
            {
               public partial class After_calling_AddEntity_with_name_entity2_and_a_newGuid
               {
                  public partial class After_calling_rename_on_entity1_with_string_newName : After_calling_AddEntity_with_name_entity2_and_a_newGuid
                  {
                     public After_calling_rename_on_entity1_with_string_newName() => _entity1.Rename("newName");
                     [XFact] public void entity1_name_is_newName() => _entity1.Name.Should().Be("newName");
                     [XFact] public void QueryModel_entity1_name_is_newName() => _qmEntity1.Name.Should().Be("newName");
                     [XFact] public void entity2_name_remains_entity2() => _entity2.Name.Should().Be("entity2");
                     [XFact] public void QueryModel_entity2_name_remains_entity2() => _qmEntity2.Name.Should().Be("entity2");
                  }
               }
            }
         }
      }
   }
}
