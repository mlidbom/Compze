// ReSharper disable All

using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation;

#pragma warning disable // Documentation example code: deliberately illustrative declarations, not production code.

namespace Compze.Tessaging.Documentation.DeclaringTessageTypes;

static class RoutingByTypeExample
{
   internal static async Task RunAsync()
   {
      IContainerBuilder containerBuilder = new MicrosoftContainerBuilder();
      containerBuilder.Registrar.LocalTessagingEngine(engine => engine.RegisterTessageBusHandlers(handle => handle
         #region routing_by_type
         .ForTevent((IAccountTevent tevent) => Console.WriteLine($"Every account tevent: {tevent.GetType().Name}"))
         .ForTevent((IAccountRegistered registered) => Console.WriteLine($"Registrations, imports included: {registered.GetType().Name}"))
         .ForTevent((IAccountImported imported) => Console.WriteLine($"Imports only: {imported.GetType().Name}"))
         #endregion
      ));

      await using var container = containerBuilder.Build();
      var publisher = container.RootResolver.Resolve<IIndependentTeventPublisher>();
      var accountId = Guid.NewGuid();
      publisher.Publish(new AccountRegistered(accountId, "ada@example.com"));
      publisher.Publish(new AccountImported(Guid.NewGuid(), "grace@example.com", importedFrom: "the old system"));
      publisher.Publish(new AccountEmailChanged(accountId, "ada.lovelace@example.com"));
      await container.RootResolver.Resolve<ILocalTessagingEngine>().StopAsync();
   }

   static void InATest()
   {
      #region checking_the_vocabulary
      TessageTypeDesignRules.AssertFulfilledByAllTessageTypesIn(typeof(IAccountTevent).Assembly);
      #endregion
   }
}

#region tevent_hierarchy
public interface IAccountTevent : ITevent
{
   Guid AccountId { get; }
}

public interface IAccountRegistered : IAccountTevent
{
   string Email { get; }
}

public interface IAccountImported : IAccountRegistered
{
   string ImportedFrom { get; }
}

public interface IAccountEmailChanged : IAccountTevent
{
   string Email { get; }
}
#endregion

#region tevent_classes
public class AccountRegistered(Guid accountId, string email) : IAccountRegistered
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}

public class AccountImported(Guid accountId, string email, string importedFrom) : IAccountImported
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
   public string ImportedFrom { get; private set; } = importedFrom;
}

public class AccountEmailChanged(Guid accountId, string email) : IAccountEmailChanged
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}
#endregion

#region delivery_contracts
//Never leaves the process that publishes it.
public interface IAccountCacheInvalidated : ITevent;

//May cross to other endpoints, best-effort: no store, no deduplication, no retries.
public interface IAccountsOnlineCountChanged : IRemotableTevent
{
   int AccountsOnline { get; }
}

//Crosses to other endpoints exactly once: stored, deduplicated, its handling retried when it fails.
public interface IAccountClosed : IExactlyOnceTevent
{
   Guid AccountId { get; }
}

public class AccountClosed(Guid accountId) : IAccountClosed
{
   public TessageId Id { get; private set; } = new(); //What the receiving endpoint deduplicates on.
   public Guid AccountId { get; private set; } = accountId;
}
#endregion

#region tommand
public class CloseAccount(Guid accountId) : Remotable.ExactlyOnce.Tommand
{
   public Guid AccountId { get; private set; } = accountId;
}
#endregion

#region tuery
public class AccountDetails(Guid accountId, string email)
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}

public class AccountDetailsTuery(Guid accountId) : Remotable.NonTransactional.Tueries.Tuery<AccountDetails>
{
   public Guid AccountId { get; private set; } = accountId;
}
#endregion
