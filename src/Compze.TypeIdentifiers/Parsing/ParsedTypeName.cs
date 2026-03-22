using System.Text.RegularExpressions;

namespace Compze.TypeIdentifiers.Parsing;

/// <summary>
/// A parsed component of an <c>AssemblyQualifiedName</c>-format string.
/// Subtypes distinguish leaf components from generic components.
/// </summary>
abstract partial class ParsedTypeName
{
   /// <summary>The type name portion (namespace-qualified). For generics, includes the arity suffix (e.g. "`1").</summary>
   public string TypeName { get; }

   /// <summary>The assembly name. "0" for mapped types.</summary>
   public string AssemblyName { get; }

   /// <summary>Array suffix (e.g. "[]", "[,]"). Null for non-array types.</summary>
   public string? ArraySuffix { get; }

   protected ParsedTypeName(string typeName, string assemblyName, string? arraySuffix)
   {
      TypeName = typeName;
      AssemblyName = assemblyName;
      ArraySuffix = arraySuffix;
   }

   /// <summary>Reconstructs the <c>AssemblyQualifiedName</c>-format string from this parsed tree.</summary>
   public abstract string ToAssemblyQualifiedNameString();

   public override string ToString() => ToAssemblyQualifiedNameString();

   /// <summary>
   /// Parses an <c>AssemblyQualifiedName</c>-format string into the correct <see cref="ParsedTypeName"/> subtype.
   /// Splits the component from its assembly name, extracts any array suffix, determines whether it's
   /// a leaf or generic, and delegates to the appropriate subtype.
   /// </summary>
   internal static ParsedTypeName Parse(string assemblyQualifiedName)
   {
      var (type, assembly) = TypeAndAssemblyParts(assemblyQualifiedName);

      // Extract trailing array suffix if present (e.g. "[]", "[,]")
      string? arraySuffix = null;
      var arraySuffixMatch = TrailingArraySuffixPattern().Match(type);
      if(arraySuffixMatch.Success)
      {
         arraySuffix = arraySuffixMatch.Groups[1].Value;
         type = type[..arraySuffixMatch.Index];
      }

      // Check if the remaining type part is a generic (contains "[[")
      var genericMatch = GenericTypePartPattern().Match(type);
      if(genericMatch.Success)
         return ParsedGenericTypeName.ParseFromParts(genericMatch, assembly, arraySuffix);

      return new ParsedLeafTypeName(type.Trim(), assembly, arraySuffix);
   }

   static (string typePart, string assemblyPart) TypeAndAssemblyParts(string assemblyQualifiedName)
   {
      var typeAndAssemblyPart = TypeAndAssemblyPartsPattern().Match(assemblyQualifiedName);
      if(!typeAndAssemblyPart.Success)
         throw new FormatException($"Invalid AssemblyQualifiedName format: \"{assemblyQualifiedName}\"");

      return (typePart: typeAndAssemblyPart.Groups[1].Value.Trim(),
              assemblyPart: typeAndAssemblyPart.Groups[2].Value.Trim());
   }

   // Splits "TypePart, AssemblyName" at the first comma not inside brackets.
   // Group 1 = type part (may contain balanced bracket groups), Group 2 = assembly name.
   // Uses (?(D),|(?!)) to allow commas inside brackets but not at bracket depth 0.
   [GeneratedRegex(@"^\s*((?:[^\[\],]|\[(?<D>)|\](?<-D>)|(?(D),|(?!)))*(?(D)(?!)))\s*,\s*(.+)$")]
   private static partial Regex TypeAndAssemblyPartsPattern();

   // Matches a trailing array suffix like [], [,], [,,] at the end of a type part.
   [GeneratedRegex(@"(\[,*\])$")]
   private static partial Regex TrailingArraySuffixPattern();

   // Splits a type part into the type name and the generic argument block.
   // Group 1 = type name (everything before "[["), Group 2 = the argument block "[[...]]".
   [GeneratedRegex(@"^(.+?)(\[\[.+\]\])$")]
   private static partial Regex GenericTypePartPattern();
}
