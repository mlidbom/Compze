using Compze.Abstractions.Public;
using static Compze.Abstractions.Tessaging.Public.TessageTypes.Remotable.NonTransactional.Tueries;
// ReSharper disable MemberCanBeInternal

#pragma warning disable CA1040 //We define a number of empty marker interfaces here that are vital for framework functionality
// ReSharper disable UnusedTypeParameter

namespace Compze.Abstractions.Tessaging.Public;


//Ordinary plain old messages, not necessarily type routed
public interface IMessage;
public interface IEvent : IMessage;
public interface ICommand : IMessage;
public interface ICommand<out TResult> : ICommand;
public interface IQuery<out TResult> : IMessage;

//From here on down everything is Tessages. Type routed messages.
public interface ITessage : IMessage;
public interface ITevent : ITessage, IEvent;
public interface ITommand : ITessage, ICommand;

//todo: Should the commented out type below exist?
//public interface IFireAndForgetTommand : ITommand;

//Transactional behavior marker interfaces
public interface IMustBeSentTransactionally : ITessage;
public interface IMustBeHandledTransactionally : ITessage;
public interface IMustBeSentAndHandledTransactionally : IMustBeSentTransactionally, IMustBeHandledTransactionally;
public interface ICannotBeSentRemotelyFromWithinTransaction : ITessage;


//Typermedia
public interface ITypermediaTessage : ICannotBeSentRemotelyFromWithinTransaction;
public interface ITyperMediaTessage<out TResult> : ITypermediaTessage;
public interface ITommand<out TResult> : ITommand, ITyperMediaTessage<TResult>;
public interface ITuery<out TResult> : IQuery<TResult>, ITyperMediaTessage<TResult>;

///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the tuery is sufficient to create the result. For such tueries implement this interface. That way no network roundtrip is required to perform the tuery.</summary>
public interface ICreateMyOwnResultTuery<out TResult> : ITuery<TResult>
{
   TResult CreateResult();
}


//Note that when you look at a strictly local tessage you have guarantees about its behavior that you don't have looking at just tessage with the absence of an explicit IRemotable declaration.
//The concrete types implementing the interfaces might have been remotable, making for lost guarantees.
//With the strictly local message types we can implement behavioral guarantees in frameworks, forbidding nonsensical combinations.
public interface IStrictlyLocalTessage;
///<summary>Marker interface for infrastructure-internal tessages that should be excluded from remote route advertisement.</summary>
public interface IInternalInfrastructureTessage;
//todo: Should the commented out type below exist?
//public interface IStrictlyLocalTevent : ITevent, IStrictlyLocalTessage;
public interface IStrictlyLocalTommand : ITommand, IMustBeSentTransactionally, IStrictlyLocalTessage;
public interface IStrictlyLocalTommand<out TResult> : ITommand<TResult>, IStrictlyLocalTommand;
public interface IStrictlyLocalTuery<TTuery, out TResult> : ITuery<TResult>, IStrictlyLocalTessage where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

public interface IRemotableTessage : ITessage;
public interface IRemotableTevent : ITevent, IRemotableTessage;
public interface IRemotableTommand : ITommand, IRemotableTessage;
public interface IRemotableTommand<out TResult> : ITommand<TResult>, IRemotableTommand;
public interface IRemotableTuery<out TResult> : ITuery<TResult>, IRemotableTessage;

//Clients that are not .NET types need to send the query to the closest .NET endpoint before that will bounce the result back.
public interface IRemotableCreateMyOwnResultTuery<out TResult> : IRemotableTuery<TResult>, ICreateMyOwnResultTuery<TResult>;

//Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
///<summary>A tessage that is guaranteed not to be delivered more than once. The <see cref="Id"/> is used by infrastructure to maintain this guarantee.
/// The <see cref="Id"/> must be maintained when binding a tommand to a UI or the guarantee will be lost.</summary>
public interface IAtMostOnceTessage : IRemotableTessage, IMustBeHandledTransactionally
{
   ///<summary>Used by the infrastructure to guarantee that the same tessage is never delivered more than once. Must be generated when the tessage is created and then NEVER modified. Must be maintained when binding a tommand in a UI etc.</summary>
   TessageId Id { get; }
}
public interface IAtMostOnceTypermediaTommand : IAtMostOnceTessage, IRemotableTommand, ITypermediaTessage;
public interface IAtMostOnceTommand<out TResult> : IAtMostOnceTypermediaTommand, IRemotableTommand<TResult>;


//Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to tevents without the full exactly once delivery overhead?
//For tommands it makes sense that the tessage-type dictates such things, but for tevents it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
public interface IExactlyOnceTessage : IMustBeSentAndHandledTransactionally, IAtMostOnceTessage;
public interface IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage;
public interface IExactlyOnceTommand : IRemotableTommand, IExactlyOnceTessage;

///<summary>
/// When different types publish tevents of the same type it is impossible to distinguish the publisher by that tevent alone.
/// To ensure any tevent can be subscribed to each teventive wraps their tevents in tevents of this type.
///
/// * For example when taggregates inherit each other, or uses a reusable** tomponent or tentity.
/// ** Not exclusive to that taggregate
/// </summary>
public interface IPublisherIdentifyingTevent<out TTevent> : ITevent
   where TTevent : ITevent
{
   TTevent Tevent { get; }
}

public interface IRemotablePublisherIdentifyingTevent<out TTeventInterface> : IPublisherIdentifyingTevent<TTeventInterface>, IRemotableTevent
   where TTeventInterface : IExactlyOnceTevent;


public interface IExactlyOncePublisherIdentifyingTevent<out TTeventInterface> : IRemotablePublisherIdentifyingTevent<TTeventInterface>, IExactlyOnceTevent
   where TTeventInterface : IExactlyOnceTevent
{
#pragma warning disable CA1033 //Explicit interface implementation is the only way I know of to add a default implementation here.
   TessageId IAtMostOnceTessage.Id => Tevent.Id;
#pragma warning restore CA1033 //Explicit interface implementation is the only way I know of to add a default implementation here.
}