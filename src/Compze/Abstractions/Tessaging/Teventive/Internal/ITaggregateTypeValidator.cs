namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;

interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}