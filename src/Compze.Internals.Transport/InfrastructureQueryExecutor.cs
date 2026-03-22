using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading;

namespace Compze.Internals.Transport;

public class InfrastructureQueryExecutor
{
   readonly IScopeFactory _scopeFactory;
   IReadOnlyDictionary<Type, Func<object, IScopeResolver, object>> _queryHandlers = new Dictionary<Type, Func<object, IScopeResolver, object>>();
   readonly IMonitor _monitor = IMonitor.New();

   InfrastructureQueryExecutor(IScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

   public void RegisterQueryHandler<TQuery, TResult>(Func<TQuery, IScopeResolver, TResult> handler) where TQuery : IQuery<TResult> => _monitor.Locked(() =>
   {
      object Value(object query, IScopeResolver scopeResolver) => handler((TQuery)query, scopeResolver)!;
      Interlocked.Exchange(ref _queryHandlers, _queryHandlers.AddToCopy(typeof(TQuery), Value));
   });

   public object ExecuteQuery(IMessage query)
   {
      this.Log().Debug($"Executing infrastructure query {query.GetType().Name}");
      return _scopeFactory.ExecuteInIsolatedScope(scopeResolver =>
      {
         if(!_queryHandlers.TryGetValue(query.GetType(), out var handler))
            throw new InvalidOperationException($"No infrastructure query handler registered for {query.GetType().FullName}");

         return handler(query, scopeResolver);
      });
   }

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<InfrastructureQueryExecutor>()
                  .CreatedBy((IScopeFactory scopeFactory)
                                => new InfrastructureQueryExecutor(scopeFactory)));
}
