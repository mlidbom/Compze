namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A stable generic type: <c>TypeName[[ arg1 ],[ arg2 ]], AssemblyName</c>.</summary>
sealed class ParsedGenericTypeName(string typeName, string assemblyName, ParsedTypeName[] typeArguments) : ParsedTypeName
{
   public string TypeName { get; } = typeName;
   public string AssemblyName { get; } = assemblyName;
   public ParsedTypeName[] TypeArguments { get; } = typeArguments;

   internal override string TypePart
   {
      get
      {
         var argsString = string.Join(",", TypeArguments.Select(arg => $"[{arg.ToAssemblyQualifiedNameString()}]"));
         return $"{TypeName}[{argsString}]";
      }
   }

   internal override string AssemblyPart => AssemblyName;

   internal override Type ResolveToType(ITypeMappingLookup lookup)
   {
      var openGenericAqn = $"{TypeName}, {AssemblyName}";
      var openGenericType = Type.GetType(openGenericAqn)
         ?? throw new InvalidOperationException($"Could not resolve stable open generic type: {openGenericAqn}");

      var typeArgs = TypeArguments.Select(arg => arg.ResolveToType(lookup)).ToArray();
      return openGenericType.MakeGenericType(typeArgs);
   }

   internal override ParsedTypeName TransformToPersisted(ITypeMappingLookup lookup)
   {
      var transformedArgs = TypeArguments.Select(arg => arg.TransformToPersisted(lookup)).ToArray();

      var openGenericAqn = $"{TypeName}, {AssemblyName}";
      var openGenericType = Type.GetType(openGenericAqn);

      if(openGenericType != null && lookup.TryGetOpenGenericGuid(openGenericType, out var guid))
         return new ParsedMappedGenericTypeName(guid, transformedArgs);

      if(lookup.IsStableAssembly(AssemblyName))
         return new ParsedGenericTypeName(TypeName, AssemblyName, transformedArgs);

      throw new InvalidOperationException(
         $"Open generic type '{TypeName}' from assembly '{AssemblyName}' is not mapped and its assembly is not registered as stable.");
   }
}
