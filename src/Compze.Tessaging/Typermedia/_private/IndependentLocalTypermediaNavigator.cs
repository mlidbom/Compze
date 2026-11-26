using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.Typermedia._private;

static class IndependentLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar IndependentLocalTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(_private.IndependentLocalTypermediaNavigator.RegisterWith);
}

///<summary>The <see cref="IIndependentLocalTypermediaNavigator"/>: runs each tommand as its own unit of work<br/>
/// (<c>ExecuteUnitOfWork</c>) and each tuery in its own fresh transactionless scope (<c>ExecuteInIsolatedScope</c>), delegating<br/>
/// the execution itself to the <see cref="ILocalTypermediaNavigatorSession"/> of the context it begins — after asserting<br/>
/// that the caller stands outside any ambient transaction, so an independent execution can never silently join a caller's<br/>
/// unit of work.</summary>
[UsedImplicitly] class IndependentLocalTypermediaNavigator : IIndependentLocalTypermediaNavigator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IIndependentLocalTypermediaNavigator>()
                                     .CreatedBy((IScopeFactory scopeFactory) => new IndependentLocalTypermediaNavigator(scopeFactory)));

   readonly IScopeFactory _scopeFactory;

   IndependentLocalTypermediaNavigator(IScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      AssertNoAmbientTransaction();
      return _scopeFactory.ExecuteInIsolatedScope(scope => scope.Resolve<ILocalTypermediaNavigatorSession>().Execute(tuery));
   }

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      AssertNoAmbientTransaction();
      return _scopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().Execute(tommand));
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      AssertNoAmbientTransaction();
      _scopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().Execute(tommand));
   }

   public async Task<TResult> ExecuteAsync<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      AssertNoAmbientTransaction();
      return await _scopeFactory.ExecuteInIsolatedScopeAsync(async scope => await scope.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(tuery).caf()).caf();
   }

   public async Task<TResult> ExecuteAsync<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      AssertNoAmbientTransaction();
      return await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(tommand).caf()).caf();
   }

   public async Task ExecuteAsync(IStrictlyLocalTommand tommand)
   {
      AssertNoAmbientTransaction();
      await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<ILocalTypermediaNavigatorSession>().ExecuteAsync(tommand).caf()).caf();
   }

   static void AssertNoAmbientTransaction() =>
      State.Assert(Transaction.Current == null,
                   () => $"{nameof(IIndependentLocalTypermediaNavigator)} was called from within an ambient transaction — inside a unit of work. An independent execution runs in its own context; called here it would silently join the caller's transaction instead of standing alone. Navigate through {nameof(ILocalTypermediaNavigatorSession)}, which deliberately joins the caller's unit of work.");
}
