using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Integration.InProcess;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Engine;

///<summary>The registration-overload vocabulary (see the main partial): every <c>ForTommand</c>/<c>ForTuery</c>/<c>ForTevent</c><br/>
/// overload — the core resolver-taking verbs and every convenience form that resolves extra lambda parameters — declares a<br/>
/// handler that actually handles, with its dependencies actually resolved. One handler per overload, one tessage through each.</summary>
public partial class Given_a_container_composing_a_LocalTessagingEngine
{
   static void RegisterServicesResolvedIntoHandlers(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<FirstServiceResolvedIntoHandlers>().CreatedBy(() => new FirstServiceResolvedIntoHandlers()),
         Scoped.For<SecondServiceResolvedIntoHandlers>().CreatedBy(() => new SecondServiceResolvedIntoHandlers()),
         Scoped.For<ThirdServiceResolvedIntoHandlers>().CreatedBy(() => new ThirdServiceResolvedIntoHandlers()),
         Scoped.For<FourthServiceResolvedIntoHandlers>().CreatedBy(() => new FourthServiceResolvedIntoHandlers()));

   public class with_typermedia_handlers_declared_through_every_registration_overload : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _handledTommands = [];
      readonly List<object> _dependenciesResolvedIntoVoidTommandHandlers = [];

      public with_typermedia_handlers_declared_through_every_registration_overload() =>
         ComposeContainerWithEngine(RegisterServicesResolvedIntoHandlers, engine => engine.RegisterTypermediaHandlers(handle => handle
            .ForTommand((TommandHandledByThePlainActionOverload _) => _handledTommands.Add(nameof(TommandHandledByThePlainActionOverload)))
            .ForTommand((TommandHandledByTheAsyncOverload _) =>
             {
                _handledTommands.Add(nameof(TommandHandledByTheAsyncOverload));
                return Task.CompletedTask;
             })
            .ForTommand((TommandHandledByTheActionWithDependencyOverload _, FirstServiceResolvedIntoHandlers resolved) =>
             {
                _handledTommands.Add(nameof(TommandHandledByTheActionWithDependencyOverload));
                _dependenciesResolvedIntoVoidTommandHandlers.Add(resolved);
             })
            .ForTommand((TommandHandledByTheAsyncWithDependencyOverload _, SecondServiceResolvedIntoHandlers resolved) =>
             {
                _handledTommands.Add(nameof(TommandHandledByTheAsyncWithDependencyOverload));
                _dependenciesResolvedIntoVoidTommandHandlers.Add(resolved);
                return Task.CompletedTask;
             })
            .ForTommand((TommandHandledByTheCoreUnitOfWorkResolverOverload _, IUnitOfWorkResolver unitOfWork) =>
             {
                _handledTommands.Add(nameof(TommandHandledByTheCoreUnitOfWorkResolverOverload));
                _dependenciesResolvedIntoVoidTommandHandlers.Add(unitOfWork.Resolve<ThirdServiceResolvedIntoHandlers>());
                return Task.CompletedTask;
             })
            //The result-bearing ForTommand overloads are exercised at endpoint level with an at-most-once typermedia tommand: a
            //strictly-local tommand with a result cannot validly exist today — see the tessage-type-design finding in the sweep report.
            .ForTuery((TueryAnsweredByThePlainOverload _) => "answered by the plain overload")
            .ForTuery((TueryAnsweredByTheOneDependencyOverload _, FirstServiceResolvedIntoHandlers first) => $"resolved: {NamesOf(first)}")
            .ForTuery((TueryAnsweredByTheTwoDependencyOverload _, FirstServiceResolvedIntoHandlers first, SecondServiceResolvedIntoHandlers second) => $"resolved: {NamesOf(first, second)}")
            .ForTuery((TueryAnsweredByTheThreeDependencyOverload _, FirstServiceResolvedIntoHandlers first, SecondServiceResolvedIntoHandlers second, ThirdServiceResolvedIntoHandlers third) => $"resolved: {NamesOf(first, second, third)}")
            .ForTuery((TueryAnsweredByTheFourDependencyOverload _, FirstServiceResolvedIntoHandlers first, SecondServiceResolvedIntoHandlers second, ThirdServiceResolvedIntoHandlers third, FourthServiceResolvedIntoHandlers fourth) => $"resolved: {NamesOf(first, second, third, fourth)}")
            .ForTuery((TueryAnsweredByTheCoreAsyncOverload _, IScopeResolver scope) => Task.FromResult($"resolved: {NamesOf(scope.Resolve<FourthServiceResolvedIntoHandlers>())}"))));

