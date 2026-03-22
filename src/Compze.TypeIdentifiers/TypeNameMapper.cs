using System.Collections.Concurrent;
using Compze.TypeIdentifiers.Parsing;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Transforms between .NET <see cref="Type"/> objects and <see cref="TypeIdentifier"/> values.
/// Uses <see cref="Parsing.TypeNameParser"/> for string manipulation and mapping dictionaries for GUID↔Type lookups.
/// Supports incremental registration of assemblies. Caches results in both directions. Thread-safe.
/// </summary>
class TypeNameMapper
{
   // Leaf type mappings: Type ↔ Guid
   readonly ConcurrentDictionary<Guid, Type> _guidToLeafType = new();
   readonly ConcurrentDictionary<Type, Guid> _leafTypeToGuid = new();

   // Open generic mappings: Type ↔ Guid
   readonly ConcurrentDictionary<Guid, Type> _guidToOpenGeneric = new();
   readonly ConcurrentDictionary<Type, Guid> _openGenericToGuid = new();

   // Stable assembly names
   readonly HashSet<string> _stableAssemblyNames = [];
   readonly object _stableAssemblyNamesLock = new();

   // Caches (populated on demand, thread-safe)
   readonly ConcurrentDictionary<Type, TypeIdentifier> _typeToIdentifier = new();
   readonly ConcurrentDictionary<string, Type> _stringToType = new();

   internal void AddLeafTypeMapping(Type type, Guid guid)
   {
      _leafTypeToGuid[type] = guid;
      _guidToLeafType[guid] = type;
      ClearCaches();
   }

   internal void AddOpenGenericMapping(Type openGenericType, Guid guid)
   {
      _openGenericToGuid[openGenericType] = guid;
      _guidToOpenGeneric[guid] = openGenericType;
      ClearCaches();
   }

   internal bool TryGetOpenGenericMapping(Type openGenericType, out MappedTypeIdentifier id)
   {
      if(_openGenericToGuid.TryGetValue(openGenericType, out var guid))
      {
         id = new MappedTypeIdentifier(guid);
         return true;
      }
      id = default!;
      return false;
   }

   internal void AddStableAssemblyName(string assemblyName)
   {
      lock(_stableAssemblyNamesLock)
      {
         _stableAssemblyNames.Add(assemblyName);
      }

      ClearCaches();
   }

   void ClearCaches()
   {
      _typeToIdentifier.Clear();
      _stringToType.Clear();
   }

   internal bool HasMappingForOpenGeneric(Type openGenericType) => _openGenericToGuid.ContainsKey(openGenericType);

   internal bool IsStableType(Type type)
   {
      var assemblyName = type.Assembly.GetName().Name;
      lock(_stableAssemblyNamesLock)
      {
         return assemblyName != null && _stableAssemblyNames.Contains(assemblyName);
      }
   }

   /// <summary>
   /// Given a .NET <see cref="Type"/>, produce the correct <see cref="TypeIdentifier"/> subtype.
   /// </summary>
   internal TypeIdentifier GetId(Type type) => _typeToIdentifier.GetOrAdd(type, ComputeId);

   /// <summary>
   /// Given a <see cref="TypeIdentifier"/>, resolve it back to a .NET <see cref="Type"/>.
   /// </summary>
   internal Type GetType(TypeIdentifier typeId) => _stringToType.GetOrAdd(typeId.StringRepresentation, _ => ResolveType(typeId));

   /// <summary>
   /// Given a string from a persisted <c>$type</c> field, resolve it to a .NET <see cref="Type"/>.
   /// This is the deserialization entry point — the string is parsed, then resolved.
   /// </summary>
   internal Type GetTypeFromPersistedString(string persistedTypeString) => _stringToType.GetOrAdd(persistedTypeString, key =>
   {
      var parsed = ParsedTypeName.Parse(key);
      return ResolveFromParsed(parsed);
   });

