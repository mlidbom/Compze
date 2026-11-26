using System.Diagnostics;
using System.Transactions;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Integration.InProcess;
using Compze.Tests.Infrastructure.XUnit;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>Spike (2026-11-26, dev_docs/async-local-navigators.md): the asynchronous forms of the local navigators,<br/>
/// <see cref="IIndependentLocalTypermediaNavigator.ExecuteAsync{TResult}(Compze.Tessaging.TessageTypes.IStrictlyLocalTommand{TResult})"/><br/>
/// and <see cref="ILocalTypermediaNavigatorSession.ExecuteAsync{TResult}(Compze.Tessaging.TessageTypes.IStrictlyLocalTommand{TResult})"/>.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   public class with_a_declared_strictly_local_tuery_handler_and_tommand_handler_navigated_asynchronously : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _registeredGreeters = [];

      public with_a_declared_strictly_local_tuery_handler_and_tommand_handler_navigated_asynchronously() =>
         ComposeContainerWithEngine(engine => engine.RegisterTypermediaHandlers(handle => handle
            .ForTuery((MyStrictlyLocalGreetingTuery tuery) => new MyGreeting { Message = $"Hello {tuery.Name}!" })
            .ForTommand((MyStrictlyLocalRegisterGreeterTommand tommand) => _registeredGreeters.Add(tommand.Name))
            .ForTommand((MyStrictlyLocalRegisterGreeterReturningTotalCountTommand tommand) =>
             {
                _registeredGreeters.Add(tommand.Name);
                return _registeredGreeters.Count;
             })));

      IIndependentLocalTypermediaNavigator Navigator => Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

      [PCT] public async Task the_independent_navigator_returns_the_tuery_handlers_result() =>
         (await Navigator.ExecuteAsync(new MyStrictlyLocalGreetingTuery { Name = "World" })).Message.Must().Be("Hello World!");

      [PCT] public async Task the_independent_navigator_invokes_the_tommand_handler_in_its_own_unit_of_work()
      {
         await Navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" });
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public async Task the_independent_navigator_returns_the_tommand_handlers_result() =>
         (await Navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterReturningTotalCountTommand { Name = "Greta" })).Must().Be(1);

      [PCT] public async Task the_navigator_session_invokes_the_tommand_handler_within_the_callers_unit_of_work()
      {
         await Container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" }));
         _registeredGreeters.Single().Must().Be("Greta");
      }

      [PCT] public async Task the_independent_navigator_fails_from_within_an_ambient_transaction() =>
         (await InvokingAsync(async () =>
                 {
                    using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    await Navigator.ExecuteAsync(new MyStrictlyLocalGreetingTuery { Name = "World" });
                 })
                .Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("ambient transaction");
   }

   ///<summary>Spike: what the asynchronous form saves a caller that is asynchronous itself, such as an ASP.NET Core route, when the<br/>
   /// handler awaits. Each navigation starts on a thread-pool thread, as a route's does.</summary>
   public class with_a_strictly_local_tommand_handler_that_awaits_a_second : Given_a_container_composing_a_LocalTessagingEngine
   {
      const int Navigations = 200;
      static readonly TimeSpan HandlersAwait = TimeSpan.FromSeconds(1);

      public with_a_strictly_local_tommand_handler_that_awaits_a_second() =>
         ComposeContainerWithEngine(engine => engine.RegisterTypermediaHandlers(handle => handle
            .ForTommand(async (MyStrictlyLocalRegisterGreeterTommand _) => await Task.Delay(HandlersAwait))));

      IIndependentLocalTypermediaNavigator Navigator => Container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

      [PCT] public async Task two_hundred_navigations_at_once_through_ExecuteAsync_complete_in_about_the_second_one_takes()
      {
         var elapsed = await TimeOfNavigationsAtOnce(() => Task.Run(() => Navigator.ExecuteAsync(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" })));
         Report($"{Navigations} at once through ExecuteAsync: {elapsed.TotalMilliseconds:F0} ms");
         elapsed.Must().BeLessThan(HandlersAwait * 5);
      }

      ///<summary>A measurement for the spike, asserting nothing: each navigation blocks its pool thread for the second its handler awaits.<br/>
      /// Skipped: it starves the pool for as long as the pool takes to grow, which slows every specification running beside it.</summary>
      [PCT(Skip = "Spike measurement: starves the thread pool. Run it on its own.")] public async Task two_hundred_navigations_at_once_through_Execute_are_measured()
      {
         var elapsed = await TimeOfNavigationsAtOnce(() => Task.Run(() => Navigator.Execute(new MyStrictlyLocalRegisterGreeterTommand { Name = "Greta" })));
         Report($"{Navigations} at once through Execute: {elapsed.TotalMilliseconds:F0} ms");
      }

      static async Task<TimeSpan> TimeOfNavigationsAtOnce(Func<Task> startANavigation)
      {
         var stopwatch = Stopwatch.StartNew();
         await Task.WhenAll(Enumerable.Range(0, Navigations).Select(_ => startANavigation()));
         return stopwatch.Elapsed;
      }

      static void Report(string measurement)
      {
         ThreadPool.GetMinThreads(out var minimumWorkerThreads, out _);
         TestContext.Current.TestOutputHelper?.WriteLine($"{measurement}; {Environment.ProcessorCount} processors, pool minimum {minimumWorkerThreads}, pool threads now {ThreadPool.ThreadCount}");
      }
   }
}
