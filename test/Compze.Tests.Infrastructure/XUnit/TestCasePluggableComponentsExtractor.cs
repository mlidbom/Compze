using Compze.Tessaging.Hosting.Testing;

namespace Compze.Tests.Infrastructure.XUnit;

static class TestCasePluggableComponentsExtractor
{
   public static PluggableComponents ToPluggableComponents() =>
      new(PCTAttribute.SqlLayer, PCTAttribute.DIContainer, PCTAttribute.Serializer, PCTAttribute.Transport);

   public static PluggableComponents? TryExtractPluggableComponents()
   {
      try
      {
         return ToPluggableComponents();
      }
#pragma warning disable CA1031 // We need to catch all exceptions to return null for non-PCT tests
      catch
      {
         return null;
      }
#pragma warning restore CA1031
   }
}
