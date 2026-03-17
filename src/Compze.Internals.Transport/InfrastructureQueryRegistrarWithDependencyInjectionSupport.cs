using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Transport;

public class InfrastructureQueryRegistrarWithDependencyInjectionSupport(InfrastructureQueryExecutor executor)
{
   readonly InfrastructureQueryExecutor _executor = executor;

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(
      Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, _) => handler(query));
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TResult>(
      Func<TQuery, TDependency1, TResult> handler) where TQuery : IQuery<TResult>
                                                   where TDependency1 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>()));
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TResult>(
      Func<TQuery, TDependency1, TDependency2, TResult> handler) where TQuery : IQuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>(), scopeResolver.Resolve<TDependency2>()));
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(
      Func<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TQuery : IQuery<TResult>
                                                                                              where TDependency1 : class
                                                                                              where TDependency2 : class
                                                                                              where TDependency3 : class
                                                                                              where TDependency4 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>(), scopeResolver.Resolve<TDependency2>(), scopeResolver.Resolve<TDependency3>(), scopeResolver.Resolve<TDependency4>()));
      return this;
   }
}
