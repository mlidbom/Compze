// ReSharper disable MemberCanBeMadeStatic.Global we want the fluid navigation to be composable with other APIs (AccountApi as a member property in a composite API for a composite UI etc) so static navigation is out.
// ReSharper disable MemberCanBeMadeStatic.Local

using AccountManagement.Domain;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE;
using Compze.Typermedia;

namespace AccountManagement.API;

/// <summary>
/// This class provides the ability to use type safe API navigation from a type that does not run on .Net. For instance via Typescript in browser.
/// We generate typescript interfaces for each of the resources exposed via the Tueries and tommands ultimately reachable through the Start Tuery.
/// A generic browser type can then be used to navigate the whole API remotely.
/// For .Net clients the next class in this file is a far more convenient way to consume the API.
/// </summary>
static class AccountWebClientApi
{
   public static TessageTypes.Remotable.NonTransactional.Tueries.NewableResultLink<StartResource> Start => new();
}


/// <summary>
/// This is the entry point to the API for all .Net clients. It provides a simple intuitive fluent API for accessing all the functionality in the AccountManagement application.
/// </summary>
public class AccountApi : IStaticInstancePropertySingleton<AccountApi>
{
   public static AccountApi Instance { get; } = new();

   NavigationSpecification<StartResource> Start => NavigationSpecification.Get(AccountWebClientApi.Start);

   public TuerySection Tuery => new();
   public TommandsSection Tommand => new();

   public class TuerySection
   {
      static readonly NavigationSpecification<StartResource.TueriesResource> Tueries = Instance.Start.Select(start => start.Tueries);

      public NavigationSpecification<AccountResource> AccountById(AccountId accountId) => Tueries.Get(tueries => tueries.AccountById(accountId));
   }

   public class TommandsSection
   {
      static NavigationSpecification<StartResource.TommandsResource> Tommands => Instance.Start.Select(start => start.TommandsResources);

      public NavigationSpecification<AccountResource.Tommand.Register> Register() => Tommands.Select(tommands => tommands.Register);
      public NavigationSpecification<AccountResource.Tommand.Register.RegistrationAttemptResult> Register(AccountId accountId, string email, string password) => Tommands.Post(tommands => tommands.Register.WithValues(accountId, email, password));

      public NavigationSpecification<AccountResource.Tommand.LogIn> Login() => Tommands.Select(tommands => tommands.Login);
      public NavigationSpecification<AccountResource.Tommand.LogIn.LoginAttemptResult> Login(string email, string password) => Tommands.Post(tommands => tommands.Login.WithValues(email, password));
   }

   ///<summary>This method ensures that the client endpoints has everything it needs to use the services in this API. Type mappings etc. Teventually we will probably be setting up pipeline components such as custom caches etc here.</summary>
   public static void RegisterWithClientEndpoint(IEndpointBuilder builder)
   {
   }
}