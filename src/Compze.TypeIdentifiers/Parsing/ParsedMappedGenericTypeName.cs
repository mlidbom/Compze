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
}
