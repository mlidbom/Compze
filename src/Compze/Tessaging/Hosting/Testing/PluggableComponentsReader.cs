using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compze.Utilities.Contracts;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
public static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<IReadOnlyList<PluggableComponents>> CombinationsLazy = new(GetCombinationsInternal);

   public static IReadOnlyList<PluggableComponents> Combinations => CombinationsLazy.Value;

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
      return new PluggableComponents(Enum.Parse<SqlLayer>(sqlLayer),
                                     Enum.Parse<DIContainer>(container));
   }
}
