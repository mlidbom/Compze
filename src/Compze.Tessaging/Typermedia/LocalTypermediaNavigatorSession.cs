using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.Tessaging.Engine;

namespace Compze.Tessaging.Typermedia;

class LocalTypermediaNavigatorSession : ILocalTypermediaNavigatorSession
{
   readonly TessageHandlerExecutor _executor;
   readonly IScopeResolver _scopeResolver;
   readonly IUsageGuard _contextGuard;

   internal LocalTypermediaNavigatorSession(TessageHandlerExecutor executor, IScopeResolver scopeResolver)
   {
      _executor = executor;
      _scopeResolver = scopeResolver;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      //CommonAssertion has asserted the caller's ambient transaction (an IStrictlyLocalTommand must be sent transactionally), so the caller's scope IS a unit of work - From certifies exactly that.
      return _executor.ExecuteTommandHandler(tommand, UnitOfWorkResolver.From(_scopeResolver)).GetAwaiter().GetResult();
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      _executor.ExecuteTommandHandler(tommand, UnitOfWorkResolver.From(_scopeResolver)).GetAwaiter().GetResult();
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      return _executor.ExecuteTueryHandler(tuery, _scopeResolver).GetAwaiter().GetResult();
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TessageValidator.AssertValidToExecuteLocally(tessage);
   }
}
