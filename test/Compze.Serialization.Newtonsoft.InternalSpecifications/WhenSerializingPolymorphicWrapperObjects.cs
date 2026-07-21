using Compze.Abstractions;
using Compze.Tessaging;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Serialization.Newtonsoft.InternalSpecifications;

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
                                                     "Value": "10000000-0000-0000-0000-000000000000"
                                                   },
                                                   {
                                                     "$type": "a1d63763-f934-493b-ae92-aeb2f15368b7, 0",
                                                     "Value": "20000000-0000-0000-0000-000000000000"
                                                   },
                                                   {
                                                     "$type": "5d87bfa3-5f88-4d3b-8971-c994757286ce, 0",
                                                     "Value": "30000000-0000-0000-0000-000000000000"
                                                   },
                                                   {
                                                     "$type": "0469d68b-776e-4844-8766-1cec0a563e9c, 0",
                                                     "Value": "40000000-0000-0000-0000-000000000000"
                                                   },
                                                   {
                                                     "$type": "27226150-359e-45c7-9881-35879724612b, 0",
                                                     "Value": "50000000-0000-0000-0000-000000000000"
                                                   },
                                                   {
                                                     "$type": "2acb8a85-b9ba-4354-b545-9c6e86c4ece8, 0",
                                                     "Value": "60000000-0000-0000-0000-000000000000"
                                                   }
                                                 ]
                                               }
                                               """;

         [PCTSerializer] public void contains_type_ids_for_the_types_that_differ_from_the_declared_type_but_not_those_that_are_the_same() =>
            Root.Create()
                ._(DocumentSerializer.Serialize)
                .Must()
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

         roundTripped.Must().DeepEqual(original);
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
                      Ids =
                      [
                         null,
                         new EntityId(Guid.Parse("10000000-0000-0000-0000-000000000000")),
                         new TentityId(Guid.Parse("20000000-0000-0000-0000-000000000000")),
                         new TaggregateId(Guid.Parse("30000000-0000-0000-0000-000000000000")),
                         new TessageId(Guid.Parse("40000000-0000-0000-0000-000000000000")),
                         new PersonId(Guid.Parse("50000000-0000-0000-0000-000000000000")),
                         new UserId(Guid.Parse("60000000-0000-0000-0000-000000000000"))
                      ]
                   };
         }

         public List<EntityId?> Ids { get; set; } = [];
      }
   }
}
