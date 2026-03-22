namespace Compze.TypeIdentifiers.Parsing;

/// <summary>A non-generic component: <c>TypeName ArraySuffix?, AssemblyName</c>.</summary>
sealed class ParsedLeafTypeName(string typeName, string assemblyName, string? arraySuffix = null) : ParsedTypeName(typeName, assemblyName, arraySuffix)
{
   public override string ToAssemblyQualifiedNameString() => $"{TypeName}{ArraySuffix}, {AssemblyName}";
}
