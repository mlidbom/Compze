namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A stable non-generic type: <c>TypeName, AssemblyName</c>.</summary>
sealed class ParsedLeafTypeName(string typeName, string assemblyName) : ParsedTypeName
{
   public string TypeName { get; } = typeName;
   public string AssemblyName { get; } = assemblyName;

   internal override string TypePart => TypeName;
   internal override string AssemblyPart => AssemblyName;
}
