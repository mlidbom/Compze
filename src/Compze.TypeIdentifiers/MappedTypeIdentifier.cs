namespace Compze.TypeIdentifiers;

/// <summary>
/// A leaf type from a mapped assembly. Has an explicitly assigned GUID.
/// This is the only <see cref="TypeIdentifier"/> subtype storable in SQL GUID columns.
/// </summary>
public sealed class MappedTypeIdentifier : TypeIdentifier
{
   public Guid GuidValue { get; }
   public override string StringRepresentation { get; }

   public MappedTypeIdentifier(Guid guidValue)
   {
      GuidValue = guidValue;
      StringRepresentation = $"{guidValue}, 0";
   }

   // Override equality to use GUID directly — faster than string comparison
   public override bool Equals(object? obj) => obj is MappedTypeIdentifier other && GuidValue == other.GuidValue;
   public override int GetHashCode() => GuidValue.GetHashCode();
}
