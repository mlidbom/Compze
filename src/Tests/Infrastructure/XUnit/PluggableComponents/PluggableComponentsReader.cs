using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
public static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<IReadOnlyList<string[]>> CombinationsLazy = new(() =>
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestUsingPluggableComponentCombinations);

      if(!File.Exists(filePath)) throw new Exception($"{filePath} is missing");

      var components = File.ReadAllLines(filePath)
                           .Select(it => it.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .Where(line => !line.StartsWith('#'))
                           .Select(it => it.Split(":"))
                           .ToList();
      if(components.Count == 0)
         throw new Exception("Found no components");

      var componentDimensions = components[0].Length;
      if(components.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      return components;
   });

   public static IReadOnlyList<string[]> Combinations => CombinationsLazy.Value;
}
