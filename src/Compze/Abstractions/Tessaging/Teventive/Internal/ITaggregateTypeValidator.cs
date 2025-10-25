namespace Compze.Abstractions.Tessaging.Teventive.Internal;

interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}