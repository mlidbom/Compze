using AccountManagement.API;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE.CollectionsCE.ConcurrentCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using AccountId = AccountManagement.Domain.AccountId;

namespace AccountManagement;

public class PerformanceTest : UniversalTestBase
{
   ITestingEndpointHost? _host;
   IClient? _client;
   AccountScenarioApi? _scenarioApi;

   protected override async Task InitializeAsyncInternal()
   {
      _host = TestingEndpointHost.Create();
      var endpoint = new AccountManagementServerDomainBootstrapper().RegisterWith(_host);
      await _host.StartAsync().caf();
      _client = await TestClient.ConnectTo(endpoint.Address!).caf();
      _scenarioApi = new AccountScenarioApi(_client);
      //Warmup
      StopwatchCE.TimeExecutionThreaded(() => _scenarioApi.Register.Execute(), iterations: 10);
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

   [PCT] public void Multithreaded_creates_XX_accounts_in_60_milliseconds__db2_memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.ExecuteThreaded(
         description: "Register accounts",
         action: () => _scenarioApi!.Register.Execute().Result.Status.Must().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 2, pgSql: 4, sqlite: 2, sqliteMemory: 2),
         maxTotal: 60.Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.4));

   [PCT] public void Multithreaded_logs_in_XX_times_in_100_milliseconds()
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

   [PCT] public void Multithreaded_fetches_XX_account_resources_in_20_milliseconds()
   {
      var fetches = TestEnv.SqlLayer.ValueFor(msSql: 10, mySql: 10, pgSql: 12, sqlite: 10, sqliteMemory: 10);
      var accountsReader = CreateAccountsThreaded(Math.Min(fetches, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Fetch account resource",
                                   action: () =>
                                   {
                                      var accountId = accountsReader.Next().Id;
                                      _client!.ExecuteRequest(AccountApi.Instance.Tuery.AccountById(accountId)).Id.Must().Be(accountId);
                                   },
                                   iterations: fetches,
                                   maxTotal: 20.Milliseconds());
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
            _client!.ExecuteRequest(AccountApi.Instance.Tuery.AccountById(result.RegisteredAccount!.Id)).Id.Must().Be(result.RegisteredAccount.Id);
            created.Add((registerAccountScenario.Email, registerAccountScenario.Password, registerAccountScenario.AccountId));
         },
         iterations: accountCount);
      return created;
   }
}
