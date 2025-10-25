using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public abstract class TaggregateWrapperTevent<TBaseTeventInterface>(TBaseTeventInterface @tevent) : WrapperTevent<TBaseTeventInterface>(@tevent), ITaggregateWrapperTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;