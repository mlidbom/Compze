namespace Compze.Core.Tessaging.Teventive.Internal;

interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}