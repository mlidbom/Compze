namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}