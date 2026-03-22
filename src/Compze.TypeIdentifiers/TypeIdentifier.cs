namespace Compze.TypeIdentifiers;

/// <summary>
/// Identity of a fully constructed type. Subtypes represent different kinds of identity:
/// <list type="bullet">
///   <item><see cref="MappedTypeIdentifier"/> — leaf type from a mapped assembly. Has a GUID. SQL-storable.</item>
///   <item><see cref="StableNameTypeIdentifier"/> — type(s) entirely from stable assemblies. Untouched AssemblyQualifiedName.</item>
///   <item><see cref="ConstructedTypeIdentifier"/> — mixed: AssemblyQualifiedName with some GUID, 0 components.</item>
/// </list>
/// </summary>
public abstract class TypeIdentifier : IEquatable<TypeIdentifier>
{
   /// <summary>The string representation used in serialized <c>$type</c> fields.</summary>
   public abstract string StringRepresentation { get; }

   public bool Equals(TypeIdentifier? other) => other is not null && StringRepresentation == other.StringRepresentation;
   public override bool Equals(object? obj) => Equals(obj as TypeIdentifier);
   public override int GetHashCode() => StringRepresentation.GetHashCode(StringComparison.Ordinal);
   public override string ToString() => StringRepresentation;

   public static bool operator ==(TypeIdentifier? left, TypeIdentifier? right) => Equals(left, right);
   public static bool operator !=(TypeIdentifier? left, TypeIdentifier? right) => !Equals(left, right);
}