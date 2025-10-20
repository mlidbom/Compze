using System;
using Compze.Utilities.Contracts;
using Compze.Wiring.Testing;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tessaging.Hosting.Testing;

public readonly record struct PluggableComponents(SqlLayer SqlLayer, DIContainer DiContainer)
{
   public override string ToString() => $"{SqlLayer}:{DiContainer}";

   public static PluggableComponents FromString(string combination)
   {
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
