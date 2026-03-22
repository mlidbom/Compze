namespace Compze.TypeIdentifiers;

/// <summary>
/// Parses .NET <c>AssemblyQualifiedName</c>-format strings into a typed tree of components.
/// Each subtype of <see cref="ParsedTypeName"/> owns parsing for its own grammar production.
/// </summary>
static class TypeNameParser
{
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
      /// This is the dispatch point: splits the component from its assembly name, determines whether it's
      /// a leaf or generic, and delegates to the appropriate subtype.
      /// </summary>
      internal static ParsedTypeName Parse(string assemblyQualifiedName)
      {
         var (typePartWithArraySuffix, assemblyName) = SplitFirstTopLevelComma(assemblyQualifiedName);

         var typePart = typePartWithArraySuffix;
         string? arraySuffix = null;

         // Check for trailing array suffix (e.g. "List`1[[...]][]" or "MyType[]")
         if(typePart.EndsWith(']'))
         {
            var trailingSuffixStart = FindTrailingArraySuffixStart(typePart);
            if(trailingSuffixStart >= 0)
            {
               arraySuffix = typePart[trailingSuffixStart..];
               typePart = typePart[..trailingSuffixStart];
            }
         }

         // If the remaining type part contains a generic argument block, it's a generic type
         var firstBracket = typePart.IndexOf('[');
         if(firstBracket >= 0 && firstBracket + 1 < typePart.Length && typePart[firstBracket + 1] == '[')
            return ParsedGenericTypeName.ParseFromParts(typePart, assemblyName, arraySuffix);

         // Otherwise it's a leaf (possibly with an array suffix already extracted above)
         return new ParsedLeafTypeName(typePart.Trim(), assemblyName, arraySuffix);
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
      /// Parses a generic type from its already-split parts: the type portion (e.g. "List`1[[MyType, Asm]]"),
      /// the assembly name, and any trailing array suffix.
      /// </summary>
      internal static ParsedGenericTypeName ParseFromParts(string typePart, string assemblyName, string? arraySuffix)
      {
         // TypeName is everything before the first '['
         var firstBracket = typePart.IndexOf('[');
         var typeName = typePart[..firstBracket].Trim();

         // The argument block is the rest: "[[arg1],[arg2]]"
         var argumentBlock = typePart[firstBracket..];

         var arguments = SplitGenericArguments(argumentBlock);
         var parsedArguments = arguments.Select(ParsedTypeName.Parse).ToArray();

         return new ParsedGenericTypeName(typeName, assemblyName, parsedArguments, arraySuffix);
      }
   }

   /// <summary>
   /// Splits a generic argument block like <c>[[arg1],[arg2]]</c> into individual argument strings.
   /// Each argument is the content between matched <c>[</c>...<c>]</c> pairs inside the outer brackets.
   /// </summary>
   static string[] SplitGenericArguments(string argumentBlock)
   {
      // Strip the outer [ and ]
      var inner = argumentBlock[1..^1];

      var arguments = new List<string>();
      var position = 0;
      while(position < inner.Length)
      {
         // Skip whitespace and commas between arguments
         while(position < inner.Length && (inner[position] == ',' || char.IsWhiteSpace(inner[position])))
            position++;

         if(position >= inner.Length)
            break;

         // Expect '[' starting an argument
         if(inner[position] != '[')
            throw new FormatException($"Expected '[' at position {position} in argument block: \"{argumentBlock}\"");

         // Find the matching ']' for this argument
         var matchingClose = FindMatchingBracket(inner, position);
         // The argument content is between the brackets (exclusive)
         arguments.Add(inner[(position + 1)..matchingClose]);
         position = matchingClose + 1;
      }

      return [.. arguments];
   }

   /// <summary>
   /// Splits an <c>AssemblyQualifiedName</c>-format string at the first top-level comma
   /// (i.e. the first comma not inside any brackets). Returns (typePart, assemblyName).
   /// The assembly name portion may include version, culture, and public key token.
   /// </summary>
   static (string TypePart, string AssemblyName) SplitFirstTopLevelComma(string input)
   {
      var depth = 0;
      for(var i = 0; i < input.Length; i++)
      {
         switch(input[i])
         {
            case '[': depth++; break;
            case ']': depth--; break;
            case ',' when depth == 0:
               return (input[..i].Trim(), input[(i + 1)..].Trim());
         }
      }

      throw new FormatException($"No top-level comma found in: \"{input}\"");
   }

   /// <summary>
   /// Given a string and the position of an opening '[', returns the position of the matching ']'.
   /// </summary>
   static int FindMatchingBracket(string input, int openPosition)
   {
      var depth = 0;
      for(var i = openPosition; i < input.Length; i++)
      {
         switch(input[i])
         {
            case '[': depth++; break;
            case ']':
               depth--;
               if(depth == 0)
                  return i;
               break;
         }
      }

      throw new FormatException($"Unmatched '[' at position {openPosition} in: \"{input}\"");
   }

   /// <summary>
   /// Finds the start of a trailing array suffix (e.g. "[]", "[,]") at the end of a type part.
   /// Returns the index where the suffix starts, or -1 if no trailing array suffix.
   /// An array suffix is distinguished from generic args by containing only commas between brackets.
   /// </summary>
   static int FindTrailingArraySuffixStart(string typePart)
   {
      // Walk backwards from the end. The suffix is the last [...] where the content is only commas.
      if(!typePart.EndsWith(']'))
         return -1;

      var closePos = typePart.Length - 1;
      // Find the matching '['
      var openPos = closePos;
      while(openPos > 0 && typePart[openPos] != '[')
         openPos--;

      if(openPos == 0)
         return -1;

      // Check if the content between brackets is only commas (array suffix) vs containing actual types
      var content = typePart[(openPos + 1)..closePos];
      if(content.Length == 0 || content.All(c => c == ','))
         return openPos;

      return -1;
   }
}
