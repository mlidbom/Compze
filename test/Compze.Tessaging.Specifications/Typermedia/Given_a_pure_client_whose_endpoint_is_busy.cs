using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

using Transport = Compze.Hosting.Testing.Transport; //Inside the namespace, so it outranks the Compze.Tessaging.Transport namespace, which Transport would otherwise resolve to here.

///<summary>What a pure client (<see cref="TypermediaClient"/>) is told when the endpoint it converses with does not answer<br/>
/// as fast as the client sends to it. Over named pipes and TCP the client has 8 connections to the endpoint, and a tuery<br/>
/// holds one until its answer has arrived. A tuery that has waited 10 seconds for one of them to become free is never<br/>
/// sent: it throws an <see cref="EndpointBusyException"/>. The tueries that hold the connections are unharmed.</summary>
///<remarks>Every specification here waits those 10 seconds.</remarks>
[LongRunning]
public class Given_a_pure_client_whose_endpoint_is_busy : UniversalTestBase
{
   const string HttpNeverFindsAnEndpointBusy = "The HTTP transport's client bounds neither its connections to an endpoint nor its tueries awaiting answers: it sends all 9 tueries, and none of them ends before the endpoint answers.";

   readonly TestingEndpointHost _host = TestingEndpointHost.Create();
   readonly TaskCompletionSource _theEndpointIsToldToAnswer = new(TaskCreationOptions.RunContinuationsAsynchronously);
   readonly BestEffortEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Given_a_pure_client_whose_endpoint_is_busy() =>
      _endpoint = _host.RegisterEndpoint(new EndpointAnsweringOnlyOnceToldToDeclaration(_theEndpointIsToldToAnswer.Task));

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings());
   }

   protected override async Task DisposeAsyncInternal()
   {
      _theEndpointIsToldToAnswer.TrySetResult(); //So that no tuery is still being handled when the host stops, however the specification ended.
      await _client.DisposeAsync();
      await _host.DisposeAsync();
   }

   public class answering_none_of_the_9_tueries_the_client_sent_it_at_once : Given_a_pure_client_whose_endpoint_is_busy
   {
      IReadOnlyList<Task<Greeting>> _tueries = null!;
      Task<Greeting> _theTueryThatEndedFirst = null!;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         _tueries = [.. 1.Through(9).Select(number => _client.Navigator.GetAsync(new GreetingTuery { Name = $"tuery {number}" }))];
         _theTueryThatEndedFirst = await Task.WhenAny(_tueries).WaitAsync(TimeSpan.FromSeconds(60));
      }

      protected override async Task DisposeAsyncInternal()
      {
         _theEndpointIsToldToAnswer.TrySetResult();
         await EveryTueryHasEnded();
         await base.DisposeAsyncInternal();
      }

      [Skip<Transport>([Transport.AspNetCore], HttpNeverFindsAnEndpointBusy)]
      [PCT] public async Task the_tuery_that_waited_10_seconds_for_one_of_the_8_connections_the_others_hold_throws_EndpointBusyException() =>
         await InvokingAsync(() => _theTueryThatEndedFirst).Must().ThrowAsync<EndpointBusyException>();

      public class once_the_endpoint_answers : answering_none_of_the_9_tueries_the_client_sent_it_at_once
      {
         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            _theEndpointIsToldToAnswer.SetResult();
            await EveryTueryHasEnded();
         }

         [Skip<Transport>([Transport.AspNetCore], HttpNeverFindsAnEndpointBusy)]
         [PCT] public void the_other_8_tueries_are_answered() =>
            _tueries.Count(tuery => tuery.IsCompletedSuccessfully).Must().Be(8);
      }

      ///<summary>Awaits the end of every tuery, however it ended: the tuery that was never sent ended in an exception, which<br/>
      /// is for the specifications to assert, not for this to throw.</summary>
      async Task EveryTueryHasEnded() => await Task.WhenAny(Task.WhenAll(_tueries)).WaitAsync(TimeSpan.FromSeconds(60));
   }

   ///<summary>An endpoint whose tuery handler answers no <see cref="GreetingTuery"/> until the specification tells it to.</summary>
   ///<remarks>The handler awaits a task, where a thread gate would hold a thread for every tuery being handled: eight<br/>
   /// threads held for ten seconds would starve the thread pool of every specification running beside this one.</remarks>
   class EndpointAnsweringOnlyOnceToldToDeclaration : BestEffortEndpointDeclaration<EndpointAnsweringOnlyOnceToldToDeclaration>, IEndpointIdentity
   {
      readonly Task _toldToAnswer;
      internal EndpointAnsweringOnlyOnceToldToDeclaration(Task toldToAnswer) => _toldToAnswer = toldToAnswer;

      public static string Name => "GreetingEndpointAnsweringOnlyOnceToldTo";
      public static EndpointId Id => new(Guid.Parse("3f6c2b8e-91d4-4a7b-8c05-d2e7a4b61f93"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTypermediaHostingSpecificationTypeMappings();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery<GreetingTuery, Greeting>(async (tuery, _) =>
         {
            await _toldToAnswer;
            return new Greeting { Message = $"Hello {tuery.Name}!" };
         });
   }
}
