using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Contracts;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.Outbox;

static class OutboxRegistrar
{
   public static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Outbox.Outbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
public partial class Outbox : IOutbox
{
   internal static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITessagingRouter tessagingRouter, ITessageStorage tessageStorage)
                                                => new Outbox(tessagingRouter, tessageStorage, configuration)));
      registrar.Register(TessageStorage.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITessagingRouter _tessagingRouter;

   Outbox(ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, EndpointConfiguration configuration)
   {
      _storage = tessageStorage;
      _configuration = configuration;
      _tessagingRouter = tessagingRouter;
   }

   public void PublishTransactionally(IExactlyOnceTevent exactlyOnceTevent)
   {
      Contract.State.NotNull(Transaction.Current);
      this.Log().Debug($"Publishing tevent {exactlyOnceTevent.Id} ({exactlyOnceTevent.GetType().Name})");
      var connections = _tessagingRouter.SubscriberConnectionsFor(exactlyOnceTevent)
                                        .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                        .ToArray(); //We dispatch tevents to ourselves synchronously so don't go doing it again here.

      var teventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
      _storage.SaveTessage(exactlyOnceTevent, teventHandlerEndpointIds);

      Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(connection =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tevent {exactlyOnceTevent.Id} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForDelivery(exactlyOnceTevent);
      }));
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      Contract.State.NotNull(Transaction.Current);
      this.Log().Debug($"Sending tommand {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");
      var connection = _tessagingRouter.ConnectionToHandlerFor(exactlyOnceTommand);

      _storage.SaveTessage(exactlyOnceTommand, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tommand {exactlyOnceTommand.Id} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForDelivery(exactlyOnceTommand);
      });
   }

   bool _running = false;

   public async Task StartAsync()
   {
      Contract.State.Assert(!_running);
      this.Log().Info("Starting");

      await _storage.StartAsync().caf();

      _tessagingRouter.StartDelivery();

      _running = true;
      this.Log().Info("Started");
   }

   public async Task StopAsync()
   {
      Contract.State.Assert(_running);
      this.Log().Info("Stopping");
      _running = false;

      _tessagingRouter.StopDelivery();

      this.Log().Info("Stopped");
      await Task.CompletedTask.caf();
   }
}
