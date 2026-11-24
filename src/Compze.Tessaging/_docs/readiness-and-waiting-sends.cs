// ReSharper disable All

using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.TypeIdentifiers.DependencyInjection;
using Compze.Tessaging.Documentation.DeclaringEndpoints;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.ReadinessAndWaitingSends;

#region declaring_patience
class SignupEndpoint : ExactlyOnceEndpointDeclaration<SignupEndpoint>, IEndpointIdentity
{
   public static string Name => "Signup";
   public static EndpointId Id { get; } = new(Guid.Parse("E81B3C57-0F4A-4D92-B6C8-5A27D9E1F043"));

   //A send with no reachable handler waits up to this long for one, instead of the default 30 seconds.
   protected override TimeSpan? HandlerAvailabilityPatience => TimeSpan.FromSeconds(10);

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>();
}
#endregion

static class ReadinessExample
{
   internal static async Task RunAsync(DirectoryInfo dataDirectory)
   {
      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("ReadinessExample.EndpointRegistry", dataDirectory);
      await using var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(), new SameMachineEnvironment(registry, dataDirectory));
      host.RegisterEndpoint(new AccountsEndpoint());
      var signup = host.RegisterEndpoint(new SignupEndpoint());

      #region awaiting_readiness
      await host.StartAsync();

      //Completes once a handler is reachable for every tommand and tuery in RegisterAccount's namespace.
      await signup.AwaitReadinessAsync(ReadinessTypes.InNamespaceOf<RegisterAccount>());

      //Only now open the doors to traffic: no first caller pays for discovery.
      #endregion

      await signup.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new RegisterAccount(Guid.NewGuid(), "ada@example.com"));
   }
}
