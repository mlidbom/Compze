using System;
using System.Collections.Generic;
using Compze.Core.Public;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Tests.Infrastructure.FluentAssertionsExtensions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Functional;
using FluentAssertions;
using Shouldly;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization;

public class When_serializing_polymorphic_wrapper_objects : SerializerTest
{
   public class specifically_entity_ids : When_serializing_polymorphic_wrapper_objects
   {
      public class the_initial_serialized_data : specifically_entity_ids
      {
         const string ExpectedSerializedData = """
                                               {
                                                 "Ids": [
                                                   null,
                                                   {
                                                     "PrimitiveValue": "34db64a2-dc5b-46a7-badb-d92c2e4fa0f8"
                                                   },
                                                   {
                                                     "$type": "a1d63763-f934-493b-ae92-aeb2f15368b7",
                                                     "PrimitiveValue": "780b941a-e121-49b3-b665-0d53ea6a943a"
                                                   },
                                                   {
                                                     "$type": "5d87bfa3-5f88-4d3b-8971-c994757286ce",
                                                     "PrimitiveValue": "c7c9a76d-204b-4884-acde-e7b0494e9c34"
                                                   },
                                                   {
                                                     "$type": "0469d68b-776e-4844-8766-1cec0a563e9c",
                                                     "PrimitiveValue": "23306e93-a8fe-4040-964a-d5d4a2ba119f"
                                                   },
                                                   {
                                                     "$type": "27226150-359e-45c7-9881-35879724612b",
                                                     "PrimitiveValue": "2e9f6ca8-b573-4195-8d1b-0a06c07f73a0"
                                                   },
                                                   {
                                                     "$type": "2acb8a85-b9ba-4354-b545-9c6e86c4ece8",
                                                     "PrimitiveValue": "3e633b05-8241-4257-bcbb-dd02b87a67ee"
                                                   }
                                                 ]
                                               }
                                               """;

         [PCTSerializer] public void contains_type_ids_for_the_types_that_differ_from_the_declared_type_but_not_those_that_are_the_same() =>
            Root.Create()
                ._(DocumentSerializer.Serialize)
                .Must()
                 //bug: we have lost the types
                .Be(ExpectedSerializedData);

         public class round_tripped_serialized_data : the_initial_serialized_data
         {
            [PCTSerializer]
            public void is_identical_to_the_original_serialized_data() =>
               Root.Create()
                 ._(DocumentSerializer.Serialize)
                 ._(DocumentSerializer.Deserialize<Root>)
                 ._(DocumentSerializer.Serialize)
                 .Must()
                 .Be(ExpectedSerializedData);
         }
        }

      [PCTSerializer] public void RoundTrippedObjectIsIdenticalToOriginal()
      {
         var original = Root.Create();

         var json = DocumentSerializer.Serialize(original);

         var roundTripped = DocumentSerializer.Deserialize<Root>(json);

         roundTripped.Should().BeStrictlyEquivalentTo(original);
      }

      internal class PersonId(Guid id) : TentityId(id)
      {
         public PersonId() : this(Guid.NewGuid()) {}
      }

      internal class UserId(Guid id) : PersonId(id)
      {
         public UserId() : this(Guid.NewGuid()) {}
      }

      internal class Root
      {
         public static Root Create()
         {
            return new Root()
                   {
                      Ids = new List<EntityId?>
                            {
                               null,
                               new EntityId(Guid.Parse("34DB64A2-DC5B-46A7-BADB-D92C2E4FA0F8")),
                               new TentityId(Guid.Parse("780B941A-E121-49B3-B665-0D53EA6A943A")),
                               new TaggregateId(Guid.Parse("C7C9A76D-204B-4884-ACDE-E7B0494E9C34")),
                               new TessageId(Guid.Parse("23306E93-A8FE-4040-964A-D5D4A2BA119F")),
                               new PersonId(Guid.Parse("2E9F6CA8-B573-4195-8D1B-0A06C07F73A0")),
                               new UserId(Guid.Parse("3E633B05-8241-4257-BCBB-DD02B87A67EE"))
                            }
                   };
         }

         public List<EntityId?> Ids { get; set; }
      }
   }
}
