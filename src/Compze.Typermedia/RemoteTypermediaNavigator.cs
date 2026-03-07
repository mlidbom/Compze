using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Tessaging.Validation;
using JetBrains.Annotations;

namespace Compze.Typermedia;

public static class RemoteTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar RemoteTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Compze.Typermedia.RemoteTypermediaNavigator.RegisterWith);

   public static IComponentRegistrar SingletonRemoteTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Compze.Typermedia.RemoteTypermediaNavigator.RegisterSingletonWith);
}

//Todo: Build a pipeline to handle things like tommand validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteTypermediaNavigator(ITypermediaRouting typermediaRouting) : IRemoteTypermediaNavigator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IRemoteTypermediaNavigator>()
                                  .CreatedBy((ITypermediaRouting typermediaRouting) => new RemoteTypermediaNavigator(typermediaRouting)));

   internal static void RegisterSingletonWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemoteTypermediaNavigator>()
                                     .CreatedBy((ITypermediaRouting typermediaRouting) => new RemoteTypermediaNavigator(typermediaRouting)));

   readonly ITypermediaRouting _typermediaRouting = typermediaRouting;

   public void Post(IAtMostOnceTypermediaTommand tommand) => PostAsync(tommand).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      TessageValidator.AssertValidToSendRemote(tommand);
      return _typermediaRouting.PostAsync(tommand);
   }

   public TResult Post<TResult>(IAtMostOnceTommand<TResult> typermediaTommand) => PostAsync(typermediaTommand).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> typermediaTommand)
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
