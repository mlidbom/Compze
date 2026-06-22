using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateIdentifyingTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : PublisherIdentifyingTevent<TBaseTeventInterface>(tevent), ITaggregateIdentifyingTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;