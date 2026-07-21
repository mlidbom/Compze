using Compze.Internals.Testing;

namespace Compze.Hosting.Testing;

///<summary>Extends <see cref="TestEnv"/> with the <see cref="PluggableComponents"/> the currently executing test runs against.</summary>
public static class TestEnvPluggableComponents
{
   static Func<PluggableComponents?>? _xunitDiscoverer;

   extension(TestEnv)
   {
      ///<summary>Set by the xunit test infrastructure to expose the pluggable-component combination of the currently executing test case.</summary>
      public static Func<PluggableComponents?>? XunitDiscoverer
      {
         get => _xunitDiscoverer;
         set => _xunitDiscoverer = value;
      }

      public static SqlLayer SqlLayer => GetComponents().SqlLayer;

      public static DIContainer DIContainer => GetComponents().DiContainer;

      public static Serializer Serializer => GetComponents().Serializer;

      public static Transport Transport => GetComponents().Transport;
   }

   static PluggableComponents GetComponents()
   {
      if(_xunitDiscoverer?.Invoke() is {} xunitComponents)
         return xunitComponents;

      throw new Exception("No components provider found any components");
   }
}
