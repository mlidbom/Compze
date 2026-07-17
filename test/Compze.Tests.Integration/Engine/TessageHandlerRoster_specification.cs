using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging;
using Compze.Tessaging.Engine;
using Compze.Tessaging.TessageHandling.Registration.Public;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.HandlerRegistration;
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
/// server-side). Tueries and tommands are single-handler kinds whose second registration explodes at declaration, and the<br/>
/// roster is immutable once built: registration after the engine is built explodes.</summary>
public class TessageHandlerRoster_specification : UniversalTestBase
{
   protected IDependencyInjectionContainer Container { get; }
   protected ITypermediaHandlerRegistrar TypermediaHandlerRegistrar { get; }
   protected TessageHandlerRoster Roster => Container.RootResolver.Resolve<TessageHandlerRoster>();

   public TessageHandlerRoster_specification()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.InProcessTessaging()
             .InProcessTypermedia();
      Container = builder.Build();
      TypermediaHandlerRegistrar = Container.RootResolver.Resolve<ITypermediaHandlerRegistrar>();
   }

   protected override async Task DisposeAsyncInternal() => await Container.DisposeAsync();

   public class with_nothing_registered_every_missing_handler_lookup_throws_NoHandlerException_naming_the_type : TessageHandlerRoster_specification
   {
      [PCT] public void the_tuery_handler_lookup() =>
         Invoking(() => Roster.GetTueryHandler(typeof(UnhandledTuery)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledTuery).FullName!);

      [PCT] public void the_tommand_handler_with_result_lookup() =>
         Invoking(() => Roster.GetTommandHandlerWithResult(typeof(UnhandledTommandWithResult)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledTommandWithResult).FullName!);

      [PCT] public void the_void_tommand_handler_lookup() =>
         Invoking(() => Roster.GetVoidTommandHandler(typeof(UnhandledVoidTommand)))
            .Must().Throw<NoHandlerException>()
            .Which.Message.Must().Contain(typeof(UnhandledVoidTommand).FullName!);

      ///<summary>Tevents are the multi-subscriber kind: zero matching handlers is a legal fan-out of zero, not a missing handler.</summary>
      [PCT] public void the_tevent_handler_lookup_returns_an_empty_list_rather_than_throwing() =>
         Roster.GetTeventHandlers(typeof(IPublisherTevent<IUnsubscribedTevent>)).Must().BeEmpty();
   }

   public class registering_a_second_handler_for_a_single_handler_tessage_type_explodes_at_declaration_naming_the_type : TessageHandlerRoster_specification
   {
      [PCT] public void for_a_tuery_type()
      {
         TypermediaHandlerRegistrar.ForTuery((HandledTuery _, IScopeResolver _) => new Answer());
         Invoking(() => TypermediaHandlerRegistrar.ForTuery((HandledTuery _, IScopeResolver _) => new Answer()))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(HandledTuery).FullName!).Contain("single-handler");
      }

      [PCT] public void for_a_typermedia_tommand_type()
      {
         TypermediaHandlerRegistrar.ForTommand((HandledVoidTommand _, IUnitOfWorkResolver _) => {});
         Invoking(() => TypermediaHandlerRegistrar.ForTommand((HandledVoidTommand _, IUnitOfWorkResolver _) => {}))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain(typeof(HandledVoidTommand).FullName!).Contain("single-handler");
      }
   }

   public class after_the_engine_is_built_the_roster_is_closed : TessageHandlerRoster_specification
   {
      public after_the_engine_is_built_the_roster_is_closed()
      {
         TypermediaHandlerRegistrar.ForTuery((HandledTuery _, IScopeResolver _) => new Answer());
         Roster.GetTueryHandler(typeof(HandledTuery)); //Resolving the roster builds it; from here, registration is closed.
      }

      [PCT] public void registering_another_handler_explodes_stating_that_registration_closed_when_the_engine_was_built() =>
         Invoking(() => TypermediaHandlerRegistrar.ForTommand((HandledVoidTommand _, IUnitOfWorkResolver _) => {}))
            .Must().Throw<Exception>()
            .Which.Message.Must().Contain("registration closed when the engine was built");
   }

   protected class Answer;
   class UnhandledTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<Answer>;
   class UnhandledTommandWithResult : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<Answer>;
   class UnhandledVoidTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand;
   protected class HandledTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<Answer>;
   protected class HandledVoidTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand;
   interface IUnsubscribedTevent : ITevent;
}
