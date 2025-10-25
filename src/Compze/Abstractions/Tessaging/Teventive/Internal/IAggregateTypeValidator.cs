namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;

interface IAggregateTypeValidator
{
   void AssertIsValid<TAggregate>();
}