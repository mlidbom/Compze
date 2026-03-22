namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A mapped non-generic type identified by GUID: <c>Guid, 0</c>.</summary>
sealed class ParsedMappedLeafTypeName(Guid guid) : ParsedTypeName
{
   public Guid Guid { get; } = guid;

   internal override string TypePart => Guid.ToString();
   internal override string AssemblyPart => "0";

   internal override Type ResolveToType(ITypeMappingLookup lookup) => lookup.GetLeafType(Guid);
   internal override ParsedTypeName TransformToPersisted(ITypeMappingLookup lookup) => this;
}
