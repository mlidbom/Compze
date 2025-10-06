namespace Compze.EventStore.Abstractions;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}