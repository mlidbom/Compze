using System.Collections.Concurrent;
using Compze.Contracts;
using Compze.Utilities.SystemCE;

namespace Compze.xUnitMatrix;

static class ComponentCombinationsConfigurationFileReader
{
   static readonly ConcurrentDictionary<string, IReadOnlyList<ComponentCombination>> CombinationsCache = new();

   public static IReadOnlyList<ComponentCombination> GetCombinations(string configurationFileName, Type[] componentEnumTypes, int? onlyConsiderComponentIndex = null)
   {
      var combinations = CombinationsCache.GetOrAdd(
         configurationFileName,
         fileName => ReadFile(componentEnumTypes, fileName));

      if(onlyConsiderComponentIndex is {} index)
      {
         return combinations
               .DistinctBy(it => it.Components[index])
               .ToList();
      }

      return combinations;
   }

   const string Comment = "//";
   const char SkipCombination = '#';

   static IReadOnlyList<ComponentCombination> ReadFile(Type[] componentTypes, string fileName) =>
      ReadFileLines(fileName)
        .Select(it => it.Trim())
        .Where(it => !it.IsNullEmptyOrWhiteSpace())
        .Where(it => !it.StartsWithCE(Comment))
        .Where(it => !it.StartsWith(SkipCombination))
        .Select(it => new ComponentCombinationsConfigurationFileLine(componentTypes, it))
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
