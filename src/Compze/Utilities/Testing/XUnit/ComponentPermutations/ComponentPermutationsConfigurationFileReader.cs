using System.Collections.Concurrent;
using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

static class ComponentPermutationsConfigurationFileReader
{
   static readonly ConcurrentDictionary<string, IReadOnlyList<ComponentsPermutation>> PermutationsCache = new();

   public static IReadOnlyList<ComponentsPermutation> GetPermutations(string configurationFileName, Type[] componentEnumTypes)
   {
      return PermutationsCache.GetOrAdd(
         configurationFileName,
         fileName => ReadFile(componentEnumTypes, fileName));
   }

   static IReadOnlyList<ComponentsPermutation> ReadFile(Type[] componentEnumTypes, string fileName) =>
      ParseFileContent(ReadFileLines(fileName), componentEnumTypes);

   static string[] ReadFileLines(string fileName)
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
      if(!File.Exists(filePath)) throw new Exception($"File does not exist: {filePath}");
      var fileContent = File.ReadAllLines(filePath);
      return fileContent;
   }

   const string Comment = "//";
   const char SkipPermutation = '#';

   static IReadOnlyList<ComponentsPermutation> ParseFileContent(IReadOnlyList<string> fileLines, Type[] componentTypes)
   {
      return new List<ComponentsPermutation>(
         fileLines
           .Select(it => it.Trim())
           .Where(it => !it.IsNullEmptyOrWhiteSpace())
           .Where(it => !it.StartsWith(Comment))
           .Where(it => !it.StartsWith(SkipPermutation))
           .Select(it => it.Split(ComponentsPermutation.Separator))
           .Select(componentsOrWildCards => new ConfigFileLine(componentTypes, componentsOrWildCards))
           .SelectMany(it => it.ExpandWildcardsIntoConcretePermutations())
           .OrderBy(it => it.ToString())
           .DistinctBy(it => it.ToString())
           .ToList());
   }
}
