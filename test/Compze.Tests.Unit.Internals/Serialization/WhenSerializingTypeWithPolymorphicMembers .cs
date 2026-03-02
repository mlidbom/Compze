using System.Collections.Generic;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Unit.Internals.Serialization.OriginalTypes;
using Compze.Underscore;
using Compze.Utilities.Testing.Must;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization
{
   public class WhenSerializingTypeWithPolymorphicMembers : SerializerTest
   {
      [PCTSerializer] public void SerializedDataIs() =>
         Root.Create()
             ._(DocumentSerializer.Serialize)
             .Must()
             .Be("""
                    {
                      "ATypeProperty": {
                        "$type": "645544b7-e56c-4e3c-81cd-149e9be90bd7",
                        "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeA"
                      },
                      "BTypeProperty": {
                        "$type": "acd2c07a-d3d1-4217-9e71-b13c2775e86d",
                        "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeB"
                      },
                      "ListOfAType": [
                        {
                          "$type": "645544b7-e56c-4e3c-81cd-149e9be90bd7",
                          "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeA"
                        },
                        {
                          "$type": "acd2c07a-d3d1-4217-9e71-b13c2775e86d",
                          "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeB"
                        },
                        {
                          "$type": "f583784b-29d2-499b-a205-59ea6ef57cb3",
                          "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeA+TypeAA"
                        },
                        {
                          "$type": "d65a7c6a-eeb5-485a-a86a-cd4ac8ca99cf",
                          "Value": "Compze.Tests.Unit.Internals.Serialization.OriginalTypes.TypeB+TypeBB"
                        }
                      ]
                    }
                    """);

      [PCTSerializer] public void RoundTrippedObjectIsIdenticalToOriginal()
      {
         var original = Root.Create();

         var json = DocumentSerializer.Serialize(original);

         var roundTripped = DocumentSerializer.Deserialize<Root>(json);

         roundTripped.Must().DeepEqual(original);
      }
   }

   namespace OriginalTypes
   {
      class BaseTypeA
      {
         //Use the full type name to ensure that our code does not get confused by the types containing properties containing the type names.
         public BaseTypeA() => Value = GetType().FullName!;
         public string Value { get; private set; }
      }

      class TypeA : BaseTypeA
      {
         public class TypeAA : TypeA;
      }

      class TypeB : BaseTypeA
      {
         public class TypeBB : TypeB;
      }

      class Root
      {
         internal static Root Create() => new()
                                          {
                                             ATypeProperty = new TypeA(),
                                             BTypeProperty = new TypeB(),
                                             ListOfAType = [new TypeA(), new TypeB(), new TypeA.TypeAA(), new TypeB.TypeBB()]
                                          };

         public BaseTypeA? ATypeProperty { get; set; }
         public BaseTypeA? BTypeProperty { get; set; }

         public List<BaseTypeA> ListOfAType { get; set; } = [];
      }
   }
}
