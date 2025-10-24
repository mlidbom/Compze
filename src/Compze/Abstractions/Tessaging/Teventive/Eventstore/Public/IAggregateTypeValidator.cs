namespace Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}