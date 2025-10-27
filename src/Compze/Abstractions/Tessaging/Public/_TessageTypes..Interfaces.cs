using System;

// ReSharper disable UnusedTypeParameter

namespace Compze.Core.Tessaging.Public;

//Note that we do not handle messages that don't encode meaning and routing through types, that is: IMessage.
//That's why ITessage is the ultimate root of our hierarchy of meaning, not IMessage
public interface ITessage;
public interface ITevent : ITessage;
public interface ITommand : ITessage;
public interface IFireAndForgetTommand : ITommand;

//Transactional behavior marker interfaces
public interface IMustBeSentTransactionally : ITessage;
public interface IMustBeHandledTransactionally : ITessage;
public interface IMustBeSentAndHandledTransactionally : IMustBeSentTransactionally, IMustBeHandledTransactionally;
public interface ICannotBeSentRemotelyFromWithinTransaction : ITessage;


//Typermedia
public interface ITypermediaTessage : ICannotBeSentRemotelyFromWithinTransaction;
public interface ITyperMediaTessage<out TResult> : ITypermediaTessage;
public interface ITommand<out TResult> : ITommand, ITyperMediaTessage<TResult>;
public interface ITuery<out TResult> : ITyperMediaTessage<TResult>;

///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the tuery is sufficient to create the result. For such queries implement this interface. That way no network roundtrip is required to perform the tuery.</summary>
public interface ICreateMyOwnResultTuery<out TResult> : ITuery<TResult>
{
   TResult CreateResult();
}

//Note that when you look at a strictly local tessage you have guarantees about its behavior that you don't have looking at just tessage with the absence of an explicit IRemotable declaration.
//The concrete types implementing the interfaces might have been remotable, making for lost guarantees.
//With the strictly local message types we can implement behavioral guarantees in frameworks.
public interface IStrictlyLocalTessage;
public interface IStrictlyLocalTevent : ITevent, IStrictlyLocalTessage;
public interface IStrictlyLocalTommand : ITommand, IMustBeSentTransactionally, IStrictlyLocalTessage;
public interface IStrictlyLocalTommand<out TResult> : ITommand<TResult>, IStrictlyLocalTommand;
public interface IStrictlyLocalTuery<TTuery, out TResult> : ITuery<TResult>, IStrictlyLocalTessage where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

public interface IRemotableTessage : ITessage;
public interface IRemotableTevent : IRemotableTessage, ITevent;
public interface IRemotableTommand : ITommand, IRemotableTessage;
public interface IRemotableTommand<out TResult> : IRemotableTommand, ITommand<TResult>;
public interface IRemotableTuery<out TResult> : IRemotableTessage, ITuery<TResult>;

//Clients that are not .NET types need to send the query to the closest .NET endpoint before that will bounce the result back.
public interface IRemotableCreateMyOwnResultTuery<out TResult> : IRemotableTuery<TResult>, ICreateMyOwnResultTuery<TResult>;

//Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
///<summary>A tessage that is guaranteed not to be delivered more than once. The <see cref="Id"/> is used by infrastructure to maintain this guarantee.
/// The <see cref="Id"/> must be maintained when binding a tommand to a UI or the guarantee will be lost.</summary>
public interface IAtMostOnceTessage : IRemotableTessage, IMustBeHandledTransactionally
{
   //Refactor: We should use a custom type for TessageIds. Likely a readonly record struct.
   ///<summary>Used by the infrastructure to guarantee that the same tessage is never delivered more than once. Must be generated when the tessage is created and then NEVER modified. Must be maintained when binding a tommand in a UI etc.</summary>
   Guid Id { get; }
}
public interface IAtMostOnceTypermediaTommand : IAtMostOnceTessage, IRemotableTommand, ITypermediaTessage;
public interface IAtMostOnceTommand<out TResult> : IAtMostOnceTypermediaTommand, IRemotableTommand<TResult>;


//Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to tevents without the full exactly once delivery overhead?
//For tommands it makes sense that the tessage-type dictates such things, but for tevents it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
public interface IExactlyOnceTessage : IMustBeSentAndHandledTransactionally, IAtMostOnceTessage;
public interface IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage;
public interface IExactlyOnceTommand : IRemotableTommand, IExactlyOnceTessage;

public interface IWrapperTevent<out TTevent> : ITevent //Todo: IWrapperTevent name is not great...
   where TTevent : ITevent
{
   TTevent Tevent { get; }
}

public interface IExactlyOnceWrapperTevent<out TTeventInterface> : IWrapperTevent<TTeventInterface>
   where TTeventInterface : IExactlyOnceTevent
{
}