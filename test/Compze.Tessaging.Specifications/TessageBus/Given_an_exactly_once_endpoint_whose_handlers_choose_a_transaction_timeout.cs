using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>An exactly-once endpoint whose handlers' registrations choose a transaction timeout of a second, or choose none, and<br/>
/// another exactly-once endpoint that sends and publishes to it. The first try of each tessage's handling takes two seconds,<br/>
/// inside its transaction; every later try takes none.</summary>
public abstract class Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout : UniversalTestBase
{
   static readonly WaitTimeout HandlingTimeout = WaitTimeout.Seconds(30);
   static readonly TimeSpan HowLongTheFirstTryTakes = TimeSpan.FromSeconds(2);

   readonly TestingEndpointHost _host = TestingEndpointHost.Create();
   readonly ExactlyOnceEndpoint _endpoint;
   readonly ExactlyOnceEndpoint _otherEndpoint;
   readonly ConcurrentQueue<string> _tries = new();

   protected Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout()
   {
      _endpoint = _host.RegisterEndpoint(new TheEndpoint(this));
      _otherEndpoint = _host.RegisterEndpoint(new TheOtherEndpoint());
   }

   ///<summary>Passed through once by each tessage handled, as its handling's unit of work commits.</summary>
   IThreadGate HandledGate { get; } = IThreadGate.NewOpen(HandlingTimeout, nameof(HandledGate));

   protected int TriesOf(string tessageName) => _tries.Count(it => it == tessageName);

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   protected async Task SendInAUnitOfWorkAsync(IExactlyOnceTommand tommand) =>
      await _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWorkAsync(async unitOfWork =>
         await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(tommand, CancellationToken.None));

   protected async Task SendFromTheOtherEndpointAsync(IExactlyOnceTommand tommand) =>
      await _otherEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(tommand);

   protected async Task PublishFromTheOtherEndpointAsync(IExactlyOnceTevent tevent) =>
      await _otherEndpoint.ServiceLocator.Resolve<IIndependentTeventPublisher>().PublishAsync(tevent);

   protected async Task AwaitHandledAsync() => await HandledGate.AwaitPassedThroughCountEqualToAsync(1, HandlingTimeout);

   async Task HandleAsync(INamedTessage tessage, IUnitOfWorkResolver unitOfWork)
   {
      _tries.Enqueue(tessage.Name);
      unitOfWork.OnCommittedSuccessfully(() => HandledGate.AwaitPassThrough());
      if(TriesOf(tessage.Name) == 1) await Task.Delay(HowLongTheFirstTryTakes);
   }

   public class when_a_unit_of_work_sends_it_a_step_whose_deferred_handler_has_a_transaction_timeout_of_a_second
      : Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendInAUnitOfWorkAsync(new StepWithATransactionTimeoutOfASecond("step"));
         await AwaitHandledAsync();
      }

      [PCT] public void the_first_try_whose_transaction_times_out_is_followed_by_a_second_that_handles_the_step() => TriesOf("step").Must().Be(2);
   }

   public class when_the_other_endpoint_sends_it_a_tommand_whose_handler_has_a_transaction_timeout_of_a_second
      : Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendFromTheOtherEndpointAsync(new TommandWithATransactionTimeoutOfASecond("tommand"));
         await AwaitHandledAsync();
      }

      [PCT] public void the_first_try_whose_transaction_times_out_is_followed_by_a_second_that_handles_the_tommand() => TriesOf("tommand").Must().Be(2);
   }

   public class when_a_unit_of_work_of_the_endpoint_itself_sends_it_a_tommand_whose_handler_has_a_transaction_timeout_of_a_second
      : Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await SendInAUnitOfWorkAsync(new TommandWithATransactionTimeoutOfASecond("tommand"));
         await AwaitHandledAsync();
      }

      //The handler runs in its sender's unit of work, whose transaction has the default timeout.
      [PCT] public void the_first_try_handles_the_tommand() => TriesOf("tommand").Must().Be(1);
   }

   public class when_the_other_endpoint_publishes_a_tevent_whose_handlers_all_have_a_transaction_timeout_of_a_second
      : Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await PublishFromTheOtherEndpointAsync(new TeventWhoseHandlersAllHaveATransactionTimeoutOfASecond("tevent"));
         await AwaitHandledAsync();
      }

      [PCT] public void the_first_try_whose_transaction_times_out_is_followed_by_a_second_that_handles_the_tevent() => TriesOf("tevent").Must().Be(2);
   }

   public class when_the_other_endpoint_publishes_a_tevent_one_of_whose_handlers_has_a_transaction_timeout_of_a_second_and_the_other_none
      : Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await PublishFromTheOtherEndpointAsync(new TeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond("tevent"));
         await AwaitHandledAsync();
      }

      //The tevent's handling transaction gets the longest timeout of its handlers': the default, of the one that chose none.
      [PCT] public void the_first_try_handles_the_tevent() => TriesOf("tevent").Must().Be(1);
   }

   class TheOtherEndpoint : ExactlyOnceEndpointDeclaration<TheOtherEndpoint>, IEndpointIdentity
   {
      public static string Name => "TransactionTimeoutSender";
      public static EndpointId Id => new(Guid.Parse("7E2B4C91-3D58-4A06-B8F1-25C9E0A47D63"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();
   }

   class TheEndpoint : ExactlyOnceEndpointDeclaration<TheEndpoint>, IEndpointIdentity
   {
      static readonly TimeSpan ASecond = TimeSpan.FromSeconds(1);

      public static string Name => "TransactionTimeoutHandlers";
      public static EndpointId Id => new(Guid.Parse("D4A97E13-6B20-4F85-9C3E-81F5B2D60A7C"));

      readonly Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout _specification;
      internal TheEndpoint(Given_an_exactly_once_endpoint_whose_handlers_choose_a_transaction_timeout specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingSpecificationTypeMappings();

      protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
         .ForTommand((StepWithATransactionTimeoutOfASecond step, IUnitOfWorkResolver unitOfWork) => _specification.HandleAsync(step, unitOfWork), transactionTimeout: ASecond);

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandWithATransactionTimeoutOfASecond tommand, IUnitOfWorkResolver unitOfWork) => _specification.HandleAsync(tommand, unitOfWork), transactionTimeout: ASecond);

      protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
         .ForTevent((ITeventWhoseHandlersAllHaveATransactionTimeoutOfASecond tevent, IUnitOfWorkResolver unitOfWork) => _specification.HandleAsync(tevent, unitOfWork), transactionTimeout: ASecond)
         .ForTevent((ITeventWhoseHandlersAllHaveATransactionTimeoutOfASecond _, IUnitOfWorkResolver _) => Task.CompletedTask, transactionTimeout: ASecond)
         .ForTevent((ITeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond tevent, IUnitOfWorkResolver unitOfWork) => _specification.HandleAsync(tevent, unitOfWork), transactionTimeout: ASecond)
         .ForTevent((ITeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond _, IUnitOfWorkResolver _) => Task.CompletedTask);
   }
}
