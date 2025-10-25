using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public abstract class TaggregateWrapperTevent<TBaseTeventInterface>(TBaseTeventInterface @tevent) : WrapperTevent<TBaseTeventInterface>(@tevent), ITaggregateWrapperTevent<TBaseTeventInterface>
   where TBaseTeventInterface : ITaggregateTevent;