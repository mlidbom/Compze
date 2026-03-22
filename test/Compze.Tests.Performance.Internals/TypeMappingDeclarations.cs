using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tests.Performance.Internals.AssemblyTypeMapper))]

namespace Compze.Tests.Performance.Internals;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(ITypeMappingRegistrar map)
   {
      map.Map<Serialization.TestTevent>("7b513dd1-e697-4544-83c3-06c17819eaeb")
         .Map<Tessaging.Hypermedia.PerformanceTestBase.MyRemoteTuery>("1061c1d5-f5cd-4885-88ad-1fd48d405ff0");
   }
}
