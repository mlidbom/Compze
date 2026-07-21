using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging._private.Transport.Advertisement;

class EndpointInformationQueryRegistrarWithDependencyInjectionSupport(EndpointInformationQueryExecutor executor)
{
   readonly EndpointInformationQueryExecutor _executor = executor;

   public EndpointInformationQueryRegistrarWithDependencyInjectionSupport ForQuery<TQuery, TDependency1, TDependency2, TResult>(
      Func<TQuery, TDependency1, TDependency2, TResult> handler) where TQuery : ITuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      _executor.RegisterQueryHandler<TQuery, TResult>((query, scopeResolver) => handler(query, scopeResolver.Resolve<TDependency1>(), scopeResolver.Resolve<TDependency2>()));
      return this;
   }
}
