using System.Text.RegularExpressions;

namespace Compze.TypeIdentifiers.Parsing;

/// <summary>
/// A parsed component of an <c>AssemblyQualifiedName</c>-format string.
/// Subtypes represent distinct grammar productions: leaf, mapped leaf, generic, mapped generic, and array.
/// </summary>
abstract partial class ParsedTypeName
{
   /// <summary>The type portion of the AQN, before ", AssemblyName".</summary>
   internal abstract string TypePart { get; }

   /// <summary>The assembly name portion of the AQN.</summary>
   internal abstract string AssemblyPart { get; }

   /// <summary>Reconstructs the <c>AssemblyQualifiedName</c>-format string from this parsed tree.</summary>
   public string ToAssemblyQualifiedNameString() => $"{TypePart}, {AssemblyPart}";

   public override string ToString() => ToAssemblyQualifiedNameString();

   /// <summary>
   /// Parses an <c>AssemblyQualifiedName</c>-format string into the correct <see cref="ParsedTypeName"/> subtype.
   /// Detects mapped types by recognizing GUID type names, and wraps array types as <see cref="ParsedArrayTypeName"/>.
   /// </summary>
   internal static ParsedTypeName Parse(string assemblyQualifiedName)
   {
      var (typePart, assemblyPart) = SplitTypeAndAssembly(assemblyQualifiedName);

      // Extract trailing array suffix if present (e.g. "[]", "[,]")
      string? arraySuffix = null;
      var arraySuffixMatch = TrailingArraySuffixPattern().Match(typePart);
      if(arraySuffixMatch.Success)
      {
         arraySuffix = arraySuffixMatch.Groups[1].Value;
         typePart = typePart[..arraySuffixMatch.Index];
      }

      // Parse the inner type (without array suffix)
      ParsedTypeName inner;
      var genericMatch = GenericTypePartPattern().Match(typePart);
      if(genericMatch.Success)
         inner = ParseGenericFromParts(genericMatch, assemblyPart);
      else
         inner = ParseLeafFromParts(typePart.Trim(), assemblyPart);

      // Wrap in array if needed
      if(arraySuffix != null)
      {
         var rank = arraySuffix.Count(c => c == ',') + 1;
         inner = new ParsedArrayTypeName(inner, rank);
      }

      return inner;
   }

   static ParsedTypeName ParseLeafFromParts(string typeName, string assemblyName)
   {
      if(Guid.TryParse(typeName, out var guid))
         return new ParsedMappedLeafTypeName(guid);

      return new ParsedLeafTypeName(typeName, assemblyName);
   }

   static ParsedTypeName ParseGenericFromParts(Match genericMatch, string assemblyName)
   {
      var typeName = genericMatch.Groups[1].Value.Trim();
      var argumentBlock = genericMatch.Groups[2].Value;
      var innerBlock = argumentBlock[1..^1];

      var parsedArguments = GenericArgumentPattern()
         .Matches(innerBlock)
         .Select(m => Parse(m.Groups[1].Value))
         .ToArray();

      if(Guid.TryParse(typeName, out var guid))
         return new ParsedMappedGenericTypeName(guid, parsedArguments);

      return new ParsedGenericTypeName(typeName, assemblyName, parsedArguments);
   }

   static (string typePart, string assemblyPart) SplitTypeAndAssembly(string assemblyQualifiedName)
   {
      var match = TypeAndAssemblyPartsPattern().Match(assemblyQualifiedName);
      if(!match.Success)
         throw new FormatException($"Invalid AssemblyQualifiedName format: \"{assemblyQualifiedName}\"");

      return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
   }

   // Splits "TypePart, AssemblyName" at the first comma not inside brackets.
   // Uses (?(D),|(?!)) to allow commas inside brackets but not at bracket depth 0.
   [GeneratedRegex(@"^\s*((?:[^\[\],]|\[(?<D>)|\](?<-D>)|(?(D),|(?!)))*(?(D)(?!)))\s*,\s*(.+)$")]
   private static partial Regex TypeAndAssemblyPartsPattern();

   // Matches a trailing array suffix like [], [,], [,,] at the end of a type part.
   [GeneratedRegex(@"(\[,*\])$")]
   private static partial Regex TrailingArraySuffixPattern();

   // Splits a type part into the type name and the generic argument block.
   [GeneratedRegex(@"^(.+?)(\[\[.+\]\])$")]
   private static partial Regex GenericTypePartPattern();

   // Matches each individual "[argument]" inside a generic argument block.
   // Uses balancing groups to handle nested brackets within each argument.
   [GeneratedRegex(@"\[((?:[^\[\]]|\[(?<D>)|\](?<-D>))*(?(D)(?!)))\]")]
   private static partial Regex GenericArgumentPattern();
}
