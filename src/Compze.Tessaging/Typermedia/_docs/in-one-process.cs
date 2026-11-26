// ReSharper disable All

#region usings
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageTypes;
#endregion

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Typermedia.Documentation.InOneProcess;

#region strictly_local_tessages
public class AccountSettingsLink : StrictlyLocal.Tueries.StrictlyLocalTuery<AccountSettingsLink, AccountSettings>;

public class AccountSettings(string displayName, bool receivesNewsletter)
{
   public string DisplayName { get; } = displayName;
   public bool ReceivesNewsletter { get; } = receivesNewsletter;

   public ChangeDisplayName ChangeDisplayName(string displayName) => new(displayName);
   public ToggleNewsletter ToggleNewsletter() => new();
}

//Returns nothing.
public class ChangeDisplayName(string displayName) : StrictlyLocal.Tommands.StrictlyLocalTommand
{
   public string DisplayName { get; } = displayName;
}

//Returns the settings as they are after the change.
public class ToggleNewsletter : StrictlyLocal.Tommands.StrictlyLocalTommand<AccountSettings>;
#endregion

static class InOneProcessExample
{
   internal static async Task RunAsync()
   {
      #region composing
      IContainerBuilder containerBuilder = new MicrosoftContainerBuilder();
      containerBuilder.Registrar
                      .Register(Singleton.For<SettingsStore>().CreatedBy(() => new SettingsStore()))
                      .LocalTessagingEngine(engine => engine
                         .RegisterTypermediaHandlers(handle => handle
                            .ForTuery((AccountSettingsLink _, SettingsStore store) => store.Current)
                            .ForTommand((ChangeDisplayName tommand, SettingsStore store) => store.ChangeDisplayName(tommand.DisplayName))
                            .ForTommand((ToggleNewsletter _, SettingsStore store) => store.ToggleNewsletter())));

      await using var container = containerBuilder.Build();
      #endregion

      #region executing_independently
      var navigator = container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

      var settings = navigator.Execute(new AccountSettingsLink());
      navigator.Execute(settings.ChangeDisplayName("Ada"));
      settings = navigator.Execute(settings.ToggleNewsletter());
      #endregion

      #region executing_in_a_unit_of_work
      container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
      {
         var session = unitOfWork.Resolve<ILocalTypermediaNavigatorSession>();
         var settings = session.Execute(new AccountSettingsLink());
         session.Execute(settings.ChangeDisplayName("Ada Lovelace")); //Commits, or rolls back, with the unit of work.
      });
      #endregion

      #region executing_asynchronously
      settings = await navigator.ExecuteAsync(new AccountSettingsLink());
      await navigator.ExecuteAsync(settings.ChangeDisplayName("Ada"));

      await container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork =>
      {
         var session = unitOfWork.Resolve<ILocalTypermediaNavigatorSession>();
         var current = await session.ExecuteAsync(new AccountSettingsLink());
         await session.ExecuteAsync(current.ChangeDisplayName("Ada Lovelace"));
      });
      #endregion

      Console.WriteLine($"{navigator.Execute(new AccountSettingsLink()).DisplayName}, newsletter: {settings.ReceivesNewsletter}");
      await container.RootResolver.Resolve<ILocalTessagingEngine>().StopAsync();
   }
}

class SettingsStore
{
   public AccountSettings Current { get; private set; } = new("", receivesNewsletter: false);

   public void ChangeDisplayName(string displayName) => Current = new AccountSettings(displayName, Current.ReceivesNewsletter);

   public AccountSettings ToggleNewsletter() => Current = new AccountSettings(Current.DisplayName, !Current.ReceivesNewsletter);
}
