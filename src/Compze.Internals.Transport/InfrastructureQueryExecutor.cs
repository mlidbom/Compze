using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading;

namespace Compze.Internals.Transport;

public class InfrastructureQueryExecutor
{
   readonly IServiceLocator _serviceLocator;
   IReadOnlyDictionary<Type, Func<object, object>> _queryHandlers = new Dictionary<Type, Func<object, object>>();
   readonly IMonitor _monitor = IMonitor.New();

   InfrastructureQueryExecutor(IServiceLocator serviceLocator) => _serviceLocator = serviceLocator;

   public void RegisterQueryHandler<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult> => _monitor.Locked(() =>
   {
      object Value(object query) => handler((TQuery)query)!;
      Interlocked.Exchange(ref _queryHandlers, _queryHandlers.AddToCopy(typeof(TQuery), Value));
   });

   public object ExecuteQuery(IMessage query)
   {
      this.Log().Debug($"Executing infrastructure query {query.GetType().Name}");
      return _serviceLocator.ExecuteInIsolatedScope(scope =>
      {
         if(!_queryHandlers.TryGetValue(query.GetType(), out var handler))
            throw new InvalidOperationException($"No infrastructure query handler registered for {query.GetType().FullName}");

         return handler(query);
      });
   }

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<InfrastructureQueryExecutor>()
                  .CreatedBy((IServiceLocator serviceLocator)
                                => new InfrastructureQueryExecutor(serviceLocator)));
}
