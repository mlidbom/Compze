using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Typermedia.Client.AssemblyTypeMapper))]

namespace Compze.Typermedia.Client;

class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<TypermediaEndpointInformationQuery>("9c3286ba-6a33-4448-aec0-b33b3d59300a")
         .Map<TypermediaEndpointInformation>("23d82352-7274-4389-b145-09d70c305147");
   }
}
