using Compze.Abstractions.Wiring.Testing.Internal;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>TestEnvironment class. Shortened name since it is referenced statically and has nested types</summary>
public static partial class TestEnv
{
   public static Func<PluggableComponents?>? XunitDiscoverer { get; set; } = null;

   static PluggableComponents GetComponents()
   {
      if(XunitDiscoverer?.Invoke() is {} xunitComponents)
         return xunitComponents;

      throw new Exception("No components provider found any components");
   }

   internal static Serializer Serializer => GetComponents().Serializer;

   public static SqlLayer SqlLayer => GetComponents().SqlLayer;

   public static DIContainer DIContainer => GetComponents().DiContainer;

   internal static Transport Transport => GetComponents().Transport;
}
