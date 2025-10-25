using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Typermedia;


static class RemoteHypermediaNavigatorRegistrar
{
   internal static IComponentRegistrar RemoteHypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Typermedia.RemoteHypermediaNavigator.RegisterWith);
}

//Todo: Build a pipeline to handle things like tommand validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteHypermediaNavigator : IRemoteHypermediaNavigator
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IRemoteHypermediaNavigator>()
                                  .CreatedBy((ITransportClient transportClient) => new RemoteHypermediaNavigator(transportClient)));

   readonly ITransportClient _transportClient;
   public RemoteHypermediaNavigator(ITransportClient transportClient) { _transportClient = transportClient; }

   public void Post(IAtMostOnceHypermediaTommand tommand) => PostAsync(tommand).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceHypermediaTommand tommand)
   {
      TessageInspector.AssertValidToSendRemote(tommand);
      return _transportClient.PostAsync(tommand);
   }

   public TResult Post<TResult>(IAtMostOnceTommand<TResult> tommand) => PostAsync(tommand).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> tommand)
   {
      TessageInspector.AssertValidToSendRemote(tommand);
      return _transportClient.PostAsync(tommand);
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      TessageInspector.AssertValidToSendRemote(tuery);
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return await Task.FromResult(selfCreating.CreateResult()).caf();

      return await GetAsyncAfterFastPathOptimization(tuery).caf();
   }

   async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableTuery<TResult> tuery) => await _transportClient.GetAsync(tuery).caf();

   TResult IRemoteHypermediaNavigator.Get<TResult>(IRemotableTuery<TResult> tuery) => GetAsync(tuery).ResultUnwrappingException();
}
