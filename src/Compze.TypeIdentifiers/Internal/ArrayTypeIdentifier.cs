using Compze.TypeIdentifiers.Private;

namespace Compze.TypeIdentifiers.Internal;

/// <summary>An array type wrapping an element type identifier: <c>Element[], AssemblyName</c> or <c>Element[,], AssemblyName</c>.</summary>
sealed class ArrayTypeIdentifier(TypeIdentifier element, int rank) : TypeIdentifier
{
   public TypeIdentifier Element { get; } = element;
   public int Rank { get; } = rank;

   string ArraySuffix => Rank == 1 ? "[]" : $"[{new string(',', Rank - 1)}]";

   internal override string TypePart => $"{Element.TypePart}{ArraySuffix}";
   internal override string AssemblyPart => Element.AssemblyPart;

   internal override Type ResolveToType(ITypeMappingLookup lookup)
   {
      var elementType = Element.ResolveToType(lookup);
      return Rank == 1 ? elementType.MakeArrayType() : elementType.MakeArrayType(Rank);
   }

   internal override TypeIdentifier TransformToPersisted(ITypeMappingLookup lookup) =>
      new ArrayTypeIdentifier(Element.TransformToPersisted(lookup), Rank);
}
