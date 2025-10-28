using System.Collections.Generic;
using System.Linq;
using Compze.Tests.Infrastructure.FluentAssertionsExtensions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Unit.Internals.Serialization.OriginalTypes;
using Compze.Utilities.Functional;
using FluentAssertions;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization
{
   public class WhenSerializingTypeWithPolymorphicMembers : SerializerTest
   {
      [PCTSerializer] public void SerializedDataDoesNotContainTypeNames()
      {
         var root = Root.Create();

         var serialized = DocumentSerializer.Serialize(root);
         var typeNames = EnumerableCE.OfTypes<TypeA, TypeB, BaseTypeA, Root>()
                                     .Select(it => it.Name);

         foreach(var typeName in typeNames)
         {
            serialized.Should().NotContain(typeName);
         }
      }

      [PCTSerializer] public void RoundTrippedObjectIsIdenticalToOriginal()
      {
         var root = Root.Create();

         var json = DocumentSerializer.Serialize(root);

         var roundTripped = (Root)DocumentSerializer.Deserialize(typeof(Root), json);

         roundTripped.Should().BeStrictlyEquivalentTo(root);
      }
   }

   namespace OriginalTypes
   {
      class BaseTypeA
      {
         public BaseTypeA() => Value = GetType().Name.ToUpperInvariant();
         string Value { get; set; }
      }

      class TypeA : BaseTypeA
      {
         public class TypeAA : TypeA {}
      }

      class TypeB : BaseTypeA
      {
         public class TypeBB : TypeB {}
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
