using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateWrapperTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : WrapperTevent<TBaseTeventInterface>(tevent), ITaggregateWrapperTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;