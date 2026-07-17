using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Typermedia.Client;

//The assembly's one [assembly: AssemblyTypeMapper] lives in Compze.Tessaging.TypeMappingDeclarations, which delegates here.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<TypermediaEndpointInformationQuery>("9c3286ba-6a33-4448-aec0-b33b3d59300a")
         .Map<TypermediaEndpointInformation>("23d82352-7274-4389-b145-09d70c305147");
   }
}
