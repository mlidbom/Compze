using Compze.DependencyInjection;

namespace Compze.Tessaging.Transport.Discovery;

public class EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(EndpointDiscoveryQueryExecutor executor)
{
   readonly EndpointDiscoveryQueryExecutor _executor = executor;

   public EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(
      Func<TQuery, TResult> handler) where TQuery : ITuery<TResult>
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, _) => handler(query));
      return this;
   }

   public EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TResult>(
      Func<TQuery, TDependency1, TResult> handler) where TQuery : Remotable.NonTransactional.Tueries.Tuery<TResult>
                                                   where TDependency1 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>()));
      return this;
   }

   internal EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TResult>(
      Func<TQuery, TDependency1, TDependency2, TResult> handler) where TQuery : ITuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>(), scopeResolver.Resolve<TDependency2>()));
      return this;
   }

   public EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(
      Func<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TQuery : ITuery<TResult>
                                                                                              where TDependency1 : class
                                                                                              where TDependency2 : class
                                                                                              where TDependency3 : class
                                                                                              where TDependency4 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>(), scopeResolver.Resolve<TDependency2>(), scopeResolver.Resolve<TDependency3>(), scopeResolver.Resolve<TDependency4>()));
      return this;
   }
}
