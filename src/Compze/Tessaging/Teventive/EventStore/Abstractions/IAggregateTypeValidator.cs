namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}