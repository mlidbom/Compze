namespace Compze.TypeIdentifiers;

/// <summary>
/// GUID-backed identity for an open generic definition (e.g. <c>List&lt;&gt;</c>, <c>Dictionary&lt;,&gt;</c>).
/// This is NOT a <see cref="TypeIdentifier"/> — open generics are templates, not fully constructed types.
/// <c>OpenGenericId</c> exists solely as a building block for constructing and parsing
/// <see cref="ConstructedTypeIdentifier"/> strings.
/// </summary>
readonly struct OpenGenericId(Guid guidValue) : IEquatable<OpenGenericId>
{
   public Guid GuidValue { get; } = guidValue;

   public bool Equals(OpenGenericId other) => GuidValue == other.GuidValue;
   public override bool Equals(object? obj) => obj is OpenGenericId other && Equals(other);
   public override int GetHashCode() => GuidValue.GetHashCode();
   public override string ToString() => GuidValue.ToString();

   public static bool operator ==(OpenGenericId left, OpenGenericId right) => left.Equals(right);
   public static bool operator !=(OpenGenericId left, OpenGenericId right) => !left.Equals(right);
}
