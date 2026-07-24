using Compze.TypeIdentifiers._private;

namespace Compze.TypeIdentifiers._private;

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
/// This is implementation detail of <see cref="TypeMap"/>/<see cref="TypeNameMapper"/>. The public
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
   /// A component is classified as mapped by its reserved <c>"0"</c> assembly; named components are normalized to
   /// their short assembly name (Version/Culture/PublicKeyToken stripped); array types wrap as <see cref="ArrayTypeIdentifier"/>.
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
      if(TryPeelTrailingArraySuffix(typePart, out var elementTypePart, out var rank))
         return new ArrayTypeIdentifier(ParseTypePart(elementTypePart, assemblyPart), rank);

      // A type name never contains a bracket (generic arity is a backtick, nested types use '+'), and array
      // suffixes have already been peeled — so a remaining '[' can only begin the generic argument block.
      var genericBlockStart = typePart.IndexOf('[', StringComparison.Ordinal);
      return genericBlockStart >= 0
                ? ParseGeneric(typePart, genericBlockStart, assemblyPart)
                : ParseLeaf(typePart.Trim(), assemblyPart);
   }

   static TypeIdentifier ParseLeaf(string typeName, string assemblyName)
   {
      if(IsReservedMappedAssembly(assemblyName))
         return new MappedTypeIdentifier(ParseMappedGuid(typeName));

      return new StableLeafTypeIdentifier(typeName, ShortAssemblyName(assemblyName));
   }

   static TypeIdentifier ParseGeneric(string typePart, int genericBlockStart, string assemblyName)
   {
      var typeName = typePart[..genericBlockStart].Trim();
      var argumentBlock = typePart[genericBlockStart..];
      var arguments = SplitGenericArguments(argumentBlock).Select(Parse).ToArray();

      if(IsReservedMappedAssembly(assemblyName))
         return new MappedGenericTypeIdentifier(ParseMappedGuid(typeName), arguments);

      return new StableGenericTypeIdentifier(typeName, ShortAssemblyName(assemblyName), arguments);
   }

   // The type and the assembly are separated by the first comma that is not nested inside a generic argument
   // bracket group — every other comma (between generic arguments, or inside an assembly's ", Version=..."
   // qualifiers) sits after it.
   static (string typePart, string assemblyPart) SplitTypeAndAssembly(string assemblyQualifiedName)
   {
      var splitIndex = IndexOfTopLevelComma(assemblyQualifiedName);
      if(splitIndex < 0)
         throw new FormatException($"Invalid AssemblyQualifiedName format: \"{assemblyQualifiedName}\".");

      return (assemblyQualifiedName[..splitIndex].Trim(), assemblyQualifiedName[(splitIndex + 1)..].Trim());
   }

   // A trailing "[]", "[,]", "[,,]"... is one array dimension. Returns false when the type part does not end in
   // such a suffix — including the closing "]]" of a generic, whose final bracket group contains the argument
   // text rather than only commas.
   static bool TryPeelTrailingArraySuffix(string typePart, out string elementTypePart, out int rank)
   {
      elementTypePart = typePart;
      rank = 0;
      if(!typePart.EndsWith(']'))
         return false;

      var lastGroupStart = typePart.LastIndexOf('[');
      var insideLastGroup = typePart[(lastGroupStart + 1)..^1];
      if(insideLastGroup.Any(c => c != ','))
         return false;

      elementTypePart = typePart[..lastGroupStart];
      rank = insideLastGroup.Length + 1;
      return true;
   }

   // The generic argument block is "[[arg],[arg],...]". Drop the outer brackets, split the inside at its
   // top-level commas, and unwrap each "[arg]" — leaving each argument as a full assembly-qualified name.
   static IEnumerable<string> SplitGenericArguments(string argumentBlock)
   {
      var inside = argumentBlock[1..^1];
      return SplitAtTopLevelCommas(inside).Select(argument => Unwrap(argument.Trim()));
   }

   static string Unwrap(string bracketedArgument) => bracketedArgument[1..^1];

   // The first comma at bracket depth zero, or -1 if there is none.
   static int IndexOfTopLevelComma(string s)
   {
      var depth = 0;
      for(var i = 0; i < s.Length; i++)
         switch(s[i])
         {
            case '[': depth++; break;
            case ']': depth--; break;
            case ',' when depth == 0: return i;
         }

      return -1;
   }

   // Splits at every comma at bracket depth zero, leaving nested generic arguments intact.
   static IEnumerable<string> SplitAtTopLevelCommas(string s)
   {
      var parts = new List<string>();
      var depth = 0;
      var partStart = 0;
      for(var i = 0; i < s.Length; i++)
         switch(s[i])
         {
            case '[': depth++; break;
            case ']': depth--; break;
            case ',' when depth == 0:
               parts.Add(s[partStart..i]);
               partStart = i + 1;
               break;
         }

      parts.Add(s[partStart..]);
      return parts;
   }

   // The reserved literal "0" in the assembly position is the authoritative discriminator for a mapped component.
   // A GUID-shaped type name in a real assembly is just a type that happens to look like a GUID — it stays named.
   static bool IsReservedMappedAssembly(string assemblyName) => assemblyName == "0";

   // In the reserved-"0" assembly the type name must be a canonical dashed RFC-4122 GUID. "D" is exactly the
   // 8-4-4-4-12 dashed form, so a real type name or a dash-less 'N'-format GUID is rejected rather than silently
   // accepted — the canonical mapped string is the only thing that classifies as mapped.
   static Guid ParseMappedGuid(string typeName) =>
      Guid.TryParseExact(typeName, "D", out var guid)
         ? guid
         : throw new FormatException($"The reserved assembly \"0\" requires a dashed RFC-4122 GUID type name, but got: \"{typeName}\".");

   // Strips the Version/Culture/PublicKeyToken qualifiers, leaving only the short assembly name. Doing this on read
   // makes identity stable across runtime upgrades, and collapses a full AssemblyQualifiedName and its short form
   // to one canonical string.
   static string ShortAssemblyName(string assemblyName)
   {
      var commaIndex = assemblyName.IndexOf(',', StringComparison.Ordinal);
      return commaIndex >= 0 ? assemblyName[..commaIndex].Trim() : assemblyName;
   }
}
