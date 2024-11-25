﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.SystemCE.CollectionsCE.ConcurrentCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement;

class PerformanceTest : DuplicateByPluggableComponentTest
{
   public PerformanceTest([NotNull] string pluggableComponentsColonSeparated) : base(pluggableComponentsColonSeparated) {}

   ITestingEndpointHost _host;
   IEndpoint _clientEndpoint;
   AccountScenarioApi _scenarioApi;

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      _host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(_host);
      _clientEndpoint = _host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      _scenarioApi = new AccountScenarioApi(_clientEndpoint);
      await _host.StartAsync().NoMarshalling();
      //Warmup
      StopwatchCE.TimeExecutionThreaded(() => _scenarioApi.Register.Execute(), iterations: 10);
   }

   [Test] public void SingleThreaded_creates_XX_accounts_in_30_milliseconds_db2__memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.Execute(
         description: "Register accounts",
         action: () => _scenarioApi.Register.Execute().Result.Status.Should().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.PersistenceLayer.ValueFor(db2: 2, memory: 2, msSql: 2, mySql: 2, orcl: 2, pgSql: 2),
         maxTotal: 30.Milliseconds().EnvMultiply(1.6));

   [Test] public void Multithreaded_creates_XX_accounts_in_20_milliseconds__db2_memory__msSql__mySql__oracle_pgSql_() =>
      TimeAsserter.ExecuteThreaded(
         description: "Register accounts",
         action: () => _scenarioApi.Register.Execute().Result.Status.Should().Be(RegistrationAttemptStatus.Successful),
         iterations: TestEnv.PersistenceLayer.ValueFor(db2: 2, memory: 10, msSql: 4, mySql: 1, orcl: 2, pgSql: 3),
         maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.2, unoptimized:1.4));

   [Test] public void Multithreaded_logs_in_XX_times_in_100_milliseconds_db2__memory__msSql__mySql__oracle_pgSql_()
   {
      var logins = TestEnv.PersistenceLayer.ValueFor(db2: 8, memory: 8, msSql: 8, mySql: 7, orcl: 8, pgSql: 8);
      var accountsReader = CreateAccountsThreaded(Math.Min(logins, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Log in to account",
                                   action: () =>
                                   {
                                      var (email, password, _) = accountsReader.Next();
                                      _scenarioApi.Login(email, password).Execute().Succeeded.Should().BeTrue();
                                   },
                                   iterations: logins,
                                   maxTotal: 100.Milliseconds());
   }

   [Test] public void Multithreaded_fetches_XX_account_resources_in_20_milliseconds_db2_memory__msSql__mySql__oracle_pgSql_()
   {
      var fetches = TestEnv.PersistenceLayer.ValueFor(db2: 20, memory: 40, msSql: 25, mySql: 20, orcl: 25, pgSql: 40);
      var accountsReader = CreateAccountsThreaded(Math.Min(fetches, 10)).ToConcurrentCircularReader();

      TimeAsserter.ExecuteThreaded(description: "Fetch account resource",
                                   action: () =>
                                   {
                                      var accountId = accountsReader.Next().Id;
                                      _clientEndpoint.ExecuteClientRequest(AccountApi.Instance.Query.AccountById(accountId)).Id.Should().Be(accountId);
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
            var registerAccountScenario = _scenarioApi.Register;
            var result = registerAccountScenario.Execute().Result;
            result.Status.Should().Be(RegistrationAttemptStatus.Successful);
            _clientEndpoint.ExecuteClientRequest(AccountApi.Instance.Query.AccountById(result.RegisteredAccount.Id)).Id.Should().Be(result.RegisteredAccount.Id);
            created.Add((registerAccountScenario.Email, registerAccountScenario.Password, registerAccountScenario.AccountId));
         },
         iterations: accountCount);
      return created;
   }

   [TearDown] public async Task Teardown() => await _host.DisposeAsync().NoMarshalling();
}