   /// <summary>
   /// Given a raw <c>AssemblyQualifiedName</c> from Newtonsoft (serialize direction),
   /// produce the persisted string with mapped components replaced by GUIDs.
   /// </summary>
   internal string GetPersistedStringFromAssemblyQualifiedName(string assemblyQualifiedName)
   {
      var parsed = ParsedTypeName.Parse(assemblyQualifiedName);
      var transformed = TransformToPersisted(parsed);
      return transformed.ToAssemblyQualifiedNameString();
   }

   TypeIdentifier ComputeId(Type type)
   {
      // Leaf type with explicit mapping?
      if(_leafTypeToGuid.TryGetValue(type, out var guid))
         return new MappedTypeIdentifier(guid);

      // For composite types (generics, arrays), build the structural string
      if(type.IsArray || (type.IsGenericType && !type.IsGenericTypeDefinition))
      {
         var aqn = type.AssemblyQualifiedName!;
         var parsed = ParsedTypeName.Parse(aqn);
         var transformed = TransformToPersisted(parsed);
         var resultString = transformed.ToAssemblyQualifiedNameString();

         // If nothing was transformed (all stable), it's a StableNameTypeId
         if(resultString == aqn)
            return new StableNameTypeIdentifier(aqn);

         return new ConstructedTypeIdentifier(resultString);
      }

      // Non-mapped, non-composite leaf type — must be from a stable assembly
      var assemblyName = type.Assembly.GetName().Name!;
      if(_stableAssemblyNames.Contains(assemblyName))
         return new StableNameTypeIdentifier(type.AssemblyQualifiedName!);

      throw new InvalidOperationException(
         $"Type '{type.FullName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
   }

   Type ResolveType(TypeIdentifier typeId) => typeId switch
   {
      MappedTypeIdentifier mapped => _guidToLeafType.TryGetValue(mapped.GuidValue, out var type)
         ? type
         : throw new InvalidOperationException($"No type mapping found for GUID: {mapped.GuidValue}"),

      StableNameTypeIdentifier stable => Type.GetType(stable.StringRepresentation)
         ?? throw new InvalidOperationException($"Could not resolve stable type: {stable.StringRepresentation}"),

      ConstructedTypeIdentifier constructed => GetTypeFromPersistedString(constructed.StringRepresentation),

      _ => throw new ArgumentOutOfRangeException(nameof(typeId), $"Unknown TypeId subtype: {typeId.GetType().Name}")
   };

   Type ResolveFromParsed(ParsedTypeName parsed)
   {
      var assemblyName = parsed.AssemblyName.Trim();

      Type baseType;
      if(assemblyName == "0")
      {
         // Mapped component — type name is a GUID
         var guid = Guid.Parse(parsed.TypeName);
         if(parsed is ParsedGenericTypeName mappedGeneric)
         {
            // Mapped open generic definition
            if(!_guidToOpenGeneric.TryGetValue(guid, out var openGenericType))
               throw new InvalidOperationException($"No open generic mapping found for GUID: {guid}");

            var typeArgs = mappedGeneric.TypeArguments.Select(ResolveFromParsed).ToArray();
            baseType = openGenericType.MakeGenericType(typeArgs);
         }
         else
         {
            // Mapped leaf type
            if(!_guidToLeafType.TryGetValue(guid, out baseType!))
               throw new InvalidOperationException($"No type mapping found for GUID: {guid}");
         }
      }
      else
      {
         // Stable component — real type name, real assembly name
         if(parsed is ParsedGenericTypeName stableGeneric)
         {
            // Stable open generic — strip the type arguments to get the open generic definition
            var openGenericAqn = $"{parsed.TypeName}, {assemblyName}";
            var openGenericType = Type.GetType(openGenericAqn)
               ?? throw new InvalidOperationException($"Could not resolve stable open generic type: {openGenericAqn}");

            var typeArgs = stableGeneric.TypeArguments.Select(ResolveFromParsed).ToArray();
            baseType = openGenericType.MakeGenericType(typeArgs);
         }
         else
         {
            // Stable leaf type
            var fullAqn = parsed.ToAssemblyQualifiedNameString();
            baseType = Type.GetType(fullAqn)
               ?? throw new InvalidOperationException($"Could not resolve stable type: {fullAqn}");
         }
      }

      // Handle array suffix
      if(parsed.ArraySuffix != null)
      {
         var rank = parsed.ArraySuffix.Count(c => c == ',') + 1;
         baseType = rank == 1 ? baseType.MakeArrayType() : baseType.MakeArrayType(rank);
      }

      return baseType;
   }

   /// <summary>
   /// Extracts the simple assembly name from a full or simple assembly string.
   /// "System.Private.CoreLib, Version=10.0.0.0, ..." → "System.Private.CoreLib"
   /// "System.Private.CoreLib" → "System.Private.CoreLib"
   /// </summary>
   static string SimpleAssemblyName(string assemblyString)
   {
      var commaIndex = assemblyString.IndexOf(',', StringComparison.Ordinal);
      return commaIndex >= 0 ? assemblyString[..commaIndex].Trim() : assemblyString.Trim();
   }

   bool IsStableAssembly(string assemblyString)
   {
      lock(_stableAssemblyNamesLock)
      {
         return _stableAssemblyNames.Contains(SimpleAssemblyName(assemblyString));
      }
   }

   ParsedTypeName TransformToPersisted(ParsedTypeName parsed)
   {
      var assemblyName = parsed.AssemblyName.Trim();

      // Already in persisted form (assembly is "0") — pass through
      if(assemblyName == "0")
         return parsed;

      var baseTypeName = parsed.TypeName;

      // Generic type — transform arguments recursively, then check if the open generic definition is mapped
      if(parsed is ParsedGenericTypeName generic)
      {
         var transformedArgs = generic.TypeArguments.Select(TransformToPersisted).ToArray();

         var openGenericAqn = $"{baseTypeName}, {assemblyName}";
         var openGenericType = Type.GetType(openGenericAqn);

         if(openGenericType != null && _openGenericToGuid.TryGetValue(openGenericType, out var openGenericGuid))
            return new ParsedGenericTypeName(
               openGenericGuid.ToString(),
               "0",
               transformedArgs,
               parsed.ArraySuffix);

         if(IsStableAssembly(assemblyName))
            return new ParsedGenericTypeName(baseTypeName, assemblyName, transformedArgs, parsed.ArraySuffix);

         throw new InvalidOperationException(
            $"Open generic type '{baseTypeName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
      }

      // Leaf type — check if it's mapped
      var leafAqn = $"{baseTypeName}{parsed.ArraySuffix}, {assemblyName}";
      var leafType = Type.GetType(leafAqn);

      // Strip array suffix for leaf type lookup — we want the element type's mapping
      if(parsed.ArraySuffix != null && leafType != null)
      {
         var elementType = leafType.GetElementType()!;
         if(_leafTypeToGuid.TryGetValue(elementType, out var elementGuid))
            return new ParsedLeafTypeName(
               elementGuid.ToString(),
               "0",
               parsed.ArraySuffix);

         if(IsStableAssembly(assemblyName))
            return new ParsedLeafTypeName(baseTypeName, assemblyName, parsed.ArraySuffix);

         throw new InvalidOperationException(
            $"Array element type '{elementType.FullName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
      }

      if(leafType != null && _leafTypeToGuid.TryGetValue(leafType, out var leafGuid))
         return new ParsedLeafTypeName(
            leafGuid.ToString(),
            "0",
            parsed.ArraySuffix);

      // Stable leaf type — check if assembly is stable
      if(IsStableAssembly(assemblyName))
         return parsed;

      throw new InvalidOperationException(
         $"Type '{baseTypeName}' from assembly '{assemblyName}' is not mapped and its assembly is not registered as stable.");
   }
}
