using Compze.Abstractions.Refactoring.Naming;

[assembly: TypeMappings(typeof(Compze.Typermedia.Client.TypeMappingDeclarations))]

namespace Compze.Typermedia.Client;

class TypeMappingDeclarations : ITypeMappingDeclaration
{
   public void DeclareMappings(ITypeMappingRegistrar map)
   {
      map.Map<TypermediaEndpointInformationQuery>("9c3286ba-6a33-4448-aec0-b33b3d59300a")
         .Map<TypermediaEndpointInformation>("23d82352-7274-4389-b145-09d70c305147");
   }
}
