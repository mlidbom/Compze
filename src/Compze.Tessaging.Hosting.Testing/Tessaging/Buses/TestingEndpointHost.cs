using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost : TestingEndpointHostBase
{
   IDependencyInjectionContainer? _ownedContainer = null;
   public TestingEndpointHost(IComponentRegistrar registrar, IDependencyInjectionContainer rootContainer) : base(registrar, rootContainer.Clone)
   {

   }

   public static ITestingEndpointHost Create(IDependencyInjectionContainer? rootContainer = null)
   {
#pragma warning disable CA2000// We are passing this disposable into a constructor of an object we don't own
      var usedContainer = rootContainer ?? TestEnv.DIContainer.CreateWithServiceLocator()
                                                  ._mutate(it => it.Register().CurrentTestsDbPoolIfNotCloneContainer());
#pragma warning restore CA2000// We are passing this disposable into a constructor of an object we don't own


      var host = new TestingEndpointHost(new TestingComponentRegistrar(), usedContainer);

      if(rootContainer == null)
      {
         host._ownedContainer = usedContainer;
      }

      return host;
   }

   protected override async ValueTask DisposeAsync(bool disposing)
   {
      await base.DisposeAsync(disposing).caf();
      if(_ownedContainer != null)
      {
         await _ownedContainer.DisposeAsync();
      }
   }

   public override IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
      => base.RegisterEndpoint(name,
                               id,
                               builder =>
                               {
                                  //Endpoints need a consistent connection string or things go belly up when creating a new host with a new container.
                                  builder.Container.Register()
                                         .CurrentTestsPluggableComponents(connectionStringName: id.ToString());

                                  setup(builder);

                               });


   public override IClient RegisterClient(Action<IEndpointBuilder>? setup = null) =>
      base.RegisterClient(builder =>
      {
         builder.Container.Register()
                .CurrentTestsPluggableComponents();

         setup?.Invoke(builder);
      });

   public override IClient RegisterClientForRegisteredEndpoints(Action<IEndpointBuilder>? setup = null) => RegisterClient(builder =>
   {
      setup?.Invoke(builder);
   });
}