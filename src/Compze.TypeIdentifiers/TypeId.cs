namespace Compze.TypeIdentifiers;

/// <summary>
/// The canonical identity of a .NET type in a type-mapped system: the resolved <see cref="System.Type"/>
/// paired with its rename-safe canonical string representation.
/// <para>
/// A <see cref="TypeId"/> is always canonical for its type — two instances representing the same type are
/// equal and share the same <see cref="CanonicalString"/>. This invariant is guaranteed by construction:
/// instances can only be obtained from an <see cref="ITypeMap"/>, never built directly.
/// </para>
/// </summary>
public sealed class TypeId : IEquatable<TypeId>
{
   internal TypeId(Type type, string canonicalString)
   {
      Type = type;
      CanonicalString = canonicalString;
   }

   /// <summary>The resolved .NET type.</summary>
   public Type Type { get; }

   /// <summary>
   /// The canonical string used in serialized <c>$type</c> fields. Mapped components appear as
   /// <c>"GUID, 0"</c>; stable components keep their assembly-qualified names. Rename-safe.
   /// </summary>
   public string CanonicalString { get; }

   public override string ToString() => CanonicalString;

   // Equality is type identity. .NET guarantees one Type instance per type per load context, so reference
   // equality on Type is both correct and cheaper than comparing canonical strings.
   public bool Equals(TypeId? other) => other is not null && Type == other.Type;
   public override bool Equals(object? obj) => Equals(obj as TypeId);
   public override int GetHashCode() => Type.GetHashCode();

   public static bool operator ==(TypeId? left, TypeId? right) => Equals(left, right);
   public static bool operator !=(TypeId? left, TypeId? right) => !Equals(left, right);
}
