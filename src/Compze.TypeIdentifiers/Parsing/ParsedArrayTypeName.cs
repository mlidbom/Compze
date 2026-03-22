namespace Compze.TypeIdentifiers.Parsing;

/// <summary>An array type wrapping an element type: <c>Element[], AssemblyName</c> or <c>Element[,], AssemblyName</c>.</summary>
sealed class ParsedArrayTypeName(ParsedTypeName element, int rank) : ParsedTypeName
{
   public ParsedTypeName Element { get; } = element;
   public int Rank { get; } = rank;

   string ArraySuffix => Rank == 1 ? "[]" : $"[{new string(',', Rank - 1)}]";

   internal override string TypePart => $"{Element.TypePart}{ArraySuffix}";
   internal override string AssemblyPart => Element.AssemblyPart;
}
