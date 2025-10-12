using Compze.Utilities.Contracts;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Wiring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
public static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<IReadOnlyList<PluggableComponents>> _combinationsLazy = new(GetCombinationsInternal);

   public static IReadOnlyList<PluggableComponents> GetCombinations() => _combinationsLazy.Value;

   static IReadOnlyList<PluggableComponents> GetCombinationsInternal()
   {
      ConsoleCE.WriteImportantLine("DIContainer.Current");
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestUsingPluggableComponentCombinations);

      if(!File.Exists(filePath)) throw new Exception($"{filePath} is missing");

      return File.ReadAllLines(filePath)
                 .Select(it => it.Trim())
                 .Where(line => !string.IsNullOrEmpty(line))
                 .Where(line => !line.StartsWith('#'))
                 .Select(PluggableComponents.FromString)
                 .ToList();
   }
}

public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}";

   public static PluggableComponents FromString(string combination)
   {
      ConsoleCE.WriteImportantLine("PluggableComponents.FromString");
      try
      {
         var parts = combination.Split(':');

         Assert.Argument.Is(parts.Length == 2, () => $"PluggableComponentParts has an invalid format: {combination}");

         return FromStrings(parts[0], parts[1]);
      }
      catch(Exception e)
      {
         throw new Exception($"PluggableComponentParts has an invalid format: {combination}", e);
      }
   }

   public static PluggableComponents FromStrings(string sqlLayer, string container)
   {
      return new PluggableComponents((SqlLayer)Enum.Parse(typeof(Wiring.SqlLayer), sqlLayer),
                                     (DIContainer)Enum.Parse(typeof(Wiring.DIContainer), container));
   }
}
