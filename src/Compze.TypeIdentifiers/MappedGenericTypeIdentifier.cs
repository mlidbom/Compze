namespace Compze.TypeIdentifiers;

/// <summary>A mapped generic type identified by GUID: <c>Guid[[ arg1 ],[ arg2 ]], 0</c>.</summary>
sealed class MappedGenericTypeIdentifier(Guid guidValue, TypeIdentifier[] typeArguments) : TypeIdentifier
{
   public Guid GuidValue { get; } = guidValue;
   public TypeIdentifier[] TypeArguments { get; } = typeArguments;

   internal override string TypePart
   {
      get
      {
         var argsString = string.Join(",", TypeArguments.Select(arg => $"[{arg.StringRepresentation}]"));
         return $"{GuidValue}[{argsString}]";
      }
   }

   internal override string AssemblyPart => "0";

   internal override Type ResolveToType(ITypeMappingLookup lookup)
   {
      var openGenericType = lookup.GetOpenGenericType(GuidValue);
      var typeArgs = TypeArguments.Select(arg => arg.ResolveToType(lookup)).ToArray();
      return openGenericType.MakeGenericType(typeArgs);
   }

   internal override TypeIdentifier TransformToPersisted(ITypeMappingLookup lookup) => this;
}
