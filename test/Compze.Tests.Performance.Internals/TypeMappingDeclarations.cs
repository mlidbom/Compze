using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Tests.Performance.Internals.TypeMappingDeclarations))]

namespace Compze.Tests.Performance.Internals;

#pragma warning disable CA1812 // Instantiated via reflection by StructuralTypeMapper, located via [assembly: TypeMappings]
class TypeMappingDeclarations : ITypeMappingDeclaration
#pragma warning restore CA1812
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<Serialization.TestTevent>("7b513dd1-e697-4544-83c3-06c17819eaeb")
         .Map<Tessaging.Hypermedia.PerformanceTestBase.MyRemoteTuery>("1061c1d5-f5cd-4885-88ad-1fd48d405ff0");
   }
}
