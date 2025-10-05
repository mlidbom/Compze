using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Typermedia;

//Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteHypermediaNavigator(ITransport transport) : IRemoteHypermediaNavigator
{
   readonly ITransport _transport = transport;

   public void Post(IAtMostOnceHypermediaCommand command) => PostAsync(command).WaitUnwrappingException();

   public Task PostAsync(IAtMostOnceHypermediaCommand command)
   {
      MessageInspector.AssertValidToSendRemote(command);
      return _transport.PostAsync(command);
   }

   public TResult Post<TResult>(IAtMostOnceCommand<TResult> command) => PostAsync(command).ResultUnwrappingException();

   public Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command)
   {
      MessageInspector.AssertValidToSendRemote(command);
      return _transport.PostAsync(command);
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query)
   {
      MessageInspector.AssertValidToSendRemote(query);
      if(query is ICreateMyOwnResultQuery<TResult> selfCreating)
         return await Task.FromResult(selfCreating.CreateResult()).caf();

      return await GetAsyncAfterFastPathOptimization(query).caf();
   }
   async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableQuery<TResult> query) => await _transport.GetAsync(query).caf();

   TResult IRemoteHypermediaNavigator.Get<TResult>(IRemotableQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();
}