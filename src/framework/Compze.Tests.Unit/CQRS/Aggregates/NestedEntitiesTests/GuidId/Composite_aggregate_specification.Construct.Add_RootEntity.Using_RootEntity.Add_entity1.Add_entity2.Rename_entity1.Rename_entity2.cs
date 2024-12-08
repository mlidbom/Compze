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
                  public partial class After_calling_rename_on_entity1_with_string_newName
                  {
                     public class After_calling_rename_on_entity2_with_string_newName2 : After_calling_rename_on_entity1_with_string_newName
                     {
                        public After_calling_rename_on_entity2_with_string_newName2() => _entity2.Rename("newName2");
                        [XFact] public void entity2_name_is_newName2() => _entity2.Name.Should().Be("newName2");
                        [XFact] public void entity2_QueryModel_name_is_newName2() => _qmEntity2.Name.Should().Be("newName2");
                        [XFact] public void entity1_name_remains_newName() => _entity1.Name.Should().Be("newName");
                        [XFact] public void entity1_QueryModel_name_remains_newName() => _qmEntity1.Name.Should().Be("newName");
                     }
                  }
               }
            }
         }
      }
   }
}
