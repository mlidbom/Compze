using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;

namespace Compze.Tessaging._private.Transport.Advertisement;

class EndpointInformationQueryExecutor
{
   readonly IScopeFactory _scopeFactory;
   IReadOnlyDictionary<Type, Func<object, IScopeResolver, object>> _queryHandlers = new Dictionary<Type, Func<object, IScopeResolver, object>>();
   readonly IMonitor _monitor = IMonitor.New();

   EndpointInformationQueryExecutor(IScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

   internal void RegisterQueryHandler<TQuery, TResult>(Func<TQuery, IScopeResolver, TResult> handler) where TQuery : ITuery<TResult> => _monitor.Locked(() =>
   {
      object Value(object query, IScopeResolver scopeResolver) => handler((TQuery)query, scopeResolver)!;
      Interlocked.Exchange(ref _queryHandlers, _queryHandlers.AddToCopy(typeof(TQuery), Value));
   });

   internal object ExecuteQuery(ITuery query)
   {
      this.Log().Debug($"Executing endpoint-discovery query {query.GetType().Name}");
      return _scopeFactory.ExecuteInIsolatedScope(scopeResolver =>
      {
         if(!_queryHandlers.TryGetValue(query.GetType(), out var handler))
            throw new InvalidOperationException($"No endpoint-discovery query handler registered for {query.GetType().FullName}");

         return handler(query, scopeResolver);
      });
   }

   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
                  Singleton.For<EndpointInformationQueryExecutor>()
                           .CreatedBy((IScopeFactory scopeFactory)
                                         => new EndpointInformationQueryExecutor(scopeFactory)));
}
