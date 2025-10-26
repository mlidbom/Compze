using System;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost : TestingEndpointHostBase
{
   public TestingEndpointHost(IComponentRegistrar registrar, IDependencyInjectionContainer rootContainer) : base(registrar, rootContainer.Clone) {}

   public static ITestingEndpointHost Create(IDependencyInjectionContainer rootContainer)
      => new TestingEndpointHost(new TestingComponentRegistrar(), rootContainer);


   public override IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
      => base.RegisterEndpoint(name,
                               id,
                               builder =>
                               {
                                  //Endpoints need a consistent connection string or things go belly up when restarting the host and such so this cannot be delegated to the tests.
                                  builder.Container.Register()
                                         .CurrentTestsPluggableComponents(connectionStringName: id.ToString());

                                  setup(builder);

                               });


   public override IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup) =>
      base.RegisterClientEndpoint(builder =>
      {
         builder.Container.Register()
                .CurrentTestsPluggableComponents();

         setup(builder);
      });

   public override IEndpoint RegisterClientEndpointForRegisteredEndpoints(Action<IEndpointBuilder>? setup = null) => RegisterClientEndpoint(builder =>
   {
      setup?.Invoke(builder);
   });
}