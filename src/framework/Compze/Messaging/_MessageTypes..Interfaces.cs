﻿using System;

// ReSharper disable UnusedTypeParameter

namespace Compze.Messaging;

public interface IMessage;

public interface IMustBeSentTransactionally : IMessage;
public interface IMustBeHandledTransactionally : IMessage;
public interface IMustBeSentAndHandledTransactionally : IMustBeSentTransactionally, IMustBeHandledTransactionally;

public interface ICannotBeSentRemotelyFromWithinTransaction : IMessage;
public interface IRequireAResponse : ICannotBeSentRemotelyFromWithinTransaction;
public interface IHypermediaMessage : IRequireAResponse;
public interface IHasReturnValue<out TResult> : IHypermediaMessage;
public interface IEvent : IMessage;
public interface IWrapperEvent<out TEvent> : IEvent //Todo: IWrapperEvent name is not great...
   where TEvent : IEvent
{
   TEvent Event { get; }
}
public interface ICommand : IMessage;
public interface ICommand<out TResult> : ICommand, IHasReturnValue<TResult>;

///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
public interface IQuery<out TResult> : IHasReturnValue<TResult>;

///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
interface ICreateMyOwnResultQuery<out TResult> : IQuery<TResult>
{
   TResult CreateResult();
}

//Todo: Do we need both Remotable and Strictly local?
public interface IStrictlyLocalMessage;
public interface IStrictlyLocalEvent : IEvent, IStrictlyLocalMessage;
public interface IStrictlyLocalCommand : ICommand, IMustBeSentTransactionally, IStrictlyLocalMessage;
public interface IStrictlyLocalCommand<out TResult> : ICommand<TResult>, IStrictlyLocalCommand;
public interface IStrictlyLocalQuery<TQuery, out TResult> : IQuery<TResult>, IStrictlyLocalMessage where TQuery : IStrictlyLocalQuery<TQuery, TResult>;

//Todo: Why do we need both Remotable and Strictly local?
public interface IRemotableMessage : IMessage;
public interface IRemotableEvent : IRemotableMessage, IEvent;
public interface IRemotableCommand : ICommand, IRemotableMessage;
public interface IRemotableCommand<out TResult> : IRemotableCommand, ICommand<TResult>;
public interface IRemotableQuery<out TResult> : IRemotableMessage, IQuery<TResult>;
interface IRemotableCreateMyOwnResultQuery<out TResult> : IRemotableQuery<TResult>, ICreateMyOwnResultQuery<TResult>;

//Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
/// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
public interface IAtMostOnceMessage : IRemotableMessage, IMustBeHandledTransactionally
{
   //Refactor: We should use a custom type for MessageIds. Likely a record struct.
   ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
   Guid MessageId { get; }
}
public interface IAtMostOnceHypermediaCommand : IAtMostOnceMessage, IRemotableCommand, IHypermediaMessage;
public interface IAtMostOnceCommand<out TResult> : IAtMostOnceHypermediaCommand, IRemotableCommand<TResult>;


//Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to events without the full exactly once delivery overhead?
//For commands it makes sense that the message-type dictates such things, but for events it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
public interface IExactlyOnceMessage : IMustBeSentAndHandledTransactionally, IAtMostOnceMessage;
public interface IExactlyOnceEvent : IRemotableEvent, IExactlyOnceMessage;
public interface IExactlyOnceCommand : IRemotableCommand, IExactlyOnceMessage;

public interface IExactlyOnceWrapperEvent<out TEventInterface> : IWrapperEvent<TEventInterface>
   where TEventInterface : IExactlyOnceEvent
{
}