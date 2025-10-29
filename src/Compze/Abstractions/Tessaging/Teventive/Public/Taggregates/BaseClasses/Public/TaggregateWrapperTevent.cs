using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

//todo: should probably be abstract?
public class TaggregateWrapperTevent<TBaseTeventInterface>(TBaseTeventInterface @tevent) : WrapperTevent<TBaseTeventInterface>(@tevent), ITaggregateWrapperTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;