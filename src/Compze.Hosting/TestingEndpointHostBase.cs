using Compze.TypeIdentifiers;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Hosting;

public abstract class TestingEndpointHostBase : EndpointHost, ITestingEndpointHost, IEndpointRegistry
{
   protected TestingEndpointHostBase(Func<IContainerBuilder> containerFactory) : base(containerFactory)
   {
      var mapper = new TypeIdentifierMapper();
      mapper.MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute();
      TessagesInFlightTracker = new TessagesInFlightTracker(mapper);
   }

   public IEnumerable<EndPointAddress> ServerEndpointAddresses => Endpoints.Where(it => it.Address is not null)
                                                                           .Select(it => it.Address!)
                                                                           .ToList();

   void WaitForEndpointsToBeAtRest(WaitTimeout? timeoutOverride = null) => TessagesInFlightTracker.AwaitNoTessagesInFlight(timeoutOverride);

   bool _disposed;

   protected override async ValueTask DisposeAsync(bool disposing) => await DisposeAsync(disposing, true).caf();

   async ValueTask DisposeAsync(bool disposing, bool waitForEndpointsToBeAtRest)
   {
      if(!_disposed)
      {
         _disposed = true;
         List<Exception> unHandledExceptions = [];
         if(waitForEndpointsToBeAtRest)
         {
            try
            {
               WaitForEndpointsToBeAtRest(WaitTimeout.Seconds(10));
            }
#pragma warning disable CA1031
            catch(Exception e)
            {
               unHandledExceptions.Add(e);
            }
#pragma warning restore CA1031
         }

         unHandledExceptions.AddRange(TessagesInFlightTracker.GetExceptions().ToList());

         try
         {
            await base.DisposeAsync(disposing).caf();
         }
         catch(AggregateException taggregateException)
         {
            unHandledExceptions.AddRange(taggregateException.Flatten().InnerExceptions);
         }

         if(unHandledExceptions.Any())
         {
            throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions);
         }
      }
   }

   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, false).caf();
}
