namespace Compze.TypeIdentifiers;

/// <summary>
/// Implemented by each assembly's mapping declaration class.
/// Called at registration time to collect type↔GUID mappings.
/// </summary>
public interface IAssemblyTypeMapper
{
   /// <summary>Declares this assembly's type↔GUID mappings into the supplied <paramref name="registrar"/>.</summary>
   void Map(IAssemblyTypeMappingRegistrar registrar);
}
