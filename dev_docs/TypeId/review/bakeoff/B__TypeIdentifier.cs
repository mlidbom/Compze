namespace Compze.TypeIdentifiers;

/// <summary>
/// Internal parse tree for an <c>AssemblyQualifiedName</c>-format string. Each subtype represents a
/// distinct structural form and knows how to render its canonical string, resolve to a .NET
/// <see cref="Type"/>, and transform mapped components into persisted (GUID-backed) form:
/// <list type="bullet">
///   <item><see cref="MappedTypeIdentifier"/> — leaf type with an assigned GUID.</item>
///   <item><see cref="MappedGenericTypeIdentifier"/> — generic type with a mapped open generic definition.</item>
///   <item><see cref="StableLeafTypeIdentifier"/> — non-generic type from a stable assembly.</item>
///   <item><see cref="StableGenericTypeIdentifier"/> — generic type from a stable assembly.</item>
///   <item><see cref="ArrayTypeIdentifier"/> — array wrapping any element type identifier.</item>
/// </list>
/// This is implementation detail of <see cref="TypeMapper"/>/<see cref="TypeNameMapper"/>. The public
/// type identity is <see cref="TypeId"/>.
/// </summary>
abstract class TypeIdentifier : IEquatable<TypeIdentifier>
{
   /// <summary>The type portion of the assembly-qualified name, before ", AssemblyName".</summary>
   internal abstract string TypePart { get; }

   /// <summary>The assembly name portion of the assembly-qualified name.</summary>
   internal abstract string AssemblyPart { get; }

   /// <summary>The string representation used in serialized <c>$type</c> fields.</summary>
   public string StringRepresentation => $"{TypePart}, {AssemblyPart}";

   public override string ToString() => StringRepresentation;

   /// <summary>Resolves this type identifier to a .NET <see cref="Type"/> using the provided mapping lookup.</summary>
   internal abstract Type ResolveToType(ITypeMappingLookup lookup);

   /// <summary>Transforms this type identifier into persisted form, replacing mapped components with GUIDs.</summary>
   internal abstract TypeIdentifier TransformToPersisted(ITypeMappingLookup lookup);

   public bool Equals(TypeIdentifier? other) => other is not null && StringRepresentation == other.StringRepresentation;
   public override bool Equals(object? obj) => Equals(obj as TypeIdentifier);
   public override int GetHashCode() => StringRepresentation.GetHashCode(StringComparison.Ordinal);

   public static bool operator ==(TypeIdentifier? left, TypeIdentifier? right) => Equals(left, right);
   public static bool operator !=(TypeIdentifier? left, TypeIdentifier? right) => !Equals(left, right);

   /// <summary>
   /// Parses an <c>AssemblyQualifiedName</c>-format string into the correct <see cref="TypeIdentifier"/> subtype.
   /// Assembly Version/Culture/PublicKeyToken qualifiers are stripped from every component on read, so the
   /// canonical form is always the short-name form and is stable across runtime versions. Mapped components are
   /// recognized by the reserved <c>"0"</c> assembly combined with a dashed RFC-4122 GUID type name.
   /// </summary>
   internal static TypeIdentifier Parse(string assemblyQualifiedName) => new Scanner(assemblyQualifiedName).ParseTopLevel();
}
