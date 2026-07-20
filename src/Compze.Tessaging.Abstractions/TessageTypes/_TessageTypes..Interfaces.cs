

// ReSharper disable MemberCanBeInternal

#pragma warning disable CA1040 //We define a number of empty marker interfaces here that are vital for framework functionality
// ReSharper disable UnusedTypeParameter

namespace Compze.Tessaging.Abstractions.TessageTypes;

///<summary>A message routed by type</summary>
public interface ITessage;

///<summary>A type routed message that informs that something happened.</summary>
public interface ITevent : ITessage;

public interface IExactlyOneReceiverTessage : ITessage;

///<summary>A type routed message that instructs the receiver to do something.</summary>
public interface ITommand : IExactlyOneReceiverTessage;

//todo:review: Should the commented out type below exist?
//public interface IFireAndForgetTommand : ITommand;

//Transactional behavior marker interfaces
public interface IMustBeSentTransactionally : ITessage;
public interface IMustBeHandledTransactionally : ITessage;
public interface IMustBeSentAndHandledTransactionally : IMustBeSentTransactionally, IMustBeHandledTransactionally;
public interface ICannotBeSentRemotelyFromWithinTransaction : ITessage;

//Typermedia
//todo:review: should the version without a type parameter remain? Could ITyperMediaTessage<object> replace what it does today?
public interface ITypermediaTessage : ICannotBeSentRemotelyFromWithinTransaction, IExactlyOneReceiverTessage;
public interface ITyperMediaTessage<out TResult> : ITypermediaTessage;
public interface ITommand<out TResult> : ITommand, ITyperMediaTessage<TResult>;

//todo:review: should the version without a type parameter remain? Could ITuery<object> replace what it does today?
public interface ITuery : ITypermediaTessage;
public interface ITuery<out TResult> : ITuery, ITyperMediaTessage<TResult>;

///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the tuery is sufficient to create the result. For such tueries implement this interface. That way no network roundtrip is required to perform the tuery.</summary>
public interface ICreateMyOwnResultTuery<out TResult> : ITuery<TResult>
{
   TResult CreateResult();
}

//Note that when you look at a strictly local tessage you have guarantees about its behavior that you don't have looking at just tessage with the absence of an explicit IRemotable declaration.
//The concrete types implementing the interfaces might have been remotable, making for lost guarantees.
//With the strictly local message types we can implement behavioral guarantees in frameworks, forbidding nonsensical combinations.
public interface IStrictlyLocalTessage;


//todo:review: Should the commented out type below exist?
//public interface IStrictlyLocalTevent : ITevent, IStrictlyLocalTessage;
//todo:review: should this inherit IMustBeSentTransactionally?
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

public interface ITessageWithIdentity : ITessage
{
   ///<summary>Uniquely identifies exactly one <see cref="ITessageWithIdentity"/> Must be generated when the tessage is created and then never modified. Used by the infrastructure to implement delivery guarantees such as at-[most|least] once.</summary>
   TessageId Id { get; }
}

///<summary>A tessage that is handled at least once. Guaranteed by infrastructure through retries.</summary>
public interface IAtLeastOnceTessage : ITessageWithIdentity, IRemotableTessage;

///<summary>A tessage that is handled no more than once. Guaranteed by infrastructure through deduplication, and transactions.</summary>
public interface IAtMostOnceTessage : ITessageWithIdentity, IRemotableTessage, IMustBeHandledTransactionally;

//todo:review: should the version without a type parameter remain?
public interface IAtMostOnceTypermediaTommand : IAtMostOnceTessage, IRemotableTommand, ITypermediaTessage;
public interface IAtMostOnceTypermediaTommand<out TResult> : IAtMostOnceTypermediaTommand, IRemotableTommand<TResult>;

///<summary>A tessage that is handled exactly once. Guaranteed by infrastructure through deduplication, retries, and transactions.</summary>
public interface IExactlyOnceTessage : IMustBeSentAndHandledTransactionally, IAtMostOnceTessage, IAtLeastOnceTessage;

public interface IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage;
public interface IExactlyOnceTommand : IRemotableTommand, IExactlyOnceTessage;

///<summary>
/// Distinguishes tevents from each other by who published them.
/// These are needed because when a raw tevent of the same type is published by more than one publisher* they cannot be distinguished from each other by type
/// and thus cannot be subscribed to precisely.
///
/// To fix this, different publishers should wrap the tevents in another tevent that identifies the publisher by its type.
/// For example,
/// 
/// <code>
/// <![CDATA[
///   IAnimalTevent<out T> : IPublisherTevent<T> {} //Make sure to use your own base type so that you cann subscribe to the whole hierarchy
///   IMammalTevent<out T> : IAnimalTevent<T> {}
///   IZebraTevent<out T> : IMammalTevent<T> {}
///   ITigerTevent<out T> : IMammalTevent<T> {}
/// ]]>
/// </code>
/// 
/// The built-in taggregate and teventive base classes already do this automatically.
/// 
/// * For example, when teventives inherit, or compose, each other.
/// </summary>
public interface IPublisherTevent<out TTevent> : ITevent
   where TTevent : ITevent
{
   TTevent Tevent { get; }
}
