using static Compze.Abstractions.Refactoring.Naming.Internal.Implementation.TypeMapperType;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>Result of correlating scanned types with existing mappings for one assembly.</summary>
class AssemblyCorrelationResult(
   IReadOnlyList<(ExplicitlyMappedType Type, TypeId Id)> matchedExplicitTypes,
   IReadOnlyList<ExplicitlyMappedType> missingExplicitTypes,
   IReadOnlyList<(ComputedTypeIdType Type, TypeId Id)> resolvedComputedTypes,
   IReadOnlyList<ComputedTypeIdType> unresolvedComputedTypes)
{
   internal IReadOnlyList<(ExplicitlyMappedType Type, TypeId Id)> MatchedExplicitTypes { get; } = matchedExplicitTypes;
   internal IReadOnlyList<ExplicitlyMappedType> MissingExplicitTypes { get; } = missingExplicitTypes;
   internal IReadOnlyList<(ComputedTypeIdType Type, TypeId Id)> ResolvedComputedTypes { get; } = resolvedComputedTypes;
   internal IReadOnlyList<ComputedTypeIdType> UnresolvedComputedTypes { get; } = unresolvedComputedTypes;

   internal bool HasMissingMappings => MissingExplicitTypes.Count > 0 || UnresolvedComputedTypes.Count > 0;
}

/// <summary>Correlates scanned assembly types with existing TypeId mappings and computes deterministic IDs.</summary>
static class AssemblyMappingCorrelator
{
   /// <param name="scannedTypes">Types discovered in the assembly by the scanner.</param>
   /// <param name="assemblyMappings">Explicit TypeId assignments from the assembly's mapping file.</param>
   /// <param name="resolveTypeId">Resolves a <see cref="System.Type"/> to its TypeId from the global state.
   /// Used for cross-assembly component lookups (e.g. an open generic defined in another assembly).</param>
   internal static AssemblyCorrelationResult Correlate(
      ScannedAssemblyTypes scannedTypes,
      IReadOnlyDictionary<Type, TypeId> assemblyMappings,
      Func<Type, TypeId?> resolveTypeId)
   {
      var matched = new List<(ExplicitlyMappedType, TypeId)>();
      var missing = new List<ExplicitlyMappedType>();

      CorrelateExplicitTypes(scannedTypes.LeafTypes, assemblyMappings, matched, missing);
      CorrelateExplicitTypes(scannedTypes.OpenGenericDefinitions, assemblyMappings, matched, missing);

      // Build a lookup that combines assembly mappings + previously resolved global state.
      // The matched explicit types from this assembly should also be usable for computing derived types.
      var localResolutions = matched.ToDictionary(m => m.Item1.Type, m => m.Item2);

      TypeId? ResolveForComputation(Type type)
      {
         if(localResolutions.TryGetValue(type, out var id)) return id;
         return resolveTypeId(type);
      }

      var resolved = new List<(ComputedTypeIdType, TypeId)>();
      var unresolved = new List<ComputedTypeIdType>();

      foreach(var closedGeneric in scannedTypes.ClosedGenericTypes)
         ResolveComputedType(closedGeneric, ResolveForComputation, resolved, unresolved);

      foreach(var arrayType in scannedTypes.ArrayTypes)
         ResolveComputedType(arrayType, ResolveForComputation, resolved, unresolved);

      return new AssemblyCorrelationResult(matched, missing, resolved, unresolved);
   }

   static void CorrelateExplicitTypes<TExplicit>(
      IReadOnlyList<TExplicit> types,
      IReadOnlyDictionary<Type, TypeId> mappings,
      List<(ExplicitlyMappedType, TypeId)> matched,
      List<ExplicitlyMappedType> missing)
      where TExplicit : ExplicitlyMappedType
   {
      foreach(var typed in types)
      {
         if(mappings.TryGetValue(typed.Type, out var typeId))
            matched.Add((typed, typeId));
         else
            missing.Add(typed);
      }
   }

   static void ResolveComputedType(ComputedTypeIdType computedType, Func<Type, TypeId?> resolve,
      List<(ComputedTypeIdType, TypeId)> resolved, List<ComputedTypeIdType> unresolved)
   {
      var computedId = computedType switch
      {
         ClosedGenericType closed => TryComputeClosedGenericTypeId(closed, resolve),
         ArrayType array          => TryComputeArrayTypeId(array, resolve),
         _                        => null
      };

      if(computedId != null)
         resolved.Add((computedType, computedId));
      else
         unresolved.Add(computedType);
   }

   static TypeId? TryComputeClosedGenericTypeId(ClosedGenericType closedGeneric, Func<Type, TypeId?> resolve)
   {
      var definitionId = resolve(closedGeneric.GenericDefinition);
      if(definitionId == null) return null;

      var argIds = new TypeId[closedGeneric.TypeArguments.Count];
      for(var i = 0; i < closedGeneric.TypeArguments.Count; i++)
      {
         var argId = resolve(closedGeneric.TypeArguments[i]);
         if(argId == null) return null;
         argIds[i] = argId;
      }

      return DeterministicTypeIdGenerator.ComputeCompositeTypeId(definitionId, argIds);
   }

   static TypeId? TryComputeArrayTypeId(ArrayType arrayType, Func<Type, TypeId?> resolve)
   {
      var elementId = resolve(arrayType.ElementType);
      if(elementId == null) return null;

      return DeterministicTypeIdGenerator.ComputeCompositeTypeId(
         DeterministicTypeIdGenerator.ArrayMarkerTypeId,
         elementId);
   }
}
