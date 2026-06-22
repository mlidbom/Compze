using Compze.Abstractions.Tessaging.Public;

namespace Compze.Teventive;

public interface ITeventHandlerRegistrar<in TTevent>
   where TTevent : class, ITevent
{
   ///<summary>Registers a handler for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
   ITeventHandlerRegistrar<TTevent> For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent;

   ITeventHandlerRegistrar<TTevent> ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherIdentifyingTevent<TTevent>;

   ITeventHandlerRegistrar<TTevent> ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler)
      where TWrapperTevent : IPublisherIdentifyingTevent<ITevent>;

   ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
   /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
   /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
   /// </summary>
   ITeventHandlerRegistrar<TTevent> ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent;

   ITeventHandlerRegistrar<TTevent> BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) where THandledTevent : TTevent;
   ITeventHandlerRegistrar<TTevent> AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) where THandledTevent : TTevent;
   ITeventHandlerRegistrar<TTevent> IgnoreUnhandled<TIgnored>() where TIgnored : TTevent;

   ITeventHandlerRegistrar<TTevent> IgnoreAllUnhandled() => IgnoreUnhandled<TTevent>();
}

public static class TeventHandlerRegistrar
{
   public static ITeventHandlerRegistrar<TTevent> BeforeHandlers<TTevent>(this ITeventHandlerRegistrar<TTevent> @this, Action<TTevent> handler) where TTevent : class, ITevent => @this.BeforeHandlers(handler);
   public static ITeventHandlerRegistrar<TTevent> AfterHandlers<TTevent>(this ITeventHandlerRegistrar<TTevent> @this, Action<TTevent> handler) where TTevent : class, ITevent => @this.AfterHandlers(handler);
}