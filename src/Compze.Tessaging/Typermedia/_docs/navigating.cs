// ReSharper disable All

#region usings
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Tessaging.Documentation.DeclaringEndpoints;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Typermedia.Documentation.DeclaringAnApi;
using Compze.TypeIdentifiers.DependencyInjection;
#endregion

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Typermedia.Documentation.Navigating;

#region navigating_endpoint
//Serves nothing: it navigates the API the AccountsApi endpoint serves.
class AccountsAdminEndpoint : BestEffortEndpointDeclaration<AccountsAdminEndpoint>, IEndpointIdentity
{
   public static string Name => "AccountsAdmin";
   public static EndpointId Id { get; } = new(Guid.Parse("8C1E5A72-3D94-4F0B-A6E8-17B2D9C4E3F5"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<AccountLink>();
}
#endregion

static class NavigatingExample
{
   internal static async Task RunAsync(DirectoryInfo dataDirectory)
   {
      #region hosting
      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("AccountsApiExample.EndpointRegistry", dataDirectory);
      await using var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(), new SameMachineEnvironment(registry, dataDirectory));
      host.RegisterEndpoint(new AccountsApiEndpoint());
      var admin = host.RegisterEndpoint(new AccountsAdminEndpoint());
      await host.StartAsync();

      var navigator = admin.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
      #endregion

      #region one_step_at_a_time
      var start = await navigator.GetAsync(AccountsApi.Start);
      var ada = await navigator.PostAsync(start.OpenAccount("ada@example.com"));
      ada = await navigator.PostAsync(ada.ChangeEmail("ada.lovelace@example.com"));
      #endregion

      #region navigation_specification
      var openAndChangeEmail = NavigationSpecification.Get(AccountsApi.Start)
                                                      .Post(start => start.OpenAccount("grace@example.com"))
                                                      .Post(account => account.ChangeEmail("grace.hopper@example.com"));

      var grace = await navigator.NavigateAsync(openAndChangeEmail);
      #endregion

      #region get_and_select
      var gracesEmail = NavigationSpecification.Get(AccountsApi.Start)
                                               .Get(start => start.Account(grace.AccountId))
                                               .Select(account => account.Email)
                                               .NavigateOn(navigator);
      #endregion

      Console.WriteLine($"{ada.Email}, {gracesEmail}");
   }
}
