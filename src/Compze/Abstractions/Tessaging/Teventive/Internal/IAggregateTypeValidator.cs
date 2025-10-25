namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Internal;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}