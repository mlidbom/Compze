using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>Extends <see cref="TestEnv"/> with pluggable component members for Tessaging testing.</summary>
public static class TestEnvPluggableComponents
{
   static Func<PluggableComponents?>? _xunitDiscoverer;

   extension(TestEnv)
   {
      public static Func<PluggableComponents?>? XunitDiscoverer
      {
         get => _xunitDiscoverer;
         set => _xunitDiscoverer = value;
      }

      public static SqlLayer SqlLayer => GetComponents().SqlLayer;

      public static DIContainer DIContainer => GetComponents().DiContainer;

      internal static Serializer Serializer => GetComponents().Serializer;

      internal static Transport Transport => GetComponents().Transport;
   }

   static PluggableComponents GetComponents()
   {
      if(_xunitDiscoverer?.Invoke() is {} xunitComponents)
         return xunitComponents;

      throw new Exception("No components provider found any components");
   }
}

