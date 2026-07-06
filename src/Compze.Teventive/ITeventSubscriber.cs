using Compze.Abstractions.Tessaging.Public;

namespace Compze.Teventive;

public interface ITeventSubscriber<in TTevent>
   where TTevent : class, ITevent
{
   ///<summary>Registers a handler for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
   ITeventSubscriber<TTevent> For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent;

   ITeventSubscriber<TTevent> ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherIdentifyingTevent<TTevent>;

   ITeventSubscriber<TTevent> ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherIdentifyingTevent<ITevent>;

   ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
   /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
   /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
   /// </summary>
   ITeventSubscriber<TTevent> ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent;

   ITeventSubscriber<TTevent> BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) where THandledTevent : TTevent;
   ITeventSubscriber<TTevent> AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) where THandledTevent : TTevent;
}

public static class TeventHandlerRegistrar
{
   public static ITeventSubscriber<TTevent> BeforeHandlers<TTevent>(this ITeventSubscriber<TTevent> @this, Action<TTevent> handler) where TTevent : class, ITevent => @this.BeforeHandlers(handler);
   public static ITeventSubscriber<TTevent> AfterHandlers<TTevent>(this ITeventSubscriber<TTevent> @this, Action<TTevent> handler) where TTevent : class, ITevent => @this.AfterHandlers(handler);
}
