namespace Compze.Persistence.EventStore;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}