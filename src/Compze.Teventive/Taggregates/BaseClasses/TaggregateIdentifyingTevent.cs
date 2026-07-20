using Compze.Tessaging.Abstractions.TessageBus;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>Wraps tevents in taggregate-declared <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper implementations.</summary>
public static class TaggregateIdentifyingTevent
{
   ///<summary>Wraps <paramref name="tevent"/> in <paramref name="wrapperTeventImplementation"/>'s generic type definition closed over the tevent's runtime type -<br/>
   /// the same wrapping a <c>Taggregate</c> performs when publishing through its declared <c>WrapperTeventImplementation</c>.<br/>
   /// This is how a tevent migration author wraps a replacement tevent in the publisher's wrapper.</summary>
   public static ITaggregateTevent<ITaggregateTevent> WrapIn(Type wrapperTeventImplementation, ITaggregateTevent tevent) =>
      (ITaggregateTevent<ITaggregateTevent>)PublisherTevent.WrapIn(wrapperTeventImplementation, tevent);
}

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : PublisherTevent<TBaseTeventInterface>(tevent), ITaggregateTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;