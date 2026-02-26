using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

public abstract class TestingEndpointHostBase : EndpointHost, ITestingEndpointHost, IEndpointRegistry
{
   readonly List<Exception> _expectedExceptions = [];

   protected TestingEndpointHostBase(IComponentRegistrar registrar, Func<IDependencyInjectionContainer> containerFactory) : base(registrar, containerFactory) =>
      TessagesInFlightTracker = new TessagesInFlightTracker(TypeMapper.Instance);

   public IEnumerable<EndPointAddress> ServerEndpointAddresses => Endpoints.Where(it => it.Address is not null)
                                                                           .Select(it => it.Address!)
                                                                           .ToList();

   void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) => TessagesInFlightTracker.AwaitNoTessagesInFlight(timeoutOverride);

   public TException AssertThrown<TException>() where TException : Exception
   {
      WaitForEndpointsToBeAtRest();
      var matchingException = GetThrownExceptions().OfType<TException>().SingleOrDefault();
      if(matchingException == null)
      {
         throw new Exception("Matching exception not thrown.");
      }

      _expectedExceptions.Add(matchingException);
      return matchingException;
   }

   bool _disposed;

   protected override async ValueTask DisposeAsync(bool disposing) => await DisposeAsync(disposing, true).caf();

   async ValueTask DisposeAsync(bool disposing, bool waitForEndpointsToBeAtRest)
   {
      if(!_disposed)
      {
         _disposed = true;
         List<Exception> unHandledExceptions = new();
         if(waitForEndpointsToBeAtRest)
         {
            try
            {
               WaitForEndpointsToBeAtRest(timeoutOverride: 10.Seconds());
            }
            catch(Exception e)
            {
               unHandledExceptions.Add(e);
            }

            unHandledExceptions.AddRange(GetThrownExceptions().Except(_expectedExceptions).ToList());

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
   }

   public bool WaitForEndPointsToBeAtRestOnDispose { get; set; } = true;
   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, false).caf();

   List<Exception> GetThrownExceptions() => TessagesInFlightTracker.GetExceptions().ToList();
}
