using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public abstract class AggregateWrapperTevent<TBaseTeventInterface>(TBaseTeventInterface @tevent) : WrapperTevent<TBaseTeventInterface>(@tevent), IAggregateWrapperTevent<TBaseTeventInterface>
   where TBaseTeventInterface : IAggregateTevent;