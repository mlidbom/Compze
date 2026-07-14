using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

//An alias, inside the namespace scope so it wins the lookup: within Compze.Tessaging.* the bare name otherwise resolves to the Compze.Tessaging.Transport namespace, not the enum.
using Transport = Compze.Abstractions.Wiring.Testing.Internal.Transport;

public static class TestingComponentRegistrarTessagingTransport
{
   ///<summary>Registers the current test's <see cref="Transport"/> implementation of the Tessaging transport — inbox server and transport client.</summary>
   public static IComponentRegistrar CurrentTestsTessagingTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.AspNetCoreTessagingTransport(),
         Transport.NamedPipes => register.NamedPipeTessagingTransport(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
