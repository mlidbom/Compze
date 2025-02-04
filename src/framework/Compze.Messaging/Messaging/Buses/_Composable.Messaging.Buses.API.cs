﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.Messaging.Buses.Implementation;
using Compze.Refactoring.Naming;
using Newtonsoft.Json;

namespace Compze.Messaging.Buses;

///<summary>Dispatches messages between processes.</summary>
public interface IServiceBusSession
{
   ///<summary>Sends a command if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
   void Send(IExactlyOnceCommand command);

   ///<summary>Schedules a command to be sent later if the current transaction succeeds. The execution of the handler runs is a separate transaction at the receiver.</summary>
   void ScheduleSend(DateTime sendAt, IExactlyOnceCommand command);
}

public interface IMessageHandlerRegistrar
{
   IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
   IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
   IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : ICommand<TResult>;
   IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
}

public interface IEndpoint : IAsyncDisposable
{
   EndpointId Id { get; }
   IServiceLocator ServiceLocator { get; }
   EndPointAddress? Address { get; }
   bool IsRunning { get; }
   Task InitAsync();
   Task ConnectAsync();
   Task StopAsync();
   void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
}

public record EndpointId
{
   // ReSharper disable once MemberCanBeInternal : It cannot serialization will fail.
   public Guid GuidValue { get; }
   [JsonConstructor]public EndpointId(Guid guidValue)
   {
      Assert.Argument.Is(guidValue != Guid.Empty);
      GuidValue = guidValue;
   }
}

public interface IEndpointBuilder : IDisposable
{
   IDependencyInjectionContainer Container { get; }
   ITypeMappingRegistar TypeMapper { get; }
   EndpointConfiguration Configuration { get; }
   MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}

public interface IEndpointHost : IAsyncDisposable
{
   IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
   ///<summary>Registers a default client endpoint with a host. Can be called only once per host instance.</summary>
   IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup);
   Task StartAsync();
   void Start();
}

public interface ITestingEndpointHost : IEndpointHost
{
   IEndpoint RegisterTestingEndpoint(string? name = null, EndpointId? id = null, Action<IEndpointBuilder>? setup = null);

   //Urgent: A client "endpoint" makes no sense. It is just a client, not an endpoint. It should be easy to just get a browser for an API rather than pretending to be an endpoint in order to get one.
   IEndpoint RegisterClientEndpointForRegisteredEndpoints();
   TException AssertThrown<TException>() where TException : Exception;
}

interface IExecutingMessagesSnapshot
{
   IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands { get; }
   IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands { get; }
   IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents { get; }
   IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries { get; }
}

interface IMessageDispatchingPolicy;

interface IMessageDispatchingRule
{
   bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage);
}

interface IGlobalBusStateTracker
{
   IReadOnlyList<Exception> GetExceptions();

   void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage);
   void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
   void DoneWith(Guid message, Exception? exception);
}