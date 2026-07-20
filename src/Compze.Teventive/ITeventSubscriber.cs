using Compze.Tessaging;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive;

///<summary>A party's subscribing presence on an <see cref="IMutableTeventDispatcher{TTevent}"/>, created by <see cref="IMutableTeventDispatcher{TTevent}.Register"/>.<br/>
/// Each registration method adds one subscription: a handler the dispatcher calls for every dispatched tevent that matches the subscribed tevent type.<br/>
/// Disposing the subscriber removes every subscription made through it; registering through a disposed subscriber throws.</summary>
public interface ITeventSubscriber<in TTevent> : IDisposable
   where TTevent : class, ITevent
{
   ///<summary>Registers a handler for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
   ITeventSubscriber<TTevent> For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent;

   ITeventSubscriber<TTevent> ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherTevent<TTevent>;

   ITeventSubscriber<TTevent> ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherTevent<ITevent>;

   ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
   /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
   /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
   /// </summary>
   ITeventSubscriber<TTevent> ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent;

   ITeventSubscriber<TTevent> BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) where THandledTevent : TTevent;
   ITeventSubscriber<TTevent> AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) where THandledTevent : TTevent;
}

public static class TeventSubscriberCE
{
   extension<TTevent>(ITeventSubscriber<TTevent> @this) where TTevent : class, ITevent
   {
      public ITeventSubscriber<TTevent> BeforeHandlers(Action<TTevent> handler) => @this.BeforeHandlers(handler);
      public ITeventSubscriber<TTevent> AfterHandlers(Action<TTevent> handler) => @this.AfterHandlers(handler);
   }
}
