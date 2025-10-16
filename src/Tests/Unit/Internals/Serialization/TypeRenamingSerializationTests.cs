using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tests.Unit.Internals.Serialization
{
   namespace OriginalTypes
   {
      class BaseTypeA;

      class TypeA : BaseTypeA
      {
         internal static TypeA Create() => new() { TypeAName = typeof(TypeA).FullName };

         public string? TypeAName { get; set; }

         public class TypeAA : TypeA
         {
            public new static TypeA Create() => new TypeAA { TypeAAName = typeof(TypeAA).FullName };
            public string? TypeAAName { get; set; }
         }
      }

      class TypeB : BaseTypeA
      {
         internal static TypeB Create() => new() { TypeBName = typeof(TypeB).FullName };
         public string? TypeBName { get; set; }

         public class TypeBB : TypeB
         {
            public new static TypeBB Create() => new() { TypeBBName = typeof(TypeBB).FullName };
            public string? TypeBBName { get; set; }
         }
      }

      class Root
      {
         internal static Root Create() => new()
                                          {
                                             TypeA = OriginalTypes.TypeA.Create(),
                                             TypeB = OriginalTypes.TypeB.Create(),
                                             ListOfTypeA = [OriginalTypes.TypeA.Create(), OriginalTypes.TypeB.Create(), OriginalTypes.TypeA.TypeAA.Create(), OriginalTypes.TypeB.TypeBB.Create()]
                                          };

         public BaseTypeA? TypeA { get; set; }
         public BaseTypeA? TypeB { get; set; }

         public List<BaseTypeA>? ListOfTypeA { get; set; }
      }
   }
}
