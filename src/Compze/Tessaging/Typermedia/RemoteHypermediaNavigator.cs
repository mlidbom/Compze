using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Common;
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

//Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteHypermediaNavigator : IRemoteHypermediaNavigator
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IRemoteHypermediaNavigator>()
                                  .CreatedBy((ITransportClient transportClient) => new RemoteHypermediaNavigator(transportClient)));

   readonly ITransportClient _transportClient;
   public RemoteHypermediaNavigator(ITransportClient transportClient) { _transportClient = transportClient; }

   public void Post(IAtMostOnceHypermediaCommand command) => PostAsync(command).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceHypermediaCommand command)
   {
      MessageInspector.AssertValidToSendRemote(command);
      return _transportClient.PostAsync(command);
   }

   public TResult Post<TResult>(IAtMostOnceCommand<TResult> command) => PostAsync(command).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command)
   {
      MessageInspector.AssertValidToSendRemote(command);
      return _transportClient.PostAsync(command);
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query)
   {
      MessageInspector.AssertValidToSendRemote(query);
      if(query is ICreateMyOwnResultQuery<TResult> selfCreating)
         return await Task.FromResult(selfCreating.CreateResult()).caf();

      return await GetAsyncAfterFastPathOptimization(query).caf();
   }

   async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableQuery<TResult> query) => await _transportClient.GetAsync(query).caf();

   TResult IRemoteHypermediaNavigator.Get<TResult>(IRemotableQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();
}
