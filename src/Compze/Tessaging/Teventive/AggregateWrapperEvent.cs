using Compze.EventStore.Abstractions;
using Compze.Tessaging.Common;

namespace Compze.Teventive;

public abstract class AggregateWrapperEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateWrapperEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;