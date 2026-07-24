using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

///<summary>Specifies how a <see cref="NavigationSpecification"/> composes navigation steps: each step receives the previous
/// step's result, and the whole chain executes against whichever navigator it is run on.</summary>
public class NavigationSpecification_specification : UniversalTestBase
{
   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatingEndpoint;
   readonly ConcurrentQueue<int> _numbersRemembered = new();
   IRemoteTypermediaNavigator _navigator = null!;

   public NavigationSpecification_specification()
   {
      _host = TestingEndpointHost.Create();
      _navigatingEndpoint = _host.RegisterEndpoint(new NavigatingEndpointDeclaration());
      _host.RegisterEndpoint(new AnsweringEndpointDeclaration(this));
   }

   ///<summary>Runs the chains. It declares no handlers of its own: a navigation goes to whichever connected endpoint advertises
   /// the type, so the specification's steps only mean what they say when the answering side is genuinely somewhere else.</summary>
   class NavigatingEndpointDeclaration : BestEffortEndpointDeclaration<NavigatingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NavigationSpecificationNavigating";
      public static EndpointId Id => new(Guid.Parse("5D2A9F63-84C1-4E70-B3A8-16F027C4E9B5"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTypermediaHostingSpecificationTypeMappings();
   }

   ///<summary>Answers the tueries the chains navigate and remembers the tommands posted to it — the far side of every
   /// navigation here, reached the way any navigation reaches its handler.</summary>
   class AnsweringEndpointDeclaration : BestEffortEndpointDeclaration<AnsweringEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NavigationSpecificationAnswering";
      public static EndpointId Id => new(Guid.Parse("1E7C3B48-06A9-42D5-9F31-8B0C5D6E27A4"));

      readonly NavigationSpecification_specification _specification;
      internal AnsweringEndpointDeclaration(NavigationSpecification_specification specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTypermediaHostingSpecificationTypeMappings();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((TheAnswerTuery _) => new NumberResource { Value = 42 })
         .ForTuery((AddTuery tuery) => new NumberResource { Value = tuery.Left + tuery.Right });

      protected override void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) => handle
         .ForTommand((RememberNumberTommand tommand) =>
          {
             _specification._numbersRemembered.Enqueue(tommand.Number);
             return new NumberResource { Value = tommand.Number };
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      await _host.AwaitEndpointsHaveMetEachOtherAsync().caf();
      _navigator = _navigatingEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void Get_returns_the_navigators_answer_to_the_tuery() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .NavigateOn(_navigator)
                             .Value.Must().Be(42);

   [PCT] public void Select_transforms_the_previous_steps_result() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Select(answer => answer.Value * 2)
                             .NavigateOn(_navigator)
                             .Must().Be(84);

   [PCT] public void a_chained_Get_builds_the_next_tuery_from_the_previous_steps_result() =>
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Get(answer => new AddTuery { Left = answer.Value, Right = 8 })
                             .NavigateOn(_navigator)
                             .Value.Must().Be(50);

   [PCT] public void a_chained_Post_posts_the_tommand_built_from_the_previous_steps_result()
   {
      NavigationSpecification.Get(new TheAnswerTuery())
                             .Post(answer => RememberNumberTommand.Create(answer.Value))
                             .NavigateOn(_navigator);

      _numbersRemembered.Single().Must().Be(42);
   }
}
