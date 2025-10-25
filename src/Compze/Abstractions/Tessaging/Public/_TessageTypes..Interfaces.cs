using System;

// ReSharper disable UnusedTypeParameter

namespace Compze.Abstractions.Tessaging.Public;

public interface ITessage;

public interface IMustBeSentTransactionally : ITessage;
public interface IMustBeHandledTransactionally : ITessage;
public interface IMustBeSentAndHandledTransactionally : IMustBeSentTransactionally, IMustBeHandledTransactionally;

public interface ICannotBeSentRemotelyFromWithinTransaction : ITessage;
public interface IRequireAResponse : ICannotBeSentRemotelyFromWithinTransaction;
public interface IHypermediaTessage : IRequireAResponse;
public interface IHasReturnValue<out TResult> : IHypermediaTessage;
public interface ITevent : ITessage;
public interface IWrapperTevent<out TEvent> : ITevent //Todo: IWrapperEvent name is not great...
   where TEvent : ITevent
{
   TEvent Event { get; }
}
public interface ITommand : ITessage;
public interface ITommand<out TResult> : ITommand, IHasReturnValue<TResult>;

///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
public interface ITuery<out TResult> : IHasReturnValue<TResult>;

///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
public interface ICreateMyOwnResultTuery<out TResult> : ITuery<TResult>
{
   TResult CreateResult();
}

//Todo: Do we need both Remotable and Strictly local?
public interface IStrictlyLocalMessage;
public interface IStrictlyLocalTevent : ITevent, IStrictlyLocalMessage;
public interface IStrictlyLocalTommand : ITommand, IMustBeSentTransactionally, IStrictlyLocalMessage;
public interface IStrictlyLocalTommand<out TResult> : ITommand<TResult>, IStrictlyLocalTommand;
public interface IStrictlyLocalTuery<TQuery, out TResult> : ITuery<TResult>, IStrictlyLocalMessage where TQuery : IStrictlyLocalTuery<TQuery, TResult>;

//Todo: Why do we need both Remotable and Strictly local?
public interface IRemotableTessage : ITessage;
public interface IRemotableTevent : IRemotableTessage, ITevent;
public interface IRemotableTommand : ITommand, IRemotableTessage;
public interface IRemotableTommand<out TResult> : IRemotableTommand, ITommand<TResult>;
public interface IRemotableTuery<out TResult> : IRemotableTessage, ITuery<TResult>;
public interface IRemotableCreateMyOwnResultTuery<out TResult> : IRemotableTuery<TResult>, ICreateMyOwnResultTuery<TResult>;

//Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
/// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
public interface IAtMostOnceTessage : IRemotableTessage, IMustBeHandledTransactionally
{
   //Refactor: We should use a custom type for MessageIds. Likely a record struct.
   ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
   Guid MessageId { get; }
}
public interface IAtMostOnceHypermediaTommand : IAtMostOnceTessage, IRemotableTommand, IHypermediaTessage;
public interface IAtMostOnceTommand<out TResult> : IAtMostOnceHypermediaTommand, IRemotableTommand<TResult>;


//Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to events without the full exactly once delivery overhead?
//For commands it makes sense that the message-type dictates such things, but for events it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
public interface IExactlyOnceTessage : IMustBeSentAndHandledTransactionally, IAtMostOnceTessage;
public interface IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage;
public interface IExactlyOnceTommand : IRemotableTommand, IExactlyOnceTessage;

public interface IExactlyOnceWrapperTevent<out TEventInterface> : IWrapperTevent<TEventInterface>
   where TEventInterface : IExactlyOnceTevent
{
}