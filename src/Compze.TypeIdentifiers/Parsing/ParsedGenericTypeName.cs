using System.Text.RegularExpressions;

namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A generic component: <c>TypeName[[ arg1 ],[ arg2 ]] ArraySuffix?, AssemblyName</c>.</summary>
sealed partial class ParsedGenericTypeName : ParsedTypeName
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
         .Select(m => Parse(m.Groups[1].Value))
         .ToArray();

      return new ParsedGenericTypeName(typeName, assemblyName, parsedArguments, arraySuffix);
   }

   // Matches each individual "[argument]" inside a generic argument block.
   // Uses balancing groups to handle nested brackets within each argument.
   [GeneratedRegex(@"\[((?:[^\[\]]|\[(?<D>)|\](?<-D>))*(?(D)(?!)))\]")]
   private static partial Regex GenericArgumentPattern();
}
