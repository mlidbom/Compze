using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateTypeIdentifyingTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : PublisherTypeIdentifyingTevent<TBaseTeventInterface>(tevent), ITaggregateTypeIdentifyingTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;