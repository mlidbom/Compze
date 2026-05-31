using System.Text.RegularExpressions;

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
abstract partial class TypeIdentifier : IEquatable<TypeIdentifier>
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
   /// Classifies a component as mapped by its reserved "0" assembly, normalizes named components to their short
   /// assembly name (stripping Version/Culture/PublicKeyToken), and wraps array types as <see cref="ArrayTypeIdentifier"/>.
   /// </summary>
   internal static TypeIdentifier Parse(string assemblyQualifiedName)
   {
      var (typePart, assemblyPart) = SplitTypeAndAssembly(assemblyQualifiedName);
      return ParseTypePart(typePart, assemblyPart);
   }

   // The CLR writes the outermost array rank as the last bracket group (e.g. "Int32[,][]" is a 1-D array of
   // 2-D arrays). So peel the trailing suffix as the outermost array and recurse on the remaining element
   // type part — which may itself be another array, a generic, or a leaf. Recursing (rather than stripping a
   // single suffix) is what makes jagged arrays like "List`1[[...]][][]" keep their nested structure, so a
   // mapped component nested inside them is still transformed to its GUID.
   static TypeIdentifier ParseTypePart(string typePart, string assemblyPart)
   {
      var arraySuffixMatch = TrailingArraySuffixPattern().Match(typePart);
      if(arraySuffixMatch.Success)
      {
         var rank = arraySuffixMatch.Groups[1].Value.Count(c => c == ',') + 1;
         var elementTypePart = typePart[..arraySuffixMatch.Index];
         return new ArrayTypeIdentifier(ParseTypePart(elementTypePart, assemblyPart), rank);
      }

      var genericMatch = GenericTypePartPattern().Match(typePart);
      return genericMatch.Success
                ? ParseGenericFromParts(genericMatch, assemblyPart)
                : ParseLeafFromParts(typePart.Trim(), assemblyPart);
   }

   static TypeIdentifier ParseLeafFromParts(string typeName, string assemblyName)
   {
      if(IsReservedMappedAssembly(assemblyName))
         return new MappedTypeIdentifier(ParseMappedGuid(typeName));

      return new StableLeafTypeIdentifier(typeName, ShortAssemblyName(assemblyName));
   }

   static TypeIdentifier ParseGenericFromParts(Match genericMatch, string assemblyName)
   {
      var typeName = genericMatch.Groups[1].Value.Trim();
      var argumentBlock = genericMatch.Groups[2].Value;
      var innerBlock = argumentBlock[1..^1];

      var parsedArguments = GenericArgumentPattern()
         .Matches(innerBlock)
         .Select(m => Parse(m.Groups[1].Value))
         .ToArray();

      if(IsReservedMappedAssembly(assemblyName))
         return new MappedGenericTypeIdentifier(ParseMappedGuid(typeName), parsedArguments);

      return new StableGenericTypeIdentifier(typeName, ShortAssemblyName(assemblyName), parsedArguments);
   }

   // The reserved literal "0" in the assembly position is the authoritative discriminator for a mapped component.
   // A GUID-shaped type name in a real assembly is just a type that happens to look like a GUID — it stays named.
   static bool IsReservedMappedAssembly(string assemblyName) => assemblyName == "0";

   // In the reserved-"0" assembly the type name must be a canonical dashed RFC-4122 GUID. We reject anything else
   // (a real type name, or a dash-less 'N'-format GUID) rather than fall back to Guid.TryParse's lenient formats,
   // so the canonical mapped string is the only thing that classifies as mapped.
   static Guid ParseMappedGuid(string typeName) =>
      DashedRfc4122GuidPattern().IsMatch(typeName)
         ? Guid.Parse(typeName)
         : throw new FormatException($"The reserved assembly \"0\" requires a dashed RFC-4122 GUID type name, but got: \"{typeName}\"");

   // Strips the Version/Culture/PublicKeyToken qualifiers, leaving only the short assembly name. Doing this on read
   // makes identity stable across runtime upgrades, and collapses a full AssemblyQualifiedName and its short form
   // to one canonical string.
   static string ShortAssemblyName(string assemblyName)
   {
      var commaIndex = assemblyName.IndexOf(',', StringComparison.Ordinal);
      return commaIndex >= 0 ? assemblyName[..commaIndex].Trim() : assemblyName;
   }

   static (string typePart, string assemblyPart) SplitTypeAndAssembly(string assemblyQualifiedName)
   {
      var match = TypeAndAssemblyPartsPattern().Match(assemblyQualifiedName);
      if(!match.Success)
         throw new FormatException($"Invalid AssemblyQualifiedName format: \"{assemblyQualifiedName}\"");

      return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
   }

   // Splits "TypePart, AssemblyName" at the first comma not inside brackets.
   [GeneratedRegex(@"^\s*((?:[^\[\],]|\[(?<D>)|\](?<-D>)|(?(D),|(?!)))*(?(D)(?!)))\s*,\s*(.+)$")]
   private static partial Regex TypeAndAssemblyPartsPattern();

   // Matches a trailing array suffix like [], [,], [,,] at the end of a type part.
   [GeneratedRegex(@"(\[,*\])$")]
   private static partial Regex TrailingArraySuffixPattern();

   // Splits a type part into the type name and the generic argument block.
   [GeneratedRegex(@"^(.+?)(\[\[.+\]\])$")]
   private static partial Regex GenericTypePartPattern();

   // Matches each individual "[argument]" inside a generic argument block.
   [GeneratedRegex(@"\[((?:[^\[\]]|\[(?<D>)|\](?<-D>))*(?(D)(?!)))\]")]
   private static partial Regex GenericArgumentPattern();

   // Matches a canonical dashed RFC-4122 GUID (8-4-4-4-12 hex), anchored so nothing else qualifies.
   [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
   private static partial Regex DashedRfc4122GuidPattern();
}
