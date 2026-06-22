using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>The Tessaging pipeline's runtime lifecycle within an endpoint: inbox and scheduler listen, the router connects to all endpoints, the outbox sends.</summary>
sealed class TessagingEndpointComponent : IEndpointComponent, IAsyncDisposable
{
   readonly TommandScheduler _tommandScheduler;
   readonly IInbox _inbox;
   readonly IOutbox _outbox;
   readonly ITessagingRouter _tessagingRouter;
   readonly IEndpointRegistry _endpointRegistry;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;

   bool _isListening;

   internal TessagingEndpointComponent(IRootResolver resolver)
   {
      _tommandScheduler = resolver.Resolve<TommandScheduler>();
      _inbox = resolver.Resolve<IInbox>();
      _outbox = resolver.Resolve<IOutbox>();
      _tessagingRouter = resolver.Resolve<ITessagingRouter>();
      _endpointRegistry = resolver.Resolve<IEndpointRegistry>();
      _backgroundExceptionReporter = resolver.Resolve<IBackgroundExceptionReporter>();
   }

   ///<summary>The address where the inbox listens; null until listening.</summary>
   internal EndpointAddress? Address => _isListening ? _inbox.Address : null;

   public async Task StartListeningAsync()
   {
      await Task.WhenAll(_inbox.StartAsync(), _tommandScheduler.StartAsync()).caf();
      _isListening = true;
   }

   public async Task StartSendingAsync()
   {
      //Tessaging connects to all endpoints including ourselves. Scheduled tommands need to dispatch over the remote protocol to get the delivery guarantees...
      var serverAddresses = _endpointRegistry.ServerEndpointAddresses.ToHashSet();
      serverAddresses.Add(_inbox.Address);
      await Task.WhenAll(serverAddresses.Select(address => _tessagingRouter.ConnectAsync(address))).caf();
      await _outbox.StartAsync().caf();
   }

   public async Task StopSendingAsync()
   {
      _tommandScheduler.Stop();
      await _outbox.StopAsync().caf();
   }

   public async Task StopListeningAsync()
   {
      _isListening = false;
      await _inbox.StopAsync().caf();
      _tessagingRouter.Stop();
   }

   public ValueTask DisposeAsync()
   {
      _tommandScheduler.Dispose();
      _backgroundExceptionReporter.ThrowIfAnyExceptions();
      return ValueTask.CompletedTask;
   }
}
