using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Typermedia.Client;
using Compze.Underscore;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost : EndpointHost, ITestingEndpointHost, IEndpointRegistry
{
   readonly TessagesInFlightTracker _tessagesInFlightTracker = new();
   readonly IDependencyInjectionContainer _rootContainer;
   readonly bool _ownsRootContainer;

   TestingEndpointHost(IDependencyInjectionContainer rootContainer, bool ownsRootContainer) : base(rootContainer.CreateCloneContainerBuilder)
   {
      _rootContainer = rootContainer;
      _ownsRootContainer = ownsRootContainer;
   }

   public static ITestingEndpointHost Create(IContainerBuilder? rootBuilder = null)
   {
      var usedBuilder = rootBuilder ?? TestEnv.DIContainer.CreateWithContainerRegistrations()
                                                  ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer());

      var rootContainer = usedBuilder.Build();
      return new TestingEndpointHost(rootContainer, ownsRootContainer: true);
   }

   public static ITestingEndpointHost Create(IDependencyInjectionContainer rootContainer) =>
      new TestingEndpointHost(rootContainer, ownsRootContainer: false);

   public IEnumerable<EndpointAddress> ServerEndpointAddresses => Endpoints.Where(it => it.TessagingAddress is not null)
                                                                           .Select(it => it.TessagingAddress!)
                                                                           .ToList();

   public override IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
      => base.RegisterEndpoint(name,
                               id,
                               builder =>
                               {
                                  //Endpoints need a consistent connection string or things go belly up when creating a new host with a new container.
                                  builder.Registrar
                                         .Register(Singleton.For<ITessagesInFlightTracker>().Instance(_tessagesInFlightTracker))
                                         .CurrentTestsPluggableComponents(connectionStringName: id.ToString());

                                  //Test endpoints get both paradigms' pipelines, mirroring what production endpoints have used so far.
                                  builder.AddTessaging();
                                  builder.AddTypermedia();

                                  setup(builder);
                               });

   bool _disposed;

#pragma warning disable CA1031 // We want to catch all exceptions and throw an aggregate if there are multiple
   protected override async ValueTask DisposeAsync(bool disposing) => await DisposeAsync(disposing, waitForEndpointsToBeAtRest: true).caf();

   async ValueTask DisposeAsync(bool disposing, bool waitForEndpointsToBeAtRest)
   {
      if(_disposed) return;
      _disposed = true;

      List<Exception> unHandledExceptions = [];
      if(waitForEndpointsToBeAtRest)
      {
         try
         {
            _tessagesInFlightTracker.AwaitNoTessagesInFlight(WaitTimeout.Seconds(10));
         }
         catch(Exception e)
         {
            unHandledExceptions.Add(e);
         }
      }

      unHandledExceptions.AddRange(_tessagesInFlightTracker.GetExceptions());

      try
      {
         await base.DisposeAsync(disposing).caf();
      }
      catch(AggregateException aggregateException)
      {
         unHandledExceptions.AddRange(aggregateException.Flatten().InnerExceptions);
      }
      catch(Exception e)
      {
         unHandledExceptions.Add(e);
      }

      if(_ownsRootContainer)
      {
         try
         {
            await _rootContainer.DisposeAsync().caf();
         }
         catch(Exception e)
         {
            unHandledExceptions.Add(e);
         }
      }

      if(unHandledExceptions.Any())
      {
         throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions);
      }
   }
#pragma warning restore CA1031

   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, waitForEndpointsToBeAtRest: false).caf();
}
