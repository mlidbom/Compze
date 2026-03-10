using System.Collections.Concurrent;
using Compze.Contracts;
using Compze.Internals.SystemCE;

namespace Compze.xUnitMatrix;

static class MatrixConfigurationFileReader
{
   static readonly ConcurrentDictionary<string, IReadOnlyList<MatrixCombination>> CombinationsCache = new();

   public static IReadOnlyList<MatrixCombination> GetCombinations(string configurationFileName, Type[] componentEnumTypes)
   {
      return CombinationsCache.GetOrAdd(
         configurationFileName,
         fileName => ReadFile(componentEnumTypes, fileName));
   }

   const char Comment = '#';

   static IReadOnlyList<MatrixCombination> ReadFile(Type[] componentTypes, string fileName) =>
      ReadFileLines(fileName)
        .Select(it => it.Trim())
        .Where(it => !it.IsNullEmptyOrWhiteSpace())
        .Where(it => !it.StartsWith(Comment))
        .Select(it => new MatrixConfigurationFileLine(componentTypes, it))
        .SelectMany(it => it.ExpandWildcardsIntoConcretePermutations())
        .OrderBy(it => it.ToString())
        .DistinctBy(it => it.ToString())
        .ToList()
        ._assert(it => it.Any(), _ => "found no configured component combinations");

   static string[] ReadFileLines(string fileName)
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
      if(!File.Exists(filePath)) throw new Exception($"File does not exist: {filePath}");
      var fileContent = File.ReadAllLines(filePath);
      return fileContent;
   }
}
