using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Transport.AspNet;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Tests.Integration.Hosting;

public static class EndpointBuilderTestingExtensions
{
   extension(IEndpointBuilder @this)
   {
      public EndpointFoundation ComposeFoundationWithCurrentTestsTransportAndNoDatabase() =>
         @this.ComposeEndpoint(it => TestEnv.Transport switch
         {
            Transport.AspNetCore => it.AspNetCoreEndpointTransport(),
            Transport.NamedPipes => it.NamedPipeEndpointTransport(),
            _                    => throw new ArgumentOutOfRangeException()
         });
   }
}
