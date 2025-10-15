using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting;

public class TestingEndpointHostBase : EndpointHost, ITestingEndpointHost, IEndpointRegistry
{
   readonly List<Exception> _expectedExceptions = [];
   public TestingEndpointHostBase(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : base(mode, containerFactory) => MessagesInFlightTracker = new MessagesInFlightTracker();

   public IEnumerable<EndPointAddress> ServerEndpoints => Endpoints.Where(it => it.Address is not null)
                                                                   .Select(it => it.Address!)
                                                                   .ToList();

   void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) => Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride));

   public IEndpoint RegisterTestingEndpoint(string? name = null, EndpointId? id = null, Action<IEndpointBuilder>? setup = null)
   {
      var endpointId = id ?? new EndpointId(Guid.NewGuid());
      name ??= $"TestingEndpoint-{endpointId.GuidValue}";
      setup ??= _ => {};
      return RegisterEndpoint(name, endpointId, setup);
   }

   public IEndpoint RegisterClientEndpointForRegisteredEndpoints() =>
      RegisterClientEndpoint(_ => {});

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
         if(waitForEndpointsToBeAtRest)
         {
            WaitForEndpointsToBeAtRest(timeoutOverride: 5.Seconds());
         }

         var unHandledExceptions = GetThrownExceptions().Except(_expectedExceptions).ToList();

         try
         {
            await base.DisposeAsync(disposing).caf();
         }
         catch(AggregateException aggregateException)
         {
            unHandledExceptions.AddRange(aggregateException.Flatten().InnerExceptions);
         }

         if(unHandledExceptions.Any())
         {
            throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions);
         }
      }
   }

   public bool WaitForEndPointsToBeAtRestOnDispose { get; set; } = true;
   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, false).caf();

   List<Exception> GetThrownExceptions() => MessagesInFlightTracker.GetExceptions().ToList();
}
