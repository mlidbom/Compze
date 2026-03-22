namespace Compze.TypeIdentifiers;

/// <summary>
/// Identity of a fully constructed type. Subtypes represent different kinds of identity:
/// <list type="bullet">
///   <item><see cref="MappedTypeId"/> — leaf type from a mapped assembly. Has a GUID. SQL-storable.</item>
///   <item><see cref="StableNameTypeId"/> — type(s) entirely from stable assemblies. Untouched AssemblyQualifiedName.</item>
///   <item><see cref="ConstructedTypeId"/> — mixed: AssemblyQualifiedName with some GUID, 0 components.</item>
/// </list>
/// </summary>
public abstract class StructuralTypeId : IEquatable<StructuralTypeId>
{
   /// <summary>The string representation used in serialized <c>$type</c> fields.</summary>
   public abstract string StringRepresentation { get; }

   public bool Equals(StructuralTypeId? other) => other is not null && StringRepresentation == other.StringRepresentation;
   public override bool Equals(object? obj) => Equals(obj as StructuralTypeId);
   public override int GetHashCode() => StringRepresentation.GetHashCode(StringComparison.Ordinal);
   public override string ToString() => StringRepresentation;

   public static bool operator ==(StructuralTypeId? left, StructuralTypeId? right) => Equals(left, right);
   public static bool operator !=(StructuralTypeId? left, StructuralTypeId? right) => !Equals(left, right);
}

/// <summary>
/// A leaf type from a mapped assembly. Has an explicitly assigned GUID.
/// This is the only <see cref="StructuralTypeId"/> subtype storable in SQL GUID columns.
/// </summary>
public sealed class MappedTypeId : StructuralTypeId
{
   public Guid GuidValue { get; }
   public override string StringRepresentation { get; }

   public MappedTypeId(Guid guidValue)
   {
      GuidValue = guidValue;
      StringRepresentation = $"{guidValue}, 0";
   }

   // Override equality to use GUID directly — faster than string comparison
   public override bool Equals(object? obj) => obj is MappedTypeId other && GuidValue == other.GuidValue;
   public override int GetHashCode() => GuidValue.GetHashCode();
}

/// <summary>
/// A type — leaf or composite — where every component comes from a stable assembly.
/// The string representation is the unmodified <c>AssemblyQualifiedName</c>.
/// Resolution: pass directly to <c>Type.GetType()</c>.
/// </summary>
sealed class StableNameTypeId : StructuralTypeId
{
   public override string StringRepresentation { get; }

   public StableNameTypeId(string assemblyQualifiedName)
   {
      StringRepresentation = assemblyQualifiedName;
   }
}

/// <summary>
/// A composite type where at least one component is mapped.
/// The string representation is an <c>AssemblyQualifiedName</c>-format string
/// with <c>GUID, 0</c> in place of mapped components.
/// </summary>
sealed class ConstructedTypeId : StructuralTypeId
{
   public override string StringRepresentation { get; }

   public ConstructedTypeId(string structuralString)
   {
      StringRepresentation = structuralString;
   }
}
