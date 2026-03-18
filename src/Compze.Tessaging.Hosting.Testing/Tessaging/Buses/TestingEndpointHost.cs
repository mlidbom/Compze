using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Hosting;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Underscore;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost : TestingEndpointHostBase
{
   IContainerBuilder? _ownedBuilder = null;

   TestingEndpointHost(IContainerBuilder rootBuilder) : base(rootBuilder.Clone) {}

   public static ITestingEndpointHost Create(IContainerBuilder? rootBuilder = null)
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
      var usedBuilder = rootBuilder ?? TestEnv.DIContainer.CreateWithServiceLocator()
                                                  ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer());
#pragma warning restore CA2000 // We are passing this disposable into a constructor of an object we don't own

      var host = new TestingEndpointHost(usedBuilder);

      if(rootBuilder == null)
      {
         host._ownedBuilder = usedBuilder;
      }

      return host;
   }

#pragma warning disable CA1031 // We want to catch all exceptions and throw an aggregate if there are multiple
   protected override async ValueTask DisposeAsync(bool disposing)
   {
      List<Exception> exceptions = [];
      try
      {
         await base.DisposeAsync(disposing).caf();
      }
      catch(Exception e)
      {
         exceptions.Add(e);
      }

      if(_ownedBuilder != null)
      {
         try
         {
            await _ownedBuilder.DisposeAsync();
         }
         catch(Exception e)
         {
            exceptions.Add(e);
         }
      }

      if(exceptions.Count > 0)
      {
         throw new AggregateException(exceptions);
      }
   }
#pragma warning restore CA1031

   public override IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
      => base.RegisterEndpoint(name,
                               id,
                               builder =>
                               {
                                  //Endpoints need a consistent connection string or things go belly up when creating a new host with a new container.
                                  builder.Registrar
                                         .CurrentTestsPluggableComponents(connectionStringName: id.ToString());

                                  setup(builder);
                               });
}
