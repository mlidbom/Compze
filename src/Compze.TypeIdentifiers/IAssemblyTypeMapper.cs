namespace Compze.TypeIdentifiers;

/// <summary>
/// Implemented by each assembly's mapping declaration class.
/// Called at registration time to collect type↔GUID mappings.
/// </summary>
public interface IAssemblyTypeMapper
{
   void Map(ITypeMappingRegistrar registrar);
}
