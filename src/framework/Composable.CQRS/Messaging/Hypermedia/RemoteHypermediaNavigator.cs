﻿using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Composable.Messaging.Hypermedia;

//Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
[UsedImplicitly] class RemoteHypermediaNavigator : IRemoteHypermediaNavigator
{
   readonly ITransport _transport;

   public RemoteHypermediaNavigator(ITransport transport) => _transport = transport;

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

   public Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query)
   {
      MessageInspector.AssertValidToSendRemote(query);
      if(query is ICreateMyOwnResultQuery<TResult> selfCreating)
         return Task.FromResult(selfCreating.CreateResult());

      return GetAsyncAfterFastPathOptimization(query);
   }
   Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableQuery<TResult> query) => _transport.GetAsync(query);

   TResult IRemoteHypermediaNavigator.Get<TResult>(IRemotableQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();
}