using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Internals.Transport;

public class InfrastructureQueryExecutor
{
   readonly IServiceLocator _serviceLocator;
   IReadOnlyDictionary<Type, Func<object, object>> _queryHandlers = new Dictionary<Type, Func<object, object>>();
   readonly IMonitor _lock = IMonitor.New();

   InfrastructureQueryExecutor(IServiceLocator serviceLocator) => _serviceLocator = serviceLocator;

   public void RegisterQueryHandler<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult> => _lock.Locked(() =>
      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _queryHandlers, typeof(TQuery), query => handler((TQuery)query)!));

   public object ExecuteQuery(IMessage query)
   {
      this.Log().Debug($"Executing infrastructure query {query.GetType().Name}");
      return _serviceLocator.ExecuteInIsolatedScope(() =>
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
