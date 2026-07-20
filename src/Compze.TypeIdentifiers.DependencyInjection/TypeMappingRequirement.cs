namespace Compze.TypeIdentifiers.DependencyInjection;

/// <summary>
/// One component's declaration that it needs a particular assembly's types to have identity in the container's
/// <see cref="ITypeMap"/> — "I persist or transmit types from that assembly, so the map must cover it".
/// </summary>
/// <remarks>
/// Deliberately internal, and registered only through the verbs on <see cref="TypeMappingRegistrar"/>: a component
/// declares <em>what it needs</em>, never how the map is assembled. Every requirement the container collects is applied
/// to one <see cref="TypeMapBuilder"/> when the map is first resolved, so the requirements' registration order cannot
/// change the resulting map, and two components needing the same assembly is ordinary rather than a conflict.
/// </remarks>
sealed class TypeMappingRequirement(Action<TypeMapBuilder> declareTypeMappingRequirements)
{
   readonly Action<TypeMapBuilder> _declareTypeMappingRequirements = declareTypeMappingRequirements;

   /// <summary>Makes this requirement's declaration against the builder the container's one <see cref="ITypeMap"/> is being built from.</summary>
   internal void DeclareInto(TypeMapBuilder builder) => _declareTypeMappingRequirements(builder);

   /// <summary>Requires the types of the assembly containing <typeparamref name="T"/> to be mapped to the GUIDs that assembly declares.</summary>
   internal static TypeMappingRequirement MappedTypesFromAssemblyContaining<T>() =>
      new(it => it.MapTypesFromAssemblyContaining<T>());

   /// <summary>Requires the assembly containing <typeparamref name="T"/> to be treated as stable — its type names persisted unchanged.</summary>
   internal static TypeMappingRequirement StableTypeNamesFromAssemblyContaining<T>() =>
      new(it => it.UseStableNameStrategyForAssemblyContaining<T>());
}
