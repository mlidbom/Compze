namespace Compze.TypeIdentifiers.Specifications;

/// <summary>
/// Builds <see cref="TypeNameMapper"/> instances directly from stated mappings, bypassing <see cref="TypeMapBuilder"/>
/// and the framework stability it seeds. That lets a specification state exactly which mappings exist — including none
/// at all, which is what specifies the difference a declaration makes.
/// </summary>
static class TypeNameMapperFixture
{
   internal static TypeNameMapper MapperWith(Dictionary<Type, Guid>? leafMappings = null,
                                             Dictionary<Type, Guid>? openGenericMappings = null,
                                             IEnumerable<string>? stableAssemblyNames = null,
                                             IEnumerable<string>? stablePublicKeyTokens = null)
   {
      leafMappings ??= [];
      openGenericMappings ??= [];

      return new TypeNameMapper(leafMappings.ToDictionary(it => it.Value, it => it.Key),
                                new Dictionary<Type, Guid>(leafMappings),
                                openGenericMappings.ToDictionary(it => it.Value, it => it.Key),
                                new Dictionary<Type, Guid>(openGenericMappings),
                                [..stableAssemblyNames ?? []],
                                [..stablePublicKeyTokens ?? []]);
   }
}
