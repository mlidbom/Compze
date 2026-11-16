using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus._private.TeventDispatch;

///<summary>A tevent published in a transaction, waiting in the <see cref="TransactionTeventQueue"/> for the transaction to prepare:<br/>
/// the tevent, and the remote delivery that its type's delivery contract selected, and that was checked, when it was published.</summary>
sealed class QueuedTevent
{
   readonly Func<Task>? _remoteDelivery;

   ///<param name="wrappedTevent">The published tevent, wrapped as it is routed.</param>
   ///<param name="remoteDelivery">Delivers the tevent to its remote subscribers, or null when it travels by participation alone.</param>
   internal QueuedTevent(IPublisherTevent<ITevent> wrappedTevent, Func<Task>? remoteDelivery)
   {
      WrappedTevent = wrappedTevent;
      _remoteDelivery = remoteDelivery;
   }

   internal IPublisherTevent<ITevent> WrappedTevent { get; }

   ///<summary>Delivers the tevent to its participation handlers, through <paramref name="executor"/> in <paramref name="unitOfWork"/>,<br/>
   /// then to its remote delivery leg.</summary>
   internal async Task DispatchAsync(TessageHandlerExecutor executor, IUnitOfWorkResolver unitOfWork)
   {
      await executor.ExecuteTeventHandlers(WrappedTevent, unitOfWork).caf();
      if(_remoteDelivery != null) await _remoteDelivery().caf();
   }
}
