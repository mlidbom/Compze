// ReSharper disable All

#region usings
using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.TypeIdentifiers.DependencyInjection;
#endregion
//todo:compze: Compze.Tessaging.Endpoints.EndpointBuilder collides with ASP.NET Core's Microsoft.AspNetCore.Builder.EndpointBuilder, which the Website project's
//   implicit usings import. Delete this alias once Compze renames its EndpointBuilder.
using EndpointBuilder = Compze.Tessaging.Endpoints.EndpointBuilder;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.DeclaringEndpoints;

#region tessages
public class RegisterAccount(Guid accountId, string email) : Remotable.ExactlyOnce.Tommand
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}

public interface IAccountRegistered : IExactlyOnceTevent
{
   Guid AccountId { get; }
   string Email { get; }
}

public class AccountRegistered(Guid accountId, string email) : IAccountRegistered
{
   public TessageId Id { get; private set; } = TessageId.New();
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}

public class RecordWelcomeEmailSent(Guid accountId) : Remotable.ExactlyOnce.Tommand
{
   public Guid AccountId { get; private set; } = accountId;
}
#endregion

#region accounts_endpoint
class AccountsEndpoint : ExactlyOnceEndpointDeclaration<AccountsEndpoint>, IEndpointIdentity
{
   public static string Name => "Accounts";
   public static EndpointId Id { get; } = new(Guid.Parse("0E4C7B3A-5D21-4F8E-9A6B-2C1D8E7F3A95"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>()
      .Register(Singleton.For<AccountRecords>().CreatedBy(() => new AccountRecords()));

   protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
      .ForTommand((RegisterAccount tommand, IUnitOfWorkTeventPublisher publisher) =>
                     publisher.PublishAsync(new AccountRegistered(tommand.AccountId, tommand.Email)))
      .ForTommand((RecordWelcomeEmailSent tommand, AccountRecords records) =>
                     records.RecordWelcomeEmailSentAsync(tommand.AccountId));
}
#endregion

#region welcome_emails_endpoint
class WelcomeEmailsEndpoint : ExactlyOnceEndpointDeclaration<WelcomeEmailsEndpoint>, IEndpointIdentity
{
   public static string Name => "WelcomeEmails";
   public static EndpointId Id { get; } = new(Guid.Parse("7A2F9C14-8B3E-4D6A-B105-E94C2D6F8B37"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>()
      .Register(Singleton.For<IEmailSender>().CreatedBy(() => new ConsoleEmailSender()));

   protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
      .ForTevent(async (IAccountRegistered registered, IEmailSender emailSender, IUnitOfWorkTommandSender tommandSender) =>
       {
          await emailSender.SendWelcomeEmailAsync(registered.Email);
          await tommandSender.SendAsync(new RecordWelcomeEmailSent(registered.AccountId));
       });
}
#endregion

#region subscribing_for_dynamic_observers
class AccountViewsEndpoint : ExactlyOnceEndpointDeclaration<AccountViewsEndpoint>, IEndpointIdentity
{
   public static string Name => "AccountViews";
   public static EndpointId Id { get; } = new(Guid.Parse("190bbc33-1577-4beb-9e53-9c738635adc0"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>();

   protected override void Declare(ExactlyOnceEndpointBuilder endpoint) => endpoint
      .SubscribeForDynamicObservers<IAccountRegistered>();
}
#endregion

#region environment
class SameMachineEnvironment : IEndpointEnvironment
{
   readonly InterprocessEndpointRegistry _registry;
   readonly DirectoryInfo _dataDirectory;

   public SameMachineEnvironment(InterprocessEndpointRegistry registry, DirectoryInfo dataDirectory)
   {
      _registry = registry;
      _dataDirectory = dataDirectory;
   }

   public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
      .NamedPipeEndpointTransport()
      .NewtonsoftSerializer()
      .ParticipateIn(_registry);

   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder)
   {
      endpointBuilder.Registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                                  .CreatedBy(() => new SqliteFilesIn(_dataDirectory)));
      endpointBuilder.SqliteDomainDatabase("Accounts");
   }
}

//Gives every connection string name a sqlite database file of its own in the data directory.
class SqliteFilesIn(DirectoryInfo dataDirectory) : IConfigurationParameterProvider
{
   readonly DirectoryInfo _dataDirectory = dataDirectory;

   public string GetString(string parameterName, string? valueIfMissing = null) =>
      $"Data Source={Path.Combine(_dataDirectory.FullName, parameterName + ".sqlite")}";
}
#endregion

static class EndpointsExample
{
   internal static async Task RunAsync(DirectoryInfo dataDirectory)
   {
      #region hosting
      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("AccountsExample.EndpointRegistry", dataDirectory);

      await using var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(),
                                                            new SameMachineEnvironment(registry, dataDirectory));
      var accounts = host.RegisterEndpoint(new AccountsEndpoint());
      host.RegisterEndpoint(new WelcomeEmailsEndpoint());
      await host.StartAsync();

      var tommandSender = accounts.ServiceLocator.Resolve<IIndependentTommandSender>();
      await tommandSender.SendAsync(new RegisterAccount(Guid.NewGuid(), "ada@example.com"));
      #endregion

      await accounts.ServiceLocator.Resolve<AccountRecords>().AwaitWelcomeEmailSentAsync();
   }
}

interface IEmailSender
{
   Task SendWelcomeEmailAsync(string email);
}

class ConsoleEmailSender : IEmailSender
{
   public Task SendWelcomeEmailAsync(string email)
   {
      Console.WriteLine($"Welcome email sent to {email}");
      return Task.CompletedTask;
   }
}

class AccountRecords
{
   readonly TaskCompletionSource _welcomeEmailSent = new(TaskCreationOptions.RunContinuationsAsynchronously);

   public Task RecordWelcomeEmailSentAsync(Guid accountId)
   {
      Console.WriteLine($"Recorded that account {accountId} got its welcome email");
      _welcomeEmailSent.TrySetResult();
      return Task.CompletedTask;
   }

   internal Task AwaitWelcomeEmailSentAsync() => _welcomeEmailSent.Task.WaitAsync(TimeSpan.FromSeconds(30));
}
