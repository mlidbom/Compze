using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;

namespace Compze.Tessaging.Implementation.Transport.Infrastructure;

class InfrastructureQueryRegistrarWithDependencyInjectionSupport(InfrastructureQueryExecutor executor, LazyCE<IServiceLocator> serviceLocator)
{
   readonly InfrastructureQueryExecutor _executor = executor;
   readonly LazyCE<IServiceLocator> _serviceLocator = serviceLocator;

   TService Resolve<TService>() where TService : class => _serviceLocator.Value.Resolve<TService>();

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TResult>(
      Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>
   {
      _executor.RegisterQueryHandler(handler);
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TResult>(
      Func<TQuery, TDependency1, TResult> handler) where TQuery : IQuery<TResult>
                                                   where TDependency1 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>(query => handler(query, Resolve<TDependency1>()));
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TResult>(
      Func<TQuery, TDependency1, TDependency2, TResult> handler) where TQuery : IQuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>(query => handler(query, Resolve<TDependency1>(), Resolve<TDependency2>()));
      return this;
   }

   public InfrastructureQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(
      Func<TQuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TQuery : IQuery<TResult>
                                                                                              where TDependency1 : class
                                                                                              where TDependency2 : class
                                                                                              where TDependency3 : class
                                                                                              where TDependency4 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>(query => handler(query, Resolve<TDependency1>(), Resolve<TDependency2>(), Resolve<TDependency3>(), Resolve<TDependency4>()));
      return this;
   }
}
