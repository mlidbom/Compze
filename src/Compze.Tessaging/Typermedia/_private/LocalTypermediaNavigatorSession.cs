using Compze.Tessaging.Validation._internal;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia._private;

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
      //todo:urgent: a synchronous wait on an asynchronous handler: it holds the calling thread, and stalls while every
      //   thread-pool thread is busy. See dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // The navigator session's synchronous API, over the executor's asynchronous handlers.
      return _executor.ExecuteTommandHandlerAsync(tommand, UnitOfWorkResolver.From(_scopeResolver)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      //todo:urgent: a synchronous wait on an asynchronous handler: it holds the calling thread, and stalls while every
      //   thread-pool thread is busy. See dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // The navigator session's synchronous API, over the executor's asynchronous handlers.
      _executor.ExecuteTommandHandlerAsync(tommand, UnitOfWorkResolver.From(_scopeResolver), CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      //todo:urgent: a synchronous wait on an asynchronous handler: it holds the calling thread, and stalls while every
      //   thread-pool thread is busy. See dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // The navigator session's synchronous API, over the executor's asynchronous handlers.
      return _executor.ExecuteTueryHandlerAsync(tuery, _scopeResolver).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
   }

   public async Task<TResult> ExecuteAsync<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);
      return await _executor.ExecuteTommandHandlerAsync(tommand, UnitOfWorkResolver.From(_scopeResolver)).caf();
   }

   public async Task ExecuteAsync(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);
      await _executor.ExecuteTommandHandlerAsync(tommand, UnitOfWorkResolver.From(_scopeResolver), CancellationToken.None).caf();
   }

   public async Task<TResult> ExecuteAsync<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      return await _executor.ExecuteTueryHandlerAsync(tuery, _scopeResolver).caf();
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TessageValidator.AssertValidToExecuteLocally(tessage);
   }
}
