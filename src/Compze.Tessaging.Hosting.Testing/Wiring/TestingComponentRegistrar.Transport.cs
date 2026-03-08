using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarTransport
{
   public static IComponentRegistrar CurrentTestsTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().AspNetCoreTransport();
}
