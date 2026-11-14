namespace Compze.xUnitMatrix._private;

///<summary>
/// One dimension of a test matrix: an enum type whose members are the dimension's values, such as a persistence layer enum
/// whose members are the persistence layers a test runs against.
///</summary>
sealed record MatrixDimension(Type EnumType)
{
   /// <summary>Every value of the dimension, in the order the enum declares them.</summary>
   public IReadOnlyList<Enum> AllValues => Enum.GetValues(EnumType).Cast<Enum>().ToList();

   /// <summary>The value whose name is exactly <paramref name="name"/>, or <see langword="null"/> when the dimension has no value of that name.</summary>
   /// <remarks>
   /// Exactly, because <see cref="Enum.Parse(Type, string)"/> accepts more than the names: a number, which yields a value the enum
   /// may not define, and a comma-separated list of names, which yields their values combined bit by bit, possibly into another
   /// of the enum's values.
   /// </remarks>
   public Enum? ValueNamed(string name) => Enum.GetNames(EnumType).Contains(name) ? (Enum)Enum.Parse(EnumType, name) : null;

   /// <summary>The names of the dimension's values, comma-separated, for messages.</summary>
   public string ValueNames => string.Join(", ", Enum.GetNames(EnumType));

   /// <summary>The name of the dimension's enum type.</summary>
   public override string ToString() => EnumType.Name;
}