      static string NamesOf(params object[] resolvedServices) => string.Join(", ", resolvedServices.Select(it => it.GetType().Name));

      void ExecuteInUnitOfWork(IStrictlyLocalTommand tommand) =>
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().Execute(tommand));

      TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult> =>
         Container.ScopeFactory.ExecuteInIsolatedScope(scope => scope.Resolve<ILocalTypermediaNavigatorSession>().Execute(tuery));

      [PCT] public void the_tommand_declared_through_the_plain_action_overload_is_handled()
      {
         ExecuteInUnitOfWork(new TommandHandledByThePlainActionOverload());
         _handledTommands.Single().Must().Be(nameof(TommandHandledByThePlainActionOverload));
      }

      [PCT] public void the_tommand_declared_through_the_async_overload_is_handled()
      {
         ExecuteInUnitOfWork(new TommandHandledByTheAsyncOverload());
         _handledTommands.Single().Must().Be(nameof(TommandHandledByTheAsyncOverload));
      }

      [PCT] public void the_tommand_declared_through_the_action_with_dependency_overload_is_handled_with_its_dependency_resolved()
      {
         ExecuteInUnitOfWork(new TommandHandledByTheActionWithDependencyOverload());
         _handledTommands.Single().Must().Be(nameof(TommandHandledByTheActionWithDependencyOverload));
         _dependenciesResolvedIntoVoidTommandHandlers.Single().GetType().Must().Be(typeof(FirstServiceResolvedIntoHandlers));
      }

      [PCT] public void the_tommand_declared_through_the_async_with_dependency_overload_is_handled_with_its_dependency_resolved()
      {
         ExecuteInUnitOfWork(new TommandHandledByTheAsyncWithDependencyOverload());
         _handledTommands.Single().Must().Be(nameof(TommandHandledByTheAsyncWithDependencyOverload));
         _dependenciesResolvedIntoVoidTommandHandlers.Single().GetType().Must().Be(typeof(SecondServiceResolvedIntoHandlers));
      }

      [PCT] public void the_tommand_declared_through_the_core_unit_of_work_resolver_overload_is_handled_and_resolves_through_the_received_resolver()
      {
         ExecuteInUnitOfWork(new TommandHandledByTheCoreUnitOfWorkResolverOverload());
         _handledTommands.Single().Must().Be(nameof(TommandHandledByTheCoreUnitOfWorkResolverOverload));
         _dependenciesResolvedIntoVoidTommandHandlers.Single().GetType().Must().Be(typeof(ThirdServiceResolvedIntoHandlers));
      }

      [PCT] public void the_tuery_declared_through_the_plain_overload_answers() =>
         Execute(new TueryAnsweredByThePlainOverload()).Must().Be("answered by the plain overload");

      [PCT] public void the_tuery_declared_through_the_one_dependency_overload_answers_with_its_dependency_resolved() =>
         Execute(new TueryAnsweredByTheOneDependencyOverload()).Must().Be($"resolved: {nameof(FirstServiceResolvedIntoHandlers)}");

      [PCT] public void the_tuery_declared_through_the_two_dependency_overload_answers_with_both_dependencies_resolved() =>
         Execute(new TueryAnsweredByTheTwoDependencyOverload()).Must().Be($"resolved: {nameof(FirstServiceResolvedIntoHandlers)}, {nameof(SecondServiceResolvedIntoHandlers)}");

      [PCT] public void the_tuery_declared_through_the_three_dependency_overload_answers_with_all_three_dependencies_resolved() =>
         Execute(new TueryAnsweredByTheThreeDependencyOverload()).Must().Be($"resolved: {nameof(FirstServiceResolvedIntoHandlers)}, {nameof(SecondServiceResolvedIntoHandlers)}, {nameof(ThirdServiceResolvedIntoHandlers)}");

      [PCT] public void the_tuery_declared_through_the_four_dependency_overload_answers_with_all_four_dependencies_resolved() =>
         Execute(new TueryAnsweredByTheFourDependencyOverload()).Must().Be($"resolved: {nameof(FirstServiceResolvedIntoHandlers)}, {nameof(SecondServiceResolvedIntoHandlers)}, {nameof(ThirdServiceResolvedIntoHandlers)}, {nameof(FourthServiceResolvedIntoHandlers)}");

      [PCT] public void the_tuery_declared_through_the_core_scope_resolver_overload_answers_and_resolves_through_the_received_resolver() =>
         Execute(new TueryAnsweredByTheCoreAsyncOverload()).Must().Be($"resolved: {nameof(FourthServiceResolvedIntoHandlers)}");
   }

   //The exactly-once ForTommand forms need a send door, which is endpoint-tier machinery — they are exercised in the Hosting specs
   //(Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry), as are the result-bearing typermedia forms.
   public class with_tessage_bus_handlers_declared_through_every_ForTevent_overload : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _receivingOverloads = [];
      readonly List<object> _dependenciesResolvedIntoTeventHandlers = [];

      public with_tessage_bus_handlers_declared_through_every_ForTevent_overload() =>
         ComposeContainerWithEngine(RegisterServicesResolvedIntoHandlers, engine => engine.RegisterTessageBusHandlers(handle => handle
            .ForTevent((IMyGreetingRequestedTevent _) => _receivingOverloads.Add("action"))
            .ForTevent((IMyGreetingRequestedTevent _) =>
             {
                _receivingOverloads.Add("async");
                return Task.CompletedTask;
             })
            .ForTevent((IMyGreetingRequestedTevent _, FirstServiceResolvedIntoHandlers resolved) =>
             {
                _receivingOverloads.Add("action with one dependency");
                _dependenciesResolvedIntoTeventHandlers.Add(resolved);
             })
            .ForTevent((IMyGreetingRequestedTevent _, SecondServiceResolvedIntoHandlers resolved) =>
             {
                _receivingOverloads.Add("async with one dependency");
                _dependenciesResolvedIntoTeventHandlers.Add(resolved);
                return Task.CompletedTask;
             })
            .ForTevent((IMyGreetingRequestedTevent _, FirstServiceResolvedIntoHandlers first, SecondServiceResolvedIntoHandlers second) =>
             {
                _receivingOverloads.Add("action with two dependencies");
                _dependenciesResolvedIntoTeventHandlers.Add(first);
                _dependenciesResolvedIntoTeventHandlers.Add(second);
             })
            .ForTevent((IMyGreetingRequestedTevent _, ThirdServiceResolvedIntoHandlers third, FourthServiceResolvedIntoHandlers fourth) =>
             {
                _receivingOverloads.Add("async with two dependencies");
                _dependenciesResolvedIntoTeventHandlers.Add(third);
                _dependenciesResolvedIntoTeventHandlers.Add(fourth);
                return Task.CompletedTask;
             })));

      [PCT] public void publishing_one_tevent_reaches_the_handlers_of_all_six_ForTevent_overloads_with_their_dependencies_resolved()
      {
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));

         _receivingOverloads.Must().SequenceEqual(["action", "async", "action with one dependency", "async with one dependency", "action with two dependencies", "async with two dependencies"]);
         _dependenciesResolvedIntoTeventHandlers.Select(it => it.GetType()).Must().SequenceEqual(
            [typeof(FirstServiceResolvedIntoHandlers), typeof(SecondServiceResolvedIntoHandlers), typeof(FirstServiceResolvedIntoHandlers), typeof(SecondServiceResolvedIntoHandlers), typeof(ThirdServiceResolvedIntoHandlers), typeof(FourthServiceResolvedIntoHandlers)]);
      }
   }

   public class with_tevent_observers_declared_through_every_registration_overload : Given_a_container_composing_a_LocalTessagingEngine
   {
      readonly List<string> _observingOverloads = [];
      readonly List<object> _dependenciesResolvedIntoObservers = [];
      readonly IThreadGate _observerGate = IThreadGate.NewOpen(WaitTimeout.Seconds(10), nameof(_observerGate));

      public with_tevent_observers_declared_through_every_registration_overload() =>
         ComposeContainerWithEngine(RegisterServicesResolvedIntoHandlers, engine => engine.ObserveTevents(observe => observe
            .ForTevent((IMyGreetingRequestedTevent _) =>
             {
                _observingOverloads.Add("plain");
                _observerGate.AwaitPassThrough();
             })
            .ForTevent((IMyGreetingRequestedTevent _, FirstServiceResolvedIntoHandlers resolved) =>
             {
                _observingOverloads.Add("one dependency");
                _dependenciesResolvedIntoObservers.Add(resolved);
                _observerGate.AwaitPassThrough();
             })
            .ForTevent((IMyGreetingRequestedTevent _, SecondServiceResolvedIntoHandlers second, ThirdServiceResolvedIntoHandlers third) =>
             {
                _observingOverloads.Add("two dependencies");
                _dependenciesResolvedIntoObservers.Add(second);
                _dependenciesResolvedIntoObservers.Add(third);
                _observerGate.AwaitPassThrough();
             })));

      [PCT] public void publishing_one_tevent_reaches_the_observers_of_all_three_ForTevent_overloads_with_their_dependencies_resolved()
      {
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MySpecialGreetingRequestedTevent()));
         _observerGate.AwaitPassedThroughCountEqualTo(3);

         _observingOverloads.Must().Contain("plain");
         _observingOverloads.Must().Contain("one dependency");
         _observingOverloads.Must().Contain("two dependencies");
         _dependenciesResolvedIntoObservers.Select(it => it.GetType()).Must().Contain(typeof(FirstServiceResolvedIntoHandlers));
         _dependenciesResolvedIntoObservers.Select(it => it.GetType()).Must().Contain(typeof(SecondServiceResolvedIntoHandlers));
         _dependenciesResolvedIntoObservers.Select(it => it.GetType()).Must().Contain(typeof(ThirdServiceResolvedIntoHandlers));
      }
   }
}

