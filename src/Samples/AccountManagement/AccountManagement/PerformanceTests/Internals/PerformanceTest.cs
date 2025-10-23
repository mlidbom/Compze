using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE.CollectionsCE.ConcurrentCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;
using FluentAssertions.Extensions;


using Compze.Utilities.Threading.TasksCE;
using Compze.Wiring.Testing.Sql;

namespace AccountManagement;

[Performance]
public class PerformanceTest : UniversalTestBase
{
   ITestingEndpointHost? _host;
   IEndpoint? _clientEndpoint;
   AccountScenarioApi? _scenarioApi;

   protected override async Task InitializeAsyncInternal()
   {
      _host = TestingEndpointHost.Create(runMode => TestEnv.DIContainer.CreateWithRegisteredServiceLocator());
      new AccountManagementServerDomainBootstrapper().RegisterWith(_host);
      _clientEndpoint = _host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      _scenarioApi = new AccountScenarioApi(_clientEndpoint);
      await _host.StartAsync().caf();
      //Warmup
      StopwatchCE.TimeExecutionThreaded(() => _scenarioApi.Register.Execute(), iterations: 10);
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _host!.DisposeAsync().caf();
      if(_clientEndpoint != null) await _clientEndpoint.DisposeAsync();
   }

   [PCT] public void SingleThreaded_creates_XX_accounts_in_100_milliseconds_db2__memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.Execute(
         description: "Register accounts",
         action: () => _scenarioApi!.Register.Execute().Result.Status.Should().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.SqlLayer.ValueFor(msSql: 6, mySql: 6, pgSql: 6, sqlite: 6, sqliteMemory:6),
         maxTotal: 100.Milliseconds().EnvMultiply(1.6));

   [PCT] public void Multithreaded_creates_XX_accounts_in_20_milliseconds__db2_memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.ExecuteThreaded(
         description: "Register accounts",
         action: () => _scenarioApi!.Register.Execute().Result.Status.Should().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.SqlLayer.ValueFor(msSql: 4, mySql: 1, pgSql: 3, sqlite: 4, sqliteMemory: 4),
         maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.4));

   [PCT] public void Multithreaded_logs_in_XX_times_in_100_milliseconds_db2__memory__msSql__mySql__oracle_pgSql_()
   {
      var logins = TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 3, pgSql: 8, sqlite: 8, sqliteMemory: 8);
      var accountsReader = CreateAccountsThreaded(Math.Min(logins, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Log in to account",
                                   action: () =>
                                   {
                                      var (email, password, _) = accountsReader.Next();
                                      _scenarioApi!.Login(email, password).Execute().Succeeded.Should().BeTrue();
                                   },
                                   iterations: logins,
                                   maxTotal: 100.Milliseconds());
   }

   [PCT] public void Multithreaded_fetches_XX_account_resources_in_20_milliseconds_db2_memory__msSql__mySql__oracle_pgSql_()
   {
      var fetches = TestEnv.SqlLayer.ValueFor(msSql: 20, mySql: 20, pgSql: 30, sqlite: 20, sqliteMemory: 20);
      var accountsReader = CreateAccountsThreaded(Math.Min(fetches, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Fetch account resource",
                                   action: () =>
                                   {
                                      var accountId = accountsReader.Next().Id;
                                      _clientEndpoint!.ExecuteClientRequest(AccountApi.Instance.Query.AccountById(accountId)).Id.Should().Be(accountId);
                                   },
                                   iterations: fetches,
                                   maxTotal: 20.Milliseconds());
   }

   ConcurrentBag<(string Email, string Password, Guid Id)> CreateAccountsThreaded(int accountCount)
   {
      var created = new ConcurrentBag<(string Email, string Password, Guid Id)>();

      StopwatchCE.TimeExecutionThreaded(
         () =>
         {
            var registerAccountScenario = _scenarioApi!.Register;
            var result = registerAccountScenario.Execute().Result;
            result.Status.Should().Be(RegistrationAttemptStatus.Successful);
            _clientEndpoint!.ExecuteClientRequest(AccountApi.Instance.Query.AccountById(result.RegisteredAccount!.Id)).Id.Should().Be(result.RegisteredAccount.Id);
            created.Add((registerAccountScenario.Email, registerAccountScenario.Password, registerAccountScenario.AccountId));
         },
         iterations: accountCount);
      return created;
   }
}