namespace Compze.TypeIdentifiers;

/// <summary>
/// A left-to-right recursive-descent scanner over an <c>AssemblyQualifiedName</c>-format string. It walks the
/// ECMA-335 structure once — type name, optional generic argument list, optional array suffixes, assembly name —
/// producing the matching <see cref="TypeIdentifier"/> parse tree. As it goes it normalizes every assembly to its
/// short name (dropping Version/Culture/PublicKeyToken) and classifies each component as mapped or named.
/// </summary>
sealed class Scanner(string text)
{
   readonly string _text = text;
   int _position;

   bool AtEnd => _position >= _text.Length;
   char Current => _text[_position];

   /// <summary>Parses the whole input as one assembly-qualified name, requiring that nothing is left over.</summary>
   internal TypeIdentifier ParseTopLevel()
   {
      var identifier = ParseAssemblyQualifiedName();
      if(!AtEnd)
         throw Invalid("unexpected trailing characters");
      return identifier;
   }

   // An assembly-qualified name is: a type spec, then ", AssemblyName". The type spec carries any array suffixes,
   // so the assembly we read here belongs to the innermost (leaf or generic) component — exactly the part the CLR
   // appends after the final bracket group.
   TypeIdentifier ParseAssemblyQualifiedName()
   {
      var typeName = ParseTypeName();
      var typeArguments = TryParseGenericArgumentList();
      var arrayRanks = ParseArraySuffixes();
      var assemblyName = ParseAssemblyName();

      var component = Classify(typeName, typeArguments, assemblyName);
      return WrapInArrays(component, arrayRanks);
   }

   // The leading type name runs until the first character that starts something else: '[' (a generic list or an
   // array suffix), ',' (the assembly), or ']' (the end of an enclosing generic argument). Backtick arity such as
   // "List`1" is just part of the name.
   string ParseTypeName()
   {
      var start = _position;
      while(!AtEnd && Current is not ('[' or ',' or ']'))
         _position++;

      var name = _text[start.._position].Trim();
      if(name.Length == 0)
         throw Invalid("missing type name");
      return name;
   }

   // A generic argument list is "[[arg],[arg],...]" — a bracket group whose first inner character is itself a '['.
   // A lone "[" instead begins an array suffix, so we only enter here on the "[[" shape. Returns null when the
   // type is non-generic.
   TypeIdentifier[]? TryParseGenericArgumentList()
   {
      if(AtEnd || Current != '[' || !NextStartsAnArgument())
         return null;

      Expect('[');
      var arguments = new List<TypeIdentifier> { ParseBracketedArgument() };
      while(!AtEnd && Current == ',')
      {
         Expect(',');
         arguments.Add(ParseBracketedArgument());
      }
      Expect(']');
      return [.. arguments];
   }

   // Inside the generic list each argument is wrapped in its own "[...]" and is a complete assembly-qualified name.
   TypeIdentifier ParseBracketedArgument()
   {
      Expect('[');
      var argument = ParseAssemblyQualifiedName();
      Expect(']');
      return argument;
   }

   bool NextStartsAnArgument() => _position + 1 < _text.Length && _text[_position + 1] == '[';

   // Each trailing "[]", "[,]", "[,,]"... is one array dimension. The CLR writes the outermost rank last, so the
   // suffixes are returned in inner-to-outer order; the caller wraps from the inside out to match.
   List<int> ParseArraySuffixes()
   {
      var ranks = new List<int>();
      while(!AtEnd && Current == '[')
      {
         Expect('[');
         var rank = 1;
         while(!AtEnd && Current == ',')
         {
            Expect(',');
            rank++;
         }
         Expect(']');
         ranks.Add(rank);
      }
      return ranks;
   }

   // The assembly name is everything up to the ']' that closes an enclosing generic argument, or to the end of the
   // input at the top level. It carries comma-separated qualifiers (Version, Culture, PublicKeyToken); we keep only
   // the short name before the first such comma so identity stays stable across runtime versions.
   string ParseAssemblyName()
   {
      Expect(',');
      var start = _position;
      while(!AtEnd && Current != ']')
         _position++;

      var fullAssembly = _text[start.._position].Trim();
      var shortName = fullAssembly.Split(',', 2)[0].Trim();
      if(shortName.Length == 0)
         throw Invalid("missing assembly name");
      return shortName;
   }

   // The reserved literal "0" assembly is the authoritative marker of a mapped component, and only in combination
   // with a dashed RFC-4122 GUID type name. "0" with any other name is corrupt input; a GUID-shaped name in a real
   // assembly is just a type that happens to look like a GUID and keeps its assembly.
   TypeIdentifier Classify(string typeName, TypeIdentifier[]? typeArguments, string assemblyName)
   {
      if(assemblyName == "0")
      {
         if(!IsDashedGuid(typeName))
            throw Invalid($"reserved assembly \"0\" requires a dashed RFC-4122 GUID type name, but found \"{typeName}\"");

         var guid = Guid.Parse(typeName);
         return typeArguments is null
                   ? new MappedTypeIdentifier(guid)
                   : new MappedGenericTypeIdentifier(guid, typeArguments);
      }

      return typeArguments is null
                ? new StableLeafTypeIdentifier(typeName, assemblyName)
                : new StableGenericTypeIdentifier(typeName, assemblyName, typeArguments);
   }

   static TypeIdentifier WrapInArrays(TypeIdentifier element, List<int> innerToOuterRanks)
   {
      foreach(var rank in innerToOuterRanks)
         element = new ArrayTypeIdentifier(element, rank);
      return element;
   }

   void Expect(char expected)
   {
      if(AtEnd || Current != expected)
         throw Invalid($"expected '{expected}'");
      _position++;
   }

   FormatException Invalid(string reason) =>
      new($"Invalid AssemblyQualifiedName at position {_position} ({reason}): \"{_text}\"");

   // Deliberately stricter than Guid.TryParse: only the canonical dashed 8-4-4-4-12 form counts, so a dash-less
   // 'N'-format GUID in the reserved "0" assembly is rejected as non-canonical rather than silently accepted.
   static bool IsDashedGuid(string value)
   {
      int[] hexRunLengths = [8, 4, 4, 4, 12];
      var groups = value.Split('-');
      return groups.Length == hexRunLengths.Length
          && groups.Zip(hexRunLengths, (group, length) => group.Length == length && group.All(IsHexDigit)).All(matches => matches);
   }

   static bool IsHexDigit(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
}
