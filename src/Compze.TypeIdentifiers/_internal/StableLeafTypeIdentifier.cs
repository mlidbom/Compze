using Compze.TypeIdentifiers._private;

namespace Compze.TypeIdentifiers._internal;

/// <summary>A stable non-generic type: <c>TypeName, AssemblyName</c>.</summary>
sealed class StableLeafTypeIdentifier(string typeName, string assemblyName) : TypeIdentifier
{
   public string TypeName { get; } = typeName;
   public string AssemblyName { get; } = assemblyName;

   internal override string TypePart => TypeName;
   internal override string AssemblyPart => AssemblyName;

   internal override Type ResolveToType(ITypeMappingLookup lookup) =>
      Type.GetType(StringRepresentation)
      ?? throw new InvalidOperationException($"Could not resolve stable type: {StringRepresentation}");

   internal override TypeIdentifier TransformToPersisted(ITypeMappingLookup lookup)
   {
      var leafType = Type.GetType(StringRepresentation);
      if(leafType != null)
      {
         if(lookup.TryGetLeafTypeGuid(leafType, out var guid))
            return new MappedTypeIdentifier(guid);

         if(lookup.IsStableType(leafType))
            return this;
      }

      throw new InvalidOperationException(
         $"Type '{TypeName}' from assembly '{AssemblyName}' is not mapped and its assembly is not registered as stable.");
   }
}
