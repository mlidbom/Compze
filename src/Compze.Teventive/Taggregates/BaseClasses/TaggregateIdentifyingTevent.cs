using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.Tevents.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>Wraps tevents in taggregate-declared <see cref="ITaggregateIdentifyingTevent{TTeventInterface}"/> wrapper implementations.</summary>
public static class TaggregateIdentifyingTevent
{
   ///<summary>Wraps <paramref name="tevent"/> in <paramref name="wrapperTeventImplementation"/>'s generic type definition closed over the tevent's runtime type -<br/>
   /// the same wrapping a <c>Taggregate</c> performs when publishing through its declared <c>WrapperTeventImplementation</c>.<br/>
   /// This is how a tevent migration author wraps a replacement tevent in the publisher's wrapper.</summary>
   public static ITaggregateIdentifyingTevent<ITaggregateTevent> WrapIn(Type wrapperTeventImplementation, ITaggregateTevent tevent) =>
      (ITaggregateIdentifyingTevent<ITaggregateTevent>)Constructor.ForGenericType(wrapperTeventImplementation)
                                                                  .WithArgument(tevent.GetType())
                                                                  .Invoke(tevent);
}

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateIdentifyingTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : PublisherIdentifyingTevent<TBaseTeventInterface>(tevent), ITaggregateIdentifyingTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;