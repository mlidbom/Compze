using System.Collections.Concurrent;
using Compze.Internals.SystemCE;

namespace Compze.xUnitMatrix._private;

///<summary>
/// A file listing the combinations a matrix runs, named by its matrix attribute to run those instead of every combination.<br/>
/// Each line lists one combination, or with wildcards several: see <see cref="MatrixConfigurationFileLine"/>. Blank lines
/// and lines starting with <c>#</c> are ignored.<br/>
/// The file is looked up in the test assembly's output directory.
///</summary>
class MatrixConfigurationFile
{
   const char Comment = '#';

   static readonly ConcurrentDictionary<(string FileName, MatrixDimensions Dimensions), IReadOnlyList<MatrixCombination>> CombinationsCache = new();

   MatrixConfigurationFile(string fileName, MatrixDimensions dimensions)
   {
      FileName = fileName;
      Dimensions = dimensions;
   }

   public string FileName { get; }

   /// <summary>The dimensions of the matrix the file lists combinations for: each line gives a value of each, in this order.</summary>
   public MatrixDimensions Dimensions { get; }

   /// <summary>
   /// The combinations the file named <paramref name="fileName"/> lists for a matrix with <paramref name="dimensions"/>, each
   /// once, ordered by name. The file is read once for each set of dimensions it is read for.<br/>
   /// Throws <see cref="InvalidMatrixConfigurationException"/> when the file does not exist, lists no combination, or has a
   /// line that lists no valid combination of the dimensions' values.
   /// </summary>
   public static IReadOnlyList<MatrixCombination> CombinationsListedIn(string fileName, MatrixDimensions dimensions) =>
      CombinationsCache.GetOrAdd((fileName, dimensions), key => new MatrixConfigurationFile(key.FileName, key.Dimensions).ReadCombinations());

   IReadOnlyList<MatrixCombination> ReadCombinations()
   {
      var combinations = ReadLines()
                        .Select((text, lineIndex) => (LineNumber: lineIndex + 1, Text: text.Trim()))
                        .Where(it => !IsBlankOrComment(it.Text))
                        .Select(it => new MatrixConfigurationFileLine(this, it.LineNumber, it.Text))
                        .SelectMany(it => it.Combinations)
                        .DistinctBy(it => it.ToString())
                        .OrderBy(it => it.ToString())
                        .ToList();

      if(combinations.Count == 0)
         throw new InvalidMatrixConfigurationException($"The matrix configuration file {FileName} lists no combinations: every line in it is blank or a comment.");

      return combinations;

      static bool IsBlankOrComment(string trimmedLineText) => trimmedLineText.IsNullEmptyOrWhiteSpace() || trimmedLineText.StartsWith(Comment);
   }

   string[] ReadLines()
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
      if(!File.Exists(filePath))
         throw new InvalidMatrixConfigurationException($"The matrix configuration file {FileName} does not exist: {filePath}. "
                                                     + "It is looked up in the test assembly's output directory, so the test project must copy it there.");

      return File.ReadAllLines(filePath);
   }
}
