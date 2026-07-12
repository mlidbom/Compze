using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.Tevents.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

// ReSharper disable once ClassNeverInstantiated.Global
public class TaggregateIdentifyingTevent<TBaseTeventInterface>(TBaseTeventInterface tevent) : PublisherIdentifyingTevent<TBaseTeventInterface>(tevent), ITaggregateIdentifyingTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;