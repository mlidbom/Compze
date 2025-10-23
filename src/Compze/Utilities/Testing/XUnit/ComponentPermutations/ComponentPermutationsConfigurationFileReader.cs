using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

   const string Comment = "//";
   const char SkipPermutation = '#';

   static IReadOnlyList<ComponentsPermutation> ReadFile(Type[] componentTypes, string fileName) =>
      ReadFileLines(fileName)
        .Select(it => it.Trim())
        .Where(it => !it.IsNullEmptyOrWhiteSpace())
        .Where(it => !it.StartsWith(Comment))
        .Where(it => !it.StartsWith(SkipPermutation))
        .Select(it => new ComponentPermutationsConfigurationFileLine(componentTypes, it))
        .SelectMany(it => it.ExpandWildcardsIntoConcretePermutations())
        .OrderBy(it => it.ToString())
        .DistinctBy(it => it.ToString())
        .ToList();

   static string[] ReadFileLines(string fileName)
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
      if(!File.Exists(filePath)) throw new Exception($"File does not exist: {filePath}");
      var fileContent = File.ReadAllLines(filePath);
      return fileContent;
   }
}
