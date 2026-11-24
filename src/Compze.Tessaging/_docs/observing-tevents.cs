// ReSharper disable All

using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Documentation.DeclaringTessageTypes;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.ObservingTevents;

static class ObservingTeventsExample
{
   internal static async Task RunAsync()
   {
      IContainerBuilder containerBuilder = new MicrosoftContainerBuilder();
      #region composed_observers
      containerBuilder.Registrar.LocalTessagingEngine(engine => engine
         .ObserveTevents(observe => observe
            .ForTevent((IAccountRegistered registered) => Console.WriteLine($"Observed the registration of {registered.Email}"))
            .ForTeventsOfEachUnitOfWork((IReadOnlyList<IAccountTevent> tevents) => Console.WriteLine($"A unit of work changed accounts {tevents.Count} times"))));
      #endregion

      await using var container = containerBuilder.Build();
      var engine = container.RootResolver.Resolve<ILocalTessagingEngine>();

      using(var accountList = new AccountListView(container.RootResolver.Resolve<IDynamicTeventObservers>()))
      {
         container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
            var accountId = Guid.NewGuid();
            publisher.Publish(new AccountRegistered(accountId, "ada@example.com"));
            publisher.Publish(new AccountEmailChanged(accountId, "ada.lovelace@example.com"));
         });

         #region awaiting_the_observers
         await engine.AwaitNoTessagesInFlightAsync();
         #endregion
      }

      await engine.StopAsync();
   }
}

#region dynamic_observer
class AccountListView : IDisposable
{
   readonly IDisposable _observation;

   public AccountListView(IDynamicTeventObservers observers) =>
      _observation = observers.ObserveTevents(observe => observe.ForTevent((IAccountTevent tevent) => Refresh(tevent)));

   void Refresh(IAccountTevent tevent) => Console.WriteLine($"The account list shows the change to account {tevent.AccountId}");

   public void Dispose() => _observation.Dispose();
}
#endregion
