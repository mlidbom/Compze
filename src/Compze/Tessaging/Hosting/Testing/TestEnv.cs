using System;
using Compze.Wiring;
using Compze.Wiring.Testing;
using Compze.Wiring.Testing.Sql;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   internal static Func<PluggableComponents?>? XunitDiscoverer = null;

   static PluggableComponents GetComponents()
   {
      if(XunitDiscoverer?.Invoke() is {} xunitComponents)
         return xunitComponents;

      throw new Exception($"No components provider found any components");
   }

   public static SqlLayer SqlLayer => GetComponents().SqlLayer;

   public static DIContainer DIContainer => GetComponents().DiContainer;
}
