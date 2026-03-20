namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

/// <summary>
/// Parses .NET <c>AssemblyQualifiedName</c>-format strings into a tree of components.
/// Handles generics, arrays, nested generics, and the mapped format where
/// type names are GUIDs and assembly names are "0".
/// </summary>
static class TypeNameParser
{
   /// <summary>
   /// Represents a parsed component of an <c>AssemblyQualifiedName</c>-format string.
   /// For a simple type: TypeName + AssemblyName.
   /// For a generic type: TypeName (with arity suffix) + AssemblyName + TypeArguments.
   /// For an array type: the element type is parsed, and array suffix (e.g. "[]", "[,]") is part of the TypeName.
   /// </summary>
   internal sealed class ParsedTypeName(string typeName, string assemblyName, ParsedTypeName[]? typeArguments = null, string? arraySuffix = null)
   {
      /// <summary>The type name portion (namespace-qualified). For generics, includes the arity suffix (e.g. "`1"). Does NOT include array brackets.</summary>
      public string TypeName { get; } = typeName;

      /// <summary>The assembly name. "0" for mapped types.</summary>
      public string AssemblyName { get; } = assemblyName;

      /// <summary>Type arguments for generic types. Null for non-generic types.</summary>
      public ParsedTypeName[]? TypeArguments { get; } = typeArguments;

      /// <summary>Array suffix (e.g. "[]", "[,]"). Null for non-array types.</summary>
      public string? ArraySuffix { get; } = arraySuffix;

      /// <summary>Reconstructs the <c>AssemblyQualifiedName</c>-format string from this parsed tree.</summary>
      public string ToAssemblyQualifiedNameString()
      {
         if(TypeArguments is { Length: > 0 })
         {
            var argsString = string.Join(",", TypeArguments.Select(arg => $"[{arg.ToAssemblyQualifiedNameString()}]"));
            return $"{TypeName}[{argsString}]{ArraySuffix}, {AssemblyName}";
         }

         return $"{TypeName}{ArraySuffix}, {AssemblyName}";
      }

      public override string ToString() => ToAssemblyQualifiedNameString();
   }

   /// <summary>
   /// Parses an <c>AssemblyQualifiedName</c>-format string into a <see cref="ParsedTypeName"/> tree.
   /// </summary>
   internal static ParsedTypeName Parse(string assemblyQualifiedName)
   {
      var index = 0;
      var result = ParseComponent(assemblyQualifiedName, ref index);
      return result;
   }

   static ParsedTypeName ParseComponent(string input, ref int index)
   {
      var typeName = ParseTypeName(input, ref index);

      // Array suffix before generic args (non-generic arrays like MyType[])
      var arraySuffix = TryParseArraySuffix(input, ref index);

      ParsedTypeName[]? typeArguments = null;
      if(index < input.Length && input[index] == '[' && !IsArraySuffix(input, index))
      {
         typeArguments = ParseTypeArguments(input, ref index);
      }

      // Array suffix after generic args (generic arrays like List`1[[...]][])
      arraySuffix ??= TryParseArraySuffix(input, ref index);

      SkipWhitespace(input, ref index);
      ExpectChar(input, ref index, ',');
      SkipWhitespace(input, ref index);

      var assemblyName = ParseAssemblyName(input, ref index);

      return new ParsedTypeName(typeName, assemblyName, typeArguments, arraySuffix);
   }

   static string ParseTypeName(string input, ref int index)
   {
      var start = index;
      // Type name continues until we hit '[' (generic args or array), ',' (assembly separator), or ']' (end of nested type arg)
      while(index < input.Length)
      {
         var c = input[index];
         if(c is '[' or ',' or ']')
            break;
         index++;
      }

      return input[start..index].Trim();
   }

   static bool IsArraySuffix(string input, int index)
   {
      // An array suffix is "[" followed by "," or "]" (e.g. "[]", "[,]", "[,,]").
      // Generic type arguments start with "[[" — the double bracket distinguishes them.
      if(index + 1 >= input.Length) return false;
      var next = input[index + 1];
      return next is ']' or ',';
   }

   static string? TryParseArraySuffix(string input, ref int index)
   {
      if(index >= input.Length || input[index] != '[') return null;
      if(!IsArraySuffix(input, index)) return null;

      var start = index;
      index++; // skip '['
      while(index < input.Length && input[index] != ']')
         index++;

      if(index < input.Length)
         index++; // skip ']'

      return input[start..index];
   }

   static ParsedTypeName[] ParseTypeArguments(string input, ref int index)
   {
      ExpectChar(input, ref index, '[');

      var arguments = new List<ParsedTypeName>();
      while(true)
      {
         SkipWhitespace(input, ref index);
         ExpectChar(input, ref index, '[');
         arguments.Add(ParseComponent(input, ref index));
         SkipWhitespace(input, ref index);
         ExpectChar(input, ref index, ']');
         SkipWhitespace(input, ref index);

         if(index < input.Length && input[index] == ',')
         {
            index++; // skip ',' between arguments
            continue;
         }

         break;
      }

      ExpectChar(input, ref index, ']');
      return [.. arguments];
   }

   static string ParseAssemblyName(string input, ref int index)
   {
      var start = index;
      // Assembly name continues until end of string or ']' (end of nested type arg)
      while(index < input.Length && input[index] != ']')
         index++;

      return input[start..index].Trim();
   }

   static void SkipWhitespace(string input, ref int index)
   {
      while(index < input.Length && char.IsWhiteSpace(input[index]))
         index++;
   }

   static void ExpectChar(string input, ref int index, char expected)
   {
      if(index >= input.Length || input[index] != expected)
      {
         var actual = index < input.Length ? $"'{input[index]}'" : "end of string";
         throw new FormatException($"Expected '{expected}' at position {index} but found {actual} in: \"{input}\"");
      }

      index++;
   }
}
