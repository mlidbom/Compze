using Compze.Internals.SystemCE.LinqCE;

namespace Compze.xUnitMatrix._private;

///<summary>
/// A line of a <see cref="MatrixConfigurationFile"/> that lists combinations: a value of each of the matrix's dimensions, in
/// dimension order and separated by <c>:</c>, such as <c>SqliteMemory:Autofac</c>.<br/>
/// A <c>*</c> in place of a value stands for every value of its dimension, so the line lists every combination of the values
/// it gives: <c>*:Autofac</c> pairs each persistence layer with Autofac.
///</summary>
class MatrixConfigurationFileLine
{
   const string Wildcard = "*";
   readonly MatrixConfigurationFile _file;
   readonly int _lineNumber;
   readonly string _text;

   /// <summary>
   /// The line numbered <paramref name="lineNumber"/> in <paramref name="file"/>, whose <paramref name="text"/> lists combinations.<br/>
   /// Throws <see cref="InvalidMatrixConfigurationException"/> when the text does not give exactly one value or <c>*</c> for
   /// each dimension, or gives a name that is not a value of its dimension.
   /// </summary>
   public MatrixConfigurationFileLine(MatrixConfigurationFile file, int lineNumber, string text)
   {
      _file = file;
      _lineNumber = lineNumber;
      _text = text;
      Combinations = ValuesGivenForEachDimension()
                    .CartesianProduct()
                    .Select(MatrixCombination.FromDimensionValues)
                    .ToList();
   }

   /// <summary>The combinations the line lists: one, or with wildcards, every combination of the values it gives.</summary>
   public IReadOnlyList<MatrixCombination> Combinations { get; }

   IReadOnlyList<IReadOnlyList<Enum>> ValuesGivenForEachDimension()
   {
      var dimensions = _file.Dimensions;
      var valueNamesOrWildcards = _text.Split(MatrixCombination.Separator).Select(it => it.Trim()).ToList();
      if(valueNamesOrWildcards.Count != dimensions.Count)
         throw Invalid($"has {Quantity(valueNamesOrWildcards.Count, "value")}, but the matrix has {Quantity(dimensions.Count, "dimension")}: {dimensions}.");

      return valueNamesOrWildcards.Select((valueNameOrWildcard, dimensionIndex) => ValuesGivenFor(dimensions[dimensionIndex], valueNameOrWildcard))
                                  .ToList();
   }

   IReadOnlyList<Enum> ValuesGivenFor(MatrixDimension dimension, string valueNameOrWildcard) =>
      valueNameOrWildcard == Wildcard
         ? dimension.AllValues
         : [dimension.ValueNamed(valueNameOrWildcard) ?? throw Invalid($"gives \"{valueNameOrWildcard}\" for the dimension {dimension}, which has no value of that name. Its values are {dimension.ValueNames}.")];

   InvalidMatrixConfigurationException Invalid(string whatIsWrong) =>
      new($"Line {_lineNumber} of the matrix configuration file {_file.FileName}, \"{_text}\", {whatIsWrong}");

   static string Quantity(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
