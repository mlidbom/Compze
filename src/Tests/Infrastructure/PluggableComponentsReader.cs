using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
public static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<List<string>> _combinationsLazy = new(GetCombinationsInternal);

   public static IReadOnlyList<string> GetCombinations() => _combinationsLazy.Value;

   static List<string> GetCombinationsInternal()
   {
      var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestUsingPluggableComponentCombinations);

      if(!File.Exists(assemblyPath))
      {
         return [Enumerable.Repeat("FileMissing", 3)._(it => string.Join(":", it))];
      }

      return File.ReadAllLines(assemblyPath)
                 .Select(it => it.Trim())
                 .Where(line => !string.IsNullOrEmpty(line))
                 .Where(line => !line.StartsWith('#'))
                 .ToList();
   }
}
