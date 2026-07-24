using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;

namespace Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public abstract partial class EndpointHostTestBase
{
   ///<summary>The successor to the Remote endpoint — deliberately a NEW identity: a blue/green replacement is a different<br/>
   /// endpoint that advertises the same tommand type, never the old identity reused.</summary>
   protected class RemoteSuccessorEndpointDeclaration : ExactlyOnceEndpointDeclaration<RemoteSuccessorEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "RemoteSuccessor";
      public static EndpointId Id => new(Guid.Parse("46ECC3A4-5657-4A0A-9C78-9FEEA5A1010D"));

      readonly EndpointHostTestBase _fixture;
      internal RemoteSuccessorEndpointDeclaration(EndpointHostTestBase fixture) => _fixture = fixture;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireCommonTestTypeMappings();

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((MyExactlyOnceTommandHandledByTheRemoteEndpoint _) =>
          {
             _fixture.RemoteSuccessorTommandHandlerThreadGate.AwaitPassThrough();
             return Task.CompletedTask;
          });
   }
}
