// ReSharper disable All

#region usings
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Documentation.DeclaringTessageTypes;
#endregion

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.InOneProcess;

static class InOneProcessExample
{
   internal static async Task RunAsync()
   {
      #region composing_the_engine
      IContainerBuilder containerBuilder = new MicrosoftContainerBuilder();
      containerBuilder.Registrar
                      .Register(Singleton.For<AccountStatistics>().CreatedBy(() => new AccountStatistics()),
                                Singleton.For<AccountRegistration>().CreatedBy((IIndependentTeventPublisher publisher) => new AccountRegistration(publisher)))
                      .LocalTessagingEngine(engine => engine
                         .RegisterTessageBusHandlers(handle => handle
                            .ForTevent((IAccountRegistered registered) => Console.WriteLine($"Welcome, {registered.Email}!"))
                            .ForTevent((IAccountTevent tevent, AccountStatistics statistics) => statistics.Record(tevent))));

      await using var container = containerBuilder.Build();
      #endregion

      #region publishing_independently
      var registration = container.RootResolver.Resolve<AccountRegistration>();
      var accountId = registration.Register("ada@example.com"); //Prints "Welcome, ada@example.com!" before it returns.
      #endregion

      #region publishing_in_a_unit_of_work
      container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         publisher.Publish(new AccountEmailChanged(accountId, "ada.lovelace@example.com"));
         publisher.Publish(new AccountImported(Guid.NewGuid(), "grace@example.com", importedFrom: "the old system"));
      });
      #endregion

      #region stopping_the_engine
      await container.RootResolver.Resolve<ILocalTessagingEngine>().StopAsync();
      #endregion

      Console.WriteLine($"{container.RootResolver.Resolve<AccountStatistics>().TeventsRecorded} account tevents recorded");
   }
}

#region application_class
class AccountRegistration
{
   readonly IIndependentTeventPublisher _publisher;

   public AccountRegistration(IIndependentTeventPublisher publisher) => _publisher = publisher;

   public Guid Register(string email)
   {
      var accountId = Guid.NewGuid();
      _publisher.Publish(new AccountRegistered(accountId, email));
      return accountId;
   }
}
#endregion

class AccountStatistics
{
   public int TeventsRecorded { get; private set; }

   public void Record(IAccountTevent tevent) => TeventsRecorded++;
}
