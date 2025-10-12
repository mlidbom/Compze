using Compze.Tests.Infrastructure;
using System;
using Compze.Wiring;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   internal static Func<PluggableComponents?>? XunitDiscoverer = null;
   internal static Func<PluggableComponents?>? NunitDiscoverer = null;

   static PluggableComponents GetComponents()
   {
      if(XunitDiscoverer == null && NunitDiscoverer == null)
         throw new Exception("No test framework registered a discoverer");

      if(XunitDiscoverer?.Invoke() is {} xunitComponents)
         return xunitComponents;

      if(NunitDiscoverer?.Invoke() is {} nunitComponents)
         return nunitComponents;

      throw new Exception($"No components provider found any components");
   }

   public static SqlLayer SqlLayer => GetComponents().SqlLayer;

   public static DIContainer DIContainer => GetComponents().DiContainer;
}
