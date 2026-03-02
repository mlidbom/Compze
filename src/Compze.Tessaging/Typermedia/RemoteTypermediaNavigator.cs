using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Typermedia;


public static class RemoteHypermediaNavigatorRegistrar
{
   public static IComponentRegistrar RemoteHypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(RemoteTypermediaNavigator.RegisterWith);
}

//Todo: Build a pipeline to handle things like tommand validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteTypermediaNavigator(ITypermediaRouter typermediaRouter) : IRemoteTypermediaNavigator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IRemoteTypermediaNavigator>()
                                  .CreatedBy((ITypermediaRouter typermediaRouter) => new RemoteTypermediaNavigator(typermediaRouter)));

   readonly ITypermediaRouter _typermediaRouter = typermediaRouter;

   public void Post(IAtMostOnceTypermediaTommand tommand) => PostAsync(tommand).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      TessageInspector.AssertValidToSendRemote(tommand);
      return _typermediaRouter.PostAsync(tommand);
   }

   public TResult Post<TResult>(IAtMostOnceTommand<TResult> typermediaTommand) => PostAsync(typermediaTommand).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> typermediaTommand)
   {
      TessageInspector.AssertValidToSendRemote(typermediaTommand);
      return _typermediaRouter.PostAsync(typermediaTommand);
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      TessageInspector.AssertValidToSendRemote(tuery);
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return await Task.FromResult(selfCreating.CreateResult()).caf();

      return await GetAsyncAfterFastPathOptimization(tuery).caf();
   }

   async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableTuery<TResult> tuery) => await _typermediaRouter.GetAsync(tuery).caf();

   TResult IRemoteTypermediaNavigator.Get<TResult>(IRemotableTuery<TResult> tuery) => GetAsync(tuery).ResultUnwrappingException();
}