class FirstServiceResolvedIntoHandlers;
class SecondServiceResolvedIntoHandlers;
class ThirdServiceResolvedIntoHandlers;
class FourthServiceResolvedIntoHandlers;

class TommandHandledByThePlainActionOverload : StrictlyLocal.Tommands.StrictlyLocalTommand;
class TommandHandledByTheAsyncOverload : StrictlyLocal.Tommands.StrictlyLocalTommand;
class TommandHandledByTheActionWithDependencyOverload : StrictlyLocal.Tommands.StrictlyLocalTommand;
class TommandHandledByTheAsyncWithDependencyOverload : StrictlyLocal.Tommands.StrictlyLocalTommand;
class TommandHandledByTheCoreUnitOfWorkResolverOverload : StrictlyLocal.Tommands.StrictlyLocalTommand;

class TueryAnsweredByThePlainOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByThePlainOverload, string>;
class TueryAnsweredByTheOneDependencyOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByTheOneDependencyOverload, string>;
class TueryAnsweredByTheTwoDependencyOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByTheTwoDependencyOverload, string>;
class TueryAnsweredByTheThreeDependencyOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByTheThreeDependencyOverload, string>;
class TueryAnsweredByTheFourDependencyOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByTheFourDependencyOverload, string>;
class TueryAnsweredByTheCoreAsyncOverload : StrictlyLocal.Tueries.StrictlyLocalTuery<TueryAnsweredByTheCoreAsyncOverload, string>;
