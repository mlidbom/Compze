using AccountManagement.API;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Compze.xUnitMatrix;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE.CollectionsCE.ConcurrentCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Internals.SystemCE;
using System.Collections.Concurrent;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using AccountId = AccountManagement.Domain.AccountId;
using Compze.Tessaging.Typermedia;

namespace AccountManagement;

public class PerformanceTest : UniversalTestBase
{
   TestingEndpointHost? _host;
   TypermediaTestClient? _client;
   AccountScenarioApi? _scenarioApi;

   protected override async Task InitializeAsyncInternal()
   {
      _host = TestingEndpointHost.Create();
      var endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(_host);
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(endpoint.Address!, registrar => registrar.RequireAccountManagementTypeMappings()).caf();
      _scenarioApi = new AccountScenarioApi(_client.Navigator);
      //Warmup
      StopwatchCE.TimeExecution(() => _scenarioApi.Register.Execute(), iterations: 10);
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _host!.DisposeAsync().caf();
      if(_client != null) await _client.DisposeAsync();
   }

    [PCT] public void SingleThreaded_creates_XX_accounts_in_100_milliseconds() =>
      TimeAsserter.Execute(
         description: "Register accounts",
         action: () => _scenarioApi!.Register.Execute().Result.Status.Must().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.SqlLayer.ValueFor(msSql: 6, mySql: 3, pgSql: 4, sqlite: 3, sqliteMemory:6),
         maxTotal: 100.Milliseconds().EnvMultiply(1.6));

   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite deadlocks under parallel writes")]
   public void Multithreaded_creates_XX_accounts_in_60_milliseconds__db2_memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.ExecuteThreaded(
         description: "Register accounts",
         action: () => _scenarioApi!.Register.Execute().Result.Status.Must().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 2, pgSql: 4, sqlite: 2, sqliteMemory: 2),
         maxTotal: 60.Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.4));

   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite deadlocks under parallel writes")]
   public void Multithreaded_logs_in_XX_times_in_100_milliseconds()
   {
      var logins = TestEnv.SqlLayer.ValueFor(msSql: 6, mySql: 3, pgSql: 6, sqlite: 2, sqliteMemory: 3);
      var accountsReader = CreateAccountsThreaded(Math.Min(logins, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Log in to account",
                                   action: () =>
                                   {
                                      var (email, password, _) = accountsReader.Next();
                                      _scenarioApi!.Login(email, password).Execute().Succeeded.Must().BeTrue();
                                   },
                                   iterations: logins,
                                   maxTotal: 100.Milliseconds());
   }

   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite deadlocks under parallel writes")]
   public void Multithreaded_fetches_XX_account_resources_in_30_milliseconds()
   {
      var fetches = TestEnv.SqlLayer.ValueFor(msSql: 10, mySql: 10, pgSql: 12, sqlite: 10, sqliteMemory: 10);
      var accountsReader = CreateAccountsThreaded(Math.Min(fetches, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Fetch account resource",
                                   action: () =>
                                   {
                                      var accountId = accountsReader.Next().Id;
                                      _client!.Navigator.Navigate(AccountApi.Instance.Tuery.AccountById(accountId)).Id.Must().Be(accountId);
                                   },
                                   iterations: fetches,
                                   maxTotal: 30.Milliseconds());
   }

   ConcurrentBag<(string Email, string Password, AccountId Id)> CreateAccountsThreaded(int accountCount)
   {
      var created = new ConcurrentBag<(string Email, string Password, AccountId Id)>();

      StopwatchCE.TimeExecutionThreaded(
         () =>
         {
            var registerAccountScenario = _scenarioApi!.Register;
            var result = registerAccountScenario.Execute().Result;
            result.Status.Must().Be(RegistrationAttemptStatus.Successful);
            _client!.Navigator.Navigate(AccountApi.Instance.Tuery.AccountById(result.RegisteredAccount!.Id)).Id.Must().Be(result.RegisteredAccount.Id);
            created.Add((registerAccountScenario.Email, registerAccountScenario.Password, registerAccountScenario.AccountId));
         },
         iterations: accountCount);
      return created;
   }
}
