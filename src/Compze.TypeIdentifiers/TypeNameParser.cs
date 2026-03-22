using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Parses .NET <c>AssemblyQualifiedName</c>-format strings into a typed tree of components.
/// Each subtype of <see cref="ParsedTypeName"/> owns parsing for its own grammar production.
/// Uses compiled regular expressions with .NET balancing groups for bracket matching.
/// </summary>
static partial class TypeNameParser
{
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

   // Matches each individual "[argument]" inside a generic argument block.
   // Uses balancing groups to handle nested brackets within each argument.
   [GeneratedRegex(@"\[((?:[^\[\]]|\[(?<D>)|\](?<-D>))*(?(D)(?!)))\]")]
   private static partial Regex GenericArgumentPattern();

   /// <summary>
   /// A parsed component of an <c>AssemblyQualifiedName</c>-format string.
   /// Subtypes distinguish leaf components from generic components.
   /// </summary>
   internal abstract class ParsedTypeName
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
         var (typePart, assemblyName) = TypeAndAssemblyParts(assemblyQualifiedName);

         // Extract trailing array suffix if present (e.g. "[]", "[,]")
         string? arraySuffix = null;
         var arraySuffixMatch = TrailingArraySuffixPattern().Match(typePart);
         if(arraySuffixMatch.Success)
         {
            arraySuffix = arraySuffixMatch.Groups[1].Value;
            typePart = typePart[..arraySuffixMatch.Index];
         }

         // Check if the remaining type part is a generic (contains "[[")
         var genericMatch = GenericTypePartPattern().Match(typePart);
         if(genericMatch.Success)
            return ParsedGenericTypeName.ParseFromParts(genericMatch, assemblyName, arraySuffix);

         return new ParsedLeafTypeName(typePart.Trim(), assemblyName, arraySuffix);
      }

      static (string typePart, string assemblyName) TypeAndAssemblyParts(string assemblyQualifiedName)
      {
         var typeAndAssemblyPart = TypeAndAssemblyPartsPattern().Match(assemblyQualifiedName);
         if(!typeAndAssemblyPart.Success)
            throw new FormatException($"Invalid AssemblyQualifiedName format: \"{assemblyQualifiedName}\"");

         var typePart = typeAndAssemblyPart.Groups[1].Value.Trim();
         var assemblyName = typeAndAssemblyPart.Groups[2].Value.Trim();
         return (typePart, assemblyName);
      }
   }

   /// <summary>A non-generic component: <c>TypeName ArraySuffix?, AssemblyName</c>.</summary>
   internal sealed class ParsedLeafTypeName(string typeName, string assemblyName, string? arraySuffix = null)
      : ParsedTypeName(typeName, assemblyName, arraySuffix)
   {
      public override string ToAssemblyQualifiedNameString() => $"{TypeName}{ArraySuffix}, {AssemblyName}";
   }

   /// <summary>A generic component: <c>TypeName[[ arg1 ],[ arg2 ]] ArraySuffix?, AssemblyName</c>.</summary>
   internal sealed class ParsedGenericTypeName : ParsedTypeName
   {
      public ParsedTypeName[] TypeArguments { get; }

      internal ParsedGenericTypeName(string typeName, string assemblyName, ParsedTypeName[] typeArguments, string? arraySuffix = null)
         : base(typeName, assemblyName, arraySuffix) => TypeArguments = typeArguments;

      public override string ToAssemblyQualifiedNameString()
      {
         var argsString = string.Join(",", TypeArguments.Select(arg => $"[{arg.ToAssemblyQualifiedNameString()}]"));
         return $"{TypeName}[{argsString}]{ArraySuffix}, {AssemblyName}";
      }

      /// <summary>
      /// Parses a generic type from a regex match that already split the type name from the argument block.
      /// </summary>
      internal static ParsedGenericTypeName ParseFromParts(Match genericMatch, string assemblyName, string? arraySuffix)
      {
         var typeName = genericMatch.Groups[1].Value.Trim();
         var argumentBlock = genericMatch.Groups[2].Value;

         // Strip the outer brackets: "[[arg1],[arg2]]" -> "[arg1],[arg2]"
         var innerBlock = argumentBlock[1..^1];

         var parsedArguments = GenericArgumentPattern()
            .Matches(innerBlock)
            .Select(m => ParsedTypeName.Parse(m.Groups[1].Value))
            .ToArray();

         return new ParsedGenericTypeName(typeName, assemblyName, parsedArguments, arraySuffix);
      }
   }
}
