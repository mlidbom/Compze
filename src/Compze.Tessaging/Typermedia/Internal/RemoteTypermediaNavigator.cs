using Compze.Tessaging.Validation.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.Typermedia.Internal;

static class RemoteTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar RemoteTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Internal.RemoteTypermediaNavigator.RegisterWith);
}

//Todo: Build a pipeline to handle things like tommand validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteTypermediaNavigator(ITypermediaRouting typermediaRouting) : IRemoteTypermediaNavigator
{
   //Singleton, deliberately: remote navigation has no unit-of-work relationship to be scoped to - a typermedia tessage cannot be sent remotely from within a transaction, and routing is a singleton - so there is nothing a scope would give it.
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemoteTypermediaNavigator>()
                                     .CreatedBy((ITypermediaRouting typermediaRouting) => new RemoteTypermediaNavigator(typermediaRouting)));

   readonly ITypermediaRouting _typermediaRouting = typermediaRouting;

   public void Post(IAtMostOnceTypermediaTommand tommand) => PostAsync(tommand).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      TessageValidator.AssertValidToSendRemote(tommand);
      return _typermediaRouting.PostAsync(tommand);
   }

   public TResult Post<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand) => PostAsync(typermediaTommand).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> typermediaTommand)
   {
      TessageValidator.AssertValidToSendRemote(typermediaTommand);
      return _typermediaRouting.PostAsync(typermediaTommand);
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      TessageValidator.AssertValidToSendRemote(tuery);
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return await Task.FromResult(selfCreating.CreateResult()).caf();

      return await GetAsyncAfterFastPathOptimization(tuery).caf();
   }

   async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableTuery<TResult> tuery) => await _typermediaRouting.GetAsync(tuery).caf();

   TResult IRemoteTypermediaNavigator.Get<TResult>(IRemotableTuery<TResult> tuery) => GetAsync(tuery).ResultUnwrappingException();
}
