using Compze.TypeIdentifiers;
using Compze.Tessaging.Implementation.Transport;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.AssemblyTypeMapper))]

namespace Compze.Tessaging;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
   {
      map.Map<TessagingEndpointInformationQuery>("e441c4e2-cc09-4331-9dd9-c9790e72987a");
      new Typermedia.Client.AssemblyTypeMapper().Map(map);
   }
}
