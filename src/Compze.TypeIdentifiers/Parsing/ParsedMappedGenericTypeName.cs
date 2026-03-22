namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A mapped generic type identified by GUID: <c>Guid[[ arg1 ],[ arg2 ]], 0</c>.</summary>
sealed class ParsedMappedGenericTypeName(Guid guid, ParsedTypeName[] typeArguments) : ParsedTypeName
{
   public Guid Guid { get; } = guid;
   public ParsedTypeName[] TypeArguments { get; } = typeArguments;

   internal override string TypePart
   {
      get
      {
         var argsString = string.Join(",", TypeArguments.Select(arg => $"[{arg.ToAssemblyQualifiedNameString()}]"));
         return $"{Guid}[{argsString}]";
      }
   }

   internal override string AssemblyPart => "0";

   internal override Type ResolveToType(ITypeMappingLookup lookup)
   {
      var openGenericType = lookup.GetOpenGenericType(Guid);
      var typeArgs = TypeArguments.Select(arg => arg.ResolveToType(lookup)).ToArray();
      return openGenericType.MakeGenericType(typeArgs);
   }

   internal override ParsedTypeName TransformToPersisted(ITypeMappingLookup lookup) => this;
}
