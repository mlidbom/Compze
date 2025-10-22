using System.Collections.Concurrent;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>Reads pluggable component combinations from configuration file in the assembly directory.</summary>
static class PluggableComponentsReader
{
   static readonly ConcurrentDictionary<string, ComponentsPermutationsList> PermutationsCache = new();

   /// <summary>Gets permutations parsed with the provided component types as enums from the specified configuration file.</summary>
   public static ComponentsPermutationsList GetPermutations(string configurationFileName, Type[] componentEnumTypes)
   {
      return PermutationsCache.GetOrAdd(
         configurationFileName,
         fileName => ReadFile(componentEnumTypes, fileName));
   }

   static ComponentsPermutationsList ReadFile(Type[] componentEnumTypes, string fileName) =>
      ParseFileContent(ReadFileLines(fileName), componentEnumTypes);

   static string[] ReadFileLines(string fileName)
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
      if(!File.Exists(filePath)) throw new Exception($"{filePath} is missing");
      var fileContent = File.ReadAllLines(filePath);
      return fileContent;
   }

   const string Comment = "//";
   const char SkipPermutation = '#';

   static ComponentsPermutationsList ParseFileContent(IReadOnlyList<string> fileLines, Type[] componentTypes)
   {
      var lines = fileLines
                 .Select(it => it.Trim())
                 .Where(it => !string.IsNullOrEmpty(it))
                 .Where(it => !it.StartsWith(Comment))
                 .ToList();

      var activeLines = lines
                       .Where(it => !it.StartsWith(SkipPermutation))
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(it => it.TrimStart(SkipPermutation))
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         return new ComponentsPermutationsList([]);

      var componentDimensions = activeLines[0].Length;
      if(activeLines.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different numbers of components");

      return new ComponentsPermutationsList(
         activeLines.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentTypes))
                    .ToList());
   }
}
