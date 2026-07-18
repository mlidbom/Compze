using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1812 // Never-instantiated test message types exist only to be looked up

namespace Compze.Tests.Integration.Engine;

///<summary>The <see cref="TessageHandlerRoster"/>: the closed set of what one engine understands. Every lookup speaks one<br/>
/// language for a missing handler — <see cref="NoHandlerException"/> naming the tessage type, never a raw dictionary failure<br/>
/// (the two Type-keyed lookups the remote executor calls regressed to raw indexers once: KeyNotFoundException, retried<br/>
/// server-side). Tueries and tommands are single-handler kinds whose second registration explodes at declaration, and<br/>
/// synchrony follows the type: declaring a synchronous handler for an exactly-once kind explodes at declaration.</summary>
public class TessageHandlerRoster_specification : UniversalTestBase
{
   IDependencyInjectionContainer? _container;

   protected TessageHandlerRoster ComposeContainerWithEngineAndGetItsRoster(Action<LocalTessagingEngineBuilder> engine)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.LocalTessagingEngine(engine);
      _container = builder.Build();
      return _container.RootResolver.Resolve<TessageHandlerRoster>();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   public class with_nothing_declared_every_missing_handler_lookup_throws_NoHandlerException_naming_the_type : TessageHandlerRoster_specification
   {
      readonly TessageHandlerRoster _roster;
      public with_nothing_declared_every_missing_handler_lookup_throws_NoHandlerException_naming_the_type() =>
         _roster = ComposeContainerWithEngineAndGetItsRoster(_ => {});

      [PCT] public void the_tuery_handler_lookup() =>
         Invoking(() => _roster.GetTueryHandler(typeof(UnhandledTuery)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledTuery).FullName!);

      [PCT] public void the_tommand_handler_with_result_lookup() =>
         Invoking(() => _roster.GetTommandHandlerWithResult(typeof(UnhandledTommandWithResult)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledTommandWithResult).FullName!);

      [PCT] public void the_void_tommand_handler_lookup() =>
         Invoking(() => _roster.GetVoidTommandHandler(typeof(UnhandledVoidTommand)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledVoidTommand).FullName!);

      ///<summary>Tevents are the multi-subscriber kind: zero matching handlers is a legal fan-out of zero, not a missing handler.</summary>
      [PCT] public void the_tevent_handler_lookup_returns_an_empty_list_rather_than_throwing() =>
         _roster.GetTeventHandlers(typeof(IPublisherTevent<IUnsubscribedTevent>)).Must().BeEmpty();
   }

   public class declaring_a_second_handler_for_a_single_handler_tessage_type_explodes_at_declaration_naming_the_type : TessageHandlerRoster_specification
   {
      [PCT] public void for_a_tuery_type() =>
         Invoking(() => ComposeContainerWithEngineAndGetItsRoster(engine => engine.RegisterTessageHandlers(handle => handle
                          .ForTuery((HandledTuery _) => new Answer())
                          .ForTuery((HandledTuery _) => new Answer()))))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(HandledTuery).FullName!).Contain("single-handler");

      [PCT] public void for_a_typermedia_tommand_type() =>
         Invoking(() => ComposeContainerWithEngineAndGetItsRoster(engine => engine.RegisterTessageHandlers(handle => handle
                          .ForTommand((HandledVoidTommand _) => {})
                          .ForTommand((HandledVoidTommand _) => {}))))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(HandledVoidTommand).FullName!).Contain("single-handler");
   }

   public class declaring_a_synchronous_handler_for_an_exactly_once_kind_explodes_at_declaration : TessageHandlerRoster_specification
   {
      [PCT] public void for_an_exactly_once_tommand_stating_that_exactly_once_kinds_are_async_end_to_end() =>
         Invoking(() => ComposeContainerWithEngineAndGetItsRoster(engine => engine.RegisterTessageHandlers(handle => handle
                          .ForTommand((HandledExactlyOnceTommand _) => {}))))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(HandledExactlyOnceTommand).FullName!).Contain("async end to end");

      [PCT] public void for_a_subscription_demanding_exactly_once_delivery_stating_that_exactly_once_kinds_are_async_end_to_end() =>
         Invoking(() => ComposeContainerWithEngineAndGetItsRoster(engine => engine.RegisterTessageHandlers(handle => handle
                          .ForTevent((IHandledExactlyOnceTevent _) => {}))))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(IHandledExactlyOnceTevent).FullName!).Contain("async end to end");
   }

   protected class Answer;
   class UnhandledTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<Answer>;
   class UnhandledTommandWithResult : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<Answer>;
   class UnhandledVoidTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand;
   protected class HandledTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<Answer>;
   protected class HandledVoidTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand;
   protected class HandledExactlyOnceTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;
   protected interface IHandledExactlyOnceTevent : IExactlyOnceTevent;
   interface IUnsubscribedTevent : ITevent;
}
