using Compze.TypeIdentifiers;
using Compze.Abstractions.Private;

[assembly: AssemblyTypeMapper(typeof(Compze.Abstractions.Private.AssemblyTypeMapper))]

namespace Compze.Abstractions.Private;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<Public.TentityId>("a1d63763-f934-493b-ae92-aeb2f15368b7")
         .Map<Public.TaggregateId>("5d87bfa3-5f88-4d3b-8971-c994757286ce");
   }
}
