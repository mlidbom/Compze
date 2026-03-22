namespace Compze.TypeIdentifiers;

/// <summary>
/// A leaf type with an explicitly assigned GUID: <c>Guid, 0</c>.
/// This is the only <see cref="TypeIdentifier"/> subtype storable in SQL GUID columns.
/// </summary>
public sealed class MappedTypeIdentifier(Guid guidValue) : TypeIdentifier
{
   public Guid GuidValue { get; } = guidValue;

   internal override string TypePart => GuidValue.ToString();
   internal override string AssemblyPart => "0";

   internal override Type ResolveToType(ITypeMappingLookup lookup) => lookup.GetLeafType(GuidValue);
   internal override TypeIdentifier TransformToPersisted(ITypeMappingLookup lookup) => this;

   // Override equality to use GUID directly — faster than string comparison
   public override bool Equals(object? obj) => obj is MappedTypeIdentifier other && GuidValue == other.GuidValue;
   public override int GetHashCode() => GuidValue.GetHashCode();
}
