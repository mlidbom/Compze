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
}
