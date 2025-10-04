namespace Compze.Abstractions.Internal.Persistence.EventStore;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